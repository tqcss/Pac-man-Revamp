using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PacmanMazeController : MonoBehaviour
{
    [Header("Pacman Properties")]
    [SerializeField] private GameObject pacman;
    [SerializeField] private float defaultSpeed;

    [Header("Pacman Miscellaneous")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Animator animator;
    [SerializeField] private LayerMask collisionLayer;
    [SerializeField] private Text pointsText;
    
    [Header("Pacman Effect Item Info")]
    [SerializeField] private EffectItemList effectItemList;
    [Space(4)]
    [SerializeField] private Image effectItemIcon;
    [SerializeField] private Text cdText_effectItem;
    [Space(4)]
    [SerializeField] private Image ghostAmbient_darkness;
    
    private bool hasEffectItem = false;
    private float cd_effectItem = 0f;
    private float lastCd_effectItem = -Mathf.Infinity;

    [Space(12)]
    [SerializeField] private bool isMazeStarted = false;
    private bool isRunning = false;
    
    private Vector2 direction;
    private Vector2 targetPosition;
    private Vector2 queuedDirection;

    private BoxCollider2D pacmanCollider;

    private const float TILE_SIZE = 0.16f;


    private void Start()
    {
        InitializePacman();
        RegisterKeyActions();
    }

    private void OnDestroy()
    {
        UnregisterKeyActions();
    }

    public void StartPacmanController(bool triggerValue)
    {
        isMazeStarted = triggerValue;
    }

    private void RegisterKeyActions()
    {
        KeybindDataManager.RegisterKeyAction("pacman.face_up", () => HandleInput("pacman.face_up"));
        KeybindDataManager.RegisterKeyAction("pacman.face_down", () => HandleInput("pacman.face_down"));
        KeybindDataManager.RegisterKeyAction("pacman.face_left", () => HandleInput("pacman.face_left"));
        KeybindDataManager.RegisterKeyAction("pacman.face_right", () => HandleInput("pacman.face_right"));
        KeybindDataManager.RegisterKeyAction("pacman.use_item", () => HandleInput("pacman.use_item"));
    }

    private void UnregisterKeyActions()
    {
        KeybindDataManager.UnregisterKeyAction("pacman.face_up", () => HandleInput("pacman.face_up"));
        KeybindDataManager.UnregisterKeyAction("pacman.face_down", () => HandleInput("pacman.face_down"));
        KeybindDataManager.UnregisterKeyAction("pacman.face_left", () => HandleInput("pacman.face_left"));
        KeybindDataManager.UnregisterKeyAction("pacman.face_right", () => HandleInput("pacman.face_right"));
        KeybindDataManager.UnregisterKeyAction("pacman.use_item", () => HandleInput("pacman.use_item"));
    }

    private void Update()
    {
        if (!isMazeStarted) return;

        UpdateEffectItemDisplay();
        UpdateTextDisplay();
 
        if (!isRunning && queuedDirection != Vector2.zero)
        {
            direction = queuedDirection;
            targetPosition = (Vector2)pacman.transform.position + direction * TILE_SIZE;
            isRunning = true;
            UpdatePacmanAnimation();
        }
    }

    private void FixedUpdate()
    {
        if (!isMazeStarted) return;
        
        if (isRunning)
        {
            MoveTowards();
        }
    }

    private void InitializePacman()
    {
        Vector2 _pacman_coordinate = IngameDataManager.LoadSpecificData<Vector2>("pacman_data.coordinate");

        pacman.transform.position = (_pacman_coordinate != Vector2.zero) 
            ? new Vector2(Mathf.Round(_pacman_coordinate.x * 100.0f) / 100.0f, Mathf.Round(_pacman_coordinate.y * 100.0f) / 100.0f)
            : new Vector2(Mathf.Round(spawnPoint.position.x * 100.0f) / 100.0f, Mathf.Round(spawnPoint.position.y * 100.0f) / 100.0f);
        
        pacmanCollider = pacman.GetComponent<BoxCollider2D>();

        animator?.SetTrigger("pacman.rest");
    }

    private void HandleInput(string action)
    {
        if (!isMazeStarted) return;
        
        Vector2 _direction = action switch
        {
            "pacman.face_up" => Vector2.up,
            "pacman.face_down" => Vector2.down,
            "pacman.face_left" => Vector2.left,
            "pacman.face_right" => Vector2.right,
            _ => Vector2.zero
        };
        
        if (_direction != Vector2.zero)
        {
            queuedDirection = _direction;
        }
        else if (action == "pacman.use_item")
        {
            OnUse_EffectItem();
        }
    }

    private void MoveTowards()
    {
        Vector2 currentPosition = (Vector2)pacman.transform.position;

        if (currentPosition == targetPosition)
        {
            if (IsAbleToMoveTo(currentPosition + queuedDirection * TILE_SIZE))
            {
                direction = queuedDirection;
                targetPosition = currentPosition + direction * TILE_SIZE;
                
                UpdatePacmanAnimation();
                UpdatePacmanDirection();
            }
            else if (IsAbleToMoveTo(currentPosition + direction * TILE_SIZE))
            {
                targetPosition = currentPosition + direction * TILE_SIZE;
            }
            else
            {
                isRunning = false;
                return;
            }
        }

        if (IsAbleToMoveTo(targetPosition))
        {
            float _pacman_speedMultiplier = IngameDataManager.LoadSpecificData<float>("pacman_data.speed_multiplier");
            float _pacman_windBurstSpeedAffect = IngameDataManager.LoadSpecificData<float>("pacman_data.wind_burst_speed_affect");
            
            Vector2 newPosition = Vector2.MoveTowards(currentPosition, targetPosition, (defaultSpeed * _pacman_speedMultiplier * _pacman_windBurstSpeedAffect) * Time.fixedDeltaTime);
            pacman.transform.position = newPosition;

            if (newPosition == targetPosition)
            {
                UpdatePacmanPosition(newPosition);
            }
        }
        else
        {
            isRunning = false;
        }
    }

    private bool IsAbleToMoveTo(Vector2 targetPosition)
    {
        Bounds pacmanBounds = pacmanCollider.bounds;
        pacmanBounds.center = targetPosition;
        Collider2D[] hits = Physics2D.OverlapBoxAll(pacmanBounds.center, pacmanBounds.size, 0f, collisionLayer);

        foreach (Collider2D hit in hits)
        {
            if (hit != null && hit.gameObject != pacman)
            {
                return false;
            }
        }

        return true;
    }

    private void UpdatePacmanPosition(Vector2 position)
    {
        Vector2 _pacman_coordinate = IngameDataManager.LoadSpecificData<Vector2>("pacman_data.coordinate");
        _pacman_coordinate = position;
        IngameDataManager.SaveSpecificData("pacman_data.coordinate", _pacman_coordinate);
    }

    private void UpdatePacmanDirection()
    {
        Vector2 _pacman_direction = IngameDataManager.LoadSpecificData<Vector2>("pacman_data.direction");
        _pacman_direction = direction;
        IngameDataManager.SaveSpecificData("pacman_data.direction", _pacman_direction);
    }

    private void UpdatePacmanAnimation()
    {
        foreach(AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Trigger)
            {
                animator.ResetTrigger(parameter.name);
            }
        }

        bool _pacman_hasPowerPellet = IngameDataManager.LoadSpecificData<bool>("pacman_data.has_power_pellet");
        
        string animatorId = direction switch
        {
            Vector2 v when v == Vector2.up => (!_pacman_hasPowerPellet) ? "pacman.normal_up" : "pacman.scary_up",
            Vector2 v when v == Vector2.down => (!_pacman_hasPowerPellet) ? "pacman.normal_down" : "pacman.scary_down",
            Vector2 v when v == Vector2.left => (!_pacman_hasPowerPellet) ? "pacman.normal_left" : "pacman.scary_left",
            Vector2 v when v == Vector2.right => (!_pacman_hasPowerPellet) ? "pacman.normal_right" : "pacman.scary_right",
            _ => "pacman.rest"
        };

        animator.SetTrigger(animatorId);
    }

    public void Respawn()
    {
        Vector2 spawnPosition = new Vector2(Mathf.Round(spawnPoint.position.x * 100.0f) / 100.0f, Mathf.Round(spawnPoint.position.y * 100.0f) / 100.0f);
        targetPosition = spawnPosition;
        pacman.transform.position = spawnPosition;
        UpdatePacmanPosition(spawnPosition);

        animator.SetTrigger("pacman.rest");
        UpdatePacmanAnimation();
    }

    /*********************************************************************/
    //                        Effect Items Use
    /*********************************************************************/

    private void UpdateEffectItemDisplay()
    {
        if (!hasEffectItem)
        {
            string _pacman_currentEffectItem = IngameDataManager.LoadSpecificData<string>("pacman_data.current_effect_item");
            if (System.String.IsNullOrEmpty(_pacman_currentEffectItem)) return;

            var effectItem = effectItemList.effectItems.Find(item => item.name == _pacman_currentEffectItem);
            if (effectItem == null) return;

            effectItemIcon.sprite = effectItem.asItemSprite;
            effectItemIcon.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);        
            hasEffectItem = true;
        }
    }

    private void OnUse_EffectItem()
    {
        if (!hasEffectItem) 
        {
            Debug.Log("No effect item found in inventory.");
            return;
        }

        if (Time.time - lastCd_effectItem < cd_effectItem)
        {
            Debug.Log("Effect item use in cooldown.");
            return;
        }

        string _pacman_currentEffectItem = IngameDataManager.LoadSpecificData<string>("pacman_data.current_effect_item");
        
        if (System.String.IsNullOrEmpty(_pacman_currentEffectItem)) return;

        var actions = new Dictionary<string, System.Func<IEnumerator>>
        {
            { "rocket_boost", OnUse_RocketBoost },
            { "zoom_out", OnUse_ZoomOut },
            { "retreat", OnUse_Retreat },
            { "immunity", OnUse_Immunity },
            { "slow_move", OnUse_SlowMove },
            { "paralyze", OnUse_Paralyze },
            { "darkness", OnUse_Darkness },
            { "invert_control", OnUse_InvertControl },
        };

        if (actions.TryGetValue(_pacman_currentEffectItem, out var method))
        {
            StartCoroutine(method());
        }

        hasEffectItem = false;
        effectItemIcon.sprite = null;
        effectItemIcon.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);

        cd_effectItem = effectItemList.effectItems.Find(item => item.name == _pacman_currentEffectItem).cooldown;
        lastCd_effectItem = Time.time;
        
        _pacman_currentEffectItem = null;
        IngameDataManager.SaveSpecificData("pacman_data.current_effect_item", _pacman_currentEffectItem);
    }

    // Rocket Boost

    private IEnumerator OnUse_RocketBoost()
    {
        var item_rocketBoost = effectItemList.effectItems.Find(item => item.name == "rocket_boost");
        const float SPEED_MULTIPLIER_INCREASE = 0.5f;
        
        InEffect_RocketBoost(SPEED_MULTIPLIER_INCREASE);
        yield return new WaitForSeconds(item_rocketBoost.useTime);
        InEffect_RocketBoost(-SPEED_MULTIPLIER_INCREASE);
    }

    private void InEffect_RocketBoost(float increase)
    {
        float _pacman_speedMultiplier = IngameDataManager.LoadSpecificData<float>("pacman_data.speed_multiplier");
        _pacman_speedMultiplier += increase;

        IngameDataManager.SaveSpecificData("pacman_data.speed_multiplier", _pacman_speedMultiplier);
    }

    // Zoom Out

    private IEnumerator OnUse_ZoomOut()
    {
        var item_zoomOut = effectItemList.effectItems.Find(item => item.name == "zoom_out");
        const float VISION_MULTIPLIER_INCREASE = 0.4f;
        const float VISION_DURATION = 1.0f;

        yield return StartCoroutine(InEffect_ZoomOut(VISION_MULTIPLIER_INCREASE, VISION_DURATION));
        yield return new WaitForSeconds(item_zoomOut.useTime);
        yield return StartCoroutine(InEffect_ZoomOut(-VISION_MULTIPLIER_INCREASE, VISION_DURATION));
    }

    private IEnumerator InEffect_ZoomOut(float increase, float duration)
    {
        float _pacman_visionMultiplier = IngameDataManager.LoadSpecificData<float>("pacman_data.vision_multiplier");
        float startValue = _pacman_visionMultiplier;
        float targetValue = _pacman_visionMultiplier + increase;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            _pacman_visionMultiplier = Mathf.Lerp(startValue, targetValue, elapsedTime / duration);
            IngameDataManager.SaveSpecificData("pacman_data.vision_multiplier", _pacman_visionMultiplier);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        _pacman_visionMultiplier = targetValue;
        IngameDataManager.SaveSpecificData("pacman_data.vision_multiplier", _pacman_visionMultiplier);
    }

    // Retreat

    private IEnumerator OnUse_Retreat()
    {
        var item_retreat = effectItemList.effectItems.Find(item => item.name == "retreat");
        const float SPEED_MULTIPLIER_INCREASE = 0.75f;

        InEffect_Retreat(-SPEED_MULTIPLIER_INCREASE);
        yield return new WaitForSeconds(item_retreat.useTime);
        
        InEffect_Retreat(SPEED_MULTIPLIER_INCREASE);
        Respawn();
    }

    private void InEffect_Retreat(float increase)
    {
        float _pacman_speedMultiplier = IngameDataManager.LoadSpecificData<float>("pacman_data.speed_multiplier");
        _pacman_speedMultiplier += increase;

        IngameDataManager.SaveSpecificData("pacman_data.speed_multiplier", _pacman_speedMultiplier);
    }

    // Immunity

    private IEnumerator OnUse_Immunity()
    {
        var item_immunity = effectItemList.effectItems.Find(item => item.name == "immunity");

        InEffect_Immunity(true);
        yield return new WaitForSeconds(item_immunity.useTime);
        InEffect_Immunity(false);
    }

    private void InEffect_Immunity(bool isImmune)
    {
        pacman.GetComponent<SpriteRenderer>().color = isImmune 
            ? new Color(1.0f, 1.0f, 1.0f, 0.5f) 
            : new Color(1.0f, 1.0f, 1.0f, 1.0f);
        
        bool _pacman_isImmuneToGhost = IngameDataManager.LoadSpecificData<bool>("pacman_data.is_immune_to_ghost");
        _pacman_isImmuneToGhost = isImmune;
        
        IngameDataManager.SaveSpecificData("pacman_data.is_immune_to_ghost", _pacman_isImmuneToGhost);
    }

    // Slow Move

    private IEnumerator OnUse_SlowMove()
    {
        var item_slowMove = effectItemList.effectItems.Find(item => item.name == "slow_move");
        const float SPEED_MULTIPLIER_INCREASE = 0.25f;

        InEffect_SlowMove(-SPEED_MULTIPLIER_INCREASE);
        yield return new WaitForSeconds(item_slowMove.useTime);
        InEffect_SlowMove(SPEED_MULTIPLIER_INCREASE);
    }

    private void InEffect_SlowMove(float increase)
    {
        List<string> _ghost_listAlive = IngameDataManager.LoadSpecificData<List<string>>("ghost_data.list_alive");
                
        foreach (string ghostName in _ghost_listAlive)
        {
            float _speedMultiplier = IngameDataManager.LoadSpecificListData<float>("ghost_data.ghost_single_info", ghostName, "speed_multiplier");
            _speedMultiplier += increase;

            IngameDataManager.SaveSpecificListData("ghost_data.ghost_single_info", ghostName, "speed_multiplier", _speedMultiplier);
        }
    }

    // Paralyze

    private IEnumerator OnUse_Paralyze()
    {
        var item_paralyze = effectItemList.effectItems.Find(item => item.name == "paralyze");
        const float SPEED_MULTIPLIER_INCREASE = 0.95f;
        const float PARALYZE_DURATION = 0.5f;
        const int MAX_PARALYZE_COUNT = 4;

        List<string> _ghost_listAlive = IngameDataManager.LoadSpecificData<List<string>>("ghost_data.list_alive");
        string nearestGhost = null;
        float nearestDistance = Mathf.Infinity;
        
        foreach (string ghostName in _ghost_listAlive)
        {
            Vector2 _coordinate = IngameDataManager.LoadSpecificListData<Vector2>("ghost_data.ghost_single_info", ghostName, "coordinate");
            float distance = Vector2.Distance(_coordinate, pacman.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestGhost = ghostName;
            }
        }

        if (nearestGhost == null || nearestDistance == Mathf.Infinity) yield break;
        Debug.Log($"Nearest Ghost: {nearestGhost}");

        int paralyzeCount = 0;
        while (paralyzeCount < MAX_PARALYZE_COUNT)
        {
            InEffect_Paralyze(-SPEED_MULTIPLIER_INCREASE, nearestGhost);
            yield return new WaitForSeconds(PARALYZE_DURATION);
            InEffect_Paralyze(SPEED_MULTIPLIER_INCREASE, nearestGhost);

            yield return new WaitForSeconds((item_paralyze.useTime / 4.0f) - PARALYZE_DURATION);
            paralyzeCount++;
        }
    }

    private void InEffect_Paralyze(float increase, string ghostName)
    {
        float _speedMultiplier = IngameDataManager.LoadSpecificListData<float>("ghost_data.ghost_single_info", ghostName, "speed_multiplier");
        _speedMultiplier += increase;

        IngameDataManager.SaveSpecificListData("ghost_data.ghost_single_info", ghostName, "speed_multiplier", _speedMultiplier);
    }

    // Darkness

    private IEnumerator OnUse_Darkness()
    {
        var item_darkness = effectItemList.effectItems.Find(item => item.name == "darkness");
        const float VISION_MULTIPLIER_INCREASE = 0.1f;
        List<Color> AMBIENT_COLORS = new List<Color>
        {
            new Color(0.0f, 0.0f, 0.0f, 0.8f),
            new Color(0.0f, 0.0f, 0.0f, 0.0f)
        };
        const float DARKNESS_DURATION = 1.0f;

        yield return StartCoroutine(InEffect_Darkness(AMBIENT_COLORS[0], -VISION_MULTIPLIER_INCREASE, DARKNESS_DURATION));
        yield return new WaitForSeconds(item_darkness.useTime);
        yield return StartCoroutine(InEffect_Darkness(AMBIENT_COLORS[1], VISION_MULTIPLIER_INCREASE, DARKNESS_DURATION));
    }

    private IEnumerator InEffect_Darkness(Color changeColor, float increase, float duration)
    {
        float _ghost_visionMultiplier = IngameDataManager.LoadSpecificData<float>("ghost_data.vision_multiplier");
        float startValue = _ghost_visionMultiplier;
        float targetValue = _ghost_visionMultiplier + increase;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            ghostAmbient_darkness.color = Color.Lerp(ghostAmbient_darkness.color, changeColor, elapsedTime / duration);
            
            _ghost_visionMultiplier = Mathf.Lerp(startValue, targetValue, elapsedTime / duration);
            IngameDataManager.SaveSpecificData("ghost_data.vision_multiplier", _ghost_visionMultiplier);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        _ghost_visionMultiplier = targetValue;
        IngameDataManager.SaveSpecificData("ghost_data.vision_multiplier", _ghost_visionMultiplier);
    }

    // Invert Control

    private IEnumerator OnUse_InvertControl()
    {
        var item_invertControl = effectItemList.effectItems.Find(item => item.name == "invert_control");

        InEffect_InvertControl(true);
        yield return new WaitForSeconds(item_invertControl.useTime);
        InEffect_InvertControl(false);
    }

    private void InEffect_InvertControl(bool isInvert)
    {
        bool _ghost_isControlInverted = IngameDataManager.LoadSpecificData<bool>("ghost_data.is_control_inverted");
        _ghost_isControlInverted = isInvert;

        IngameDataManager.SaveSpecificData("ghost_data.is_control_inverted", _ghost_isControlInverted);
    }

    private void UpdateTextDisplay()
    {
        int _pacman_points = IngameDataManager.LoadSpecificData<int>("pacman_data.points"); 
        pointsText.text = _pacman_points.ToString("00,000,000");

        float remainCd_effectItem = Mathf.Max(0f, cd_effectItem - (Time.time - lastCd_effectItem));
        cdText_effectItem.text = remainCd_effectItem > 0 ? $"{remainCd_effectItem:F1}s" : "";
    }
}
