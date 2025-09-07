using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;                 // XR polling (secondaryButton = Y ביד שמאל)
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;       // Input System (אופציונלי)
#endif
using XRCommon = UnityEngine.XR.CommonUsages; // Alias למניעת דו־משמעות

[DisallowMultipleComponent]
public class InstructionsOverlay : MonoBehaviour
{
    [Header("Overlay")]
    [Tooltip("שורש ה-UI של מסך ההוראות (Panel/Canvas).")]
    public GameObject overlayRoot;
    [Tooltip("אם יש CanvasGroup, נשתמש בו ל-alpha וחסימת קליקים (מומלץ).")]
    public CanvasGroup overlayGroup;
    [Tooltip("להציג את מסך ההוראות בתחילת הסצנה?")]
    public bool showOnStart = true;

    [Header("Start Lock (פועל רק עד הסגירה הראשונה)")]
    [Tooltip("לעצור את הזמן בתחילת הסצנה עד לסגירה הראשונה (timeScale = 0)?")]
    public bool pauseTimeScaleAtStart = true;
    [Tooltip("קומפוננטות להשבית עד לסגירה הראשונה (למשל Movement/Turn Providers וכו').")]
    public Behaviour[] behavioursToDisableDuringInitialLock;

    [Header("Hide UI at Start")]
    [Tooltip("אובייקטים להסתיר בתחילת הסצנה (טיימר, ProgressBar וכו'). יופיעו אחרי הסגירה הראשונה.")]
    public GameObject[] objectsToHideUntilFirstClose;

    [Header("Timer (אופציונלי)")]
    [Tooltip("גרור את קומפוננטת הטיימר אם תרצה להתחיל אותו רק אחרי הסגירה הראשונה.")]
    public MonoBehaviour countdownTimer;            // אופציונלי
    public bool startTimerOnFirstClose = true;
    public string startTimerMethod = "StartCountdown";
    public string pauseTimerMethod  = "PauseCountdown";

    [Header("Input (Y = toggle instructions)")]
#if ENABLE_INPUT_SYSTEM
    [Tooltip("InputAction ל-Y (בד\"כ XRI LeftHand/Secondary Button). אם ריק—נשתמש ב-XR polling + מקש Y בעורך.")]
    public InputActionReference yAction;           // אופציונלי
#else
    [SerializeField, Tooltip("מושבת אם לא משתמשים ב-Input System")]
    private Object yActionPlaceholder;
#endif

    [Header("Pause Integration")]
    [Tooltip("אם Pause נסגר ועדיין יש נעילת התחלה – להחזיר את מסך ההוראות.")]
    public bool reopenOverlayAfterPauseIfInitialLock = true;

    [Header("Events")]
    public UnityEvent onFirstClose;                // יורה רק בפעם הראשונה שסוגרים
    public UnityEvent onOpen;                      // כל פעם שנפתח
    public UnityEvent onClose;                     // כל פעם שנסגר

    // --------- Internal state ---------
    bool _visible;
    bool _initialLockActive;                       // true עד הסגירה הראשונה ב-Y
    bool _firstClosedFired;
    bool _pauseActive;                             // Y חסום כש-true (Pause פעיל)
    bool _prevSecondaryLeft;                       // edge detect ל-Y (שלט שמאל)
    float _savedTimeScale = 1f;

    bool _overlayWasOpenBeforePause = false;       // לזכור מה היה מצב ההוראות כש-Pause נפתח

    /// <summary>נכון עד לסגירה הראשונה של מסך ההוראות ב-Y.</summary>
    public bool IsInitialLockActive => _initialLockActive;

#if ENABLE_INPUT_SYSTEM
    void OnEnable()
    {
        if (yAction != null) yAction.action.Enable();
    }
    void OnDisable()
    {
        if (yAction != null) yAction.action.Disable();
    }
#endif

    void Start()
    {
        _initialLockActive = showOnStart;

        // להסתיר UI התחלתיים (טיימר/ProgressBar)
        SetObjectsActive(objectsToHideUntilFirstClose, false);

        // Pause בתחילת הסצנה (רק עד Y ראשון)
        if (_initialLockActive && pauseTimeScaleAtStart)
        {
            _savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        // לנטרל קומפוננטות תנועה/אינטראקציה
        if (_initialLockActive)
            SetBehavioursEnabled(behavioursToDisableDuringInitialLock, false);

        // לעצור טיימר אם ביקשת
        if (_initialLockActive && startTimerOnFirstClose && countdownTimer != null && !string.IsNullOrEmpty(pauseTimerMethod))
            countdownTimer.SendMessage(pauseTimerMethod, SendMessageOptions.DontRequireReceiver);

        // לפתוח את מסך ההוראות עם טעינת החדר
        SetVisible(showOnStart, instant: true);
    }

    void Update()
    {
        if (PressedThisFrame_Y())
            Toggle();
    }

    // --------- Public API ---------
    public void Toggle()
    {
        if (_pauseActive) return; // חסום בזמן Pause

        if (_visible)
        {
            // סגירה ע"י Y — זו כן "סגירה ראשונה" אם זה המצב הראשוני
            CloseOverlay(countAsFirstClose: true);
        }
        else
        {
            // פתיחה (ללא Pause, אחרי הפעם הראשונה אין השפעה על משחק)
            SetVisible(true);
        }
    }

    public void Open()  { if (!_pauseActive) SetVisible(true); }
    public void Close() { CloseOverlay(countAsFirstClose: false); }

    /// <summary>לקרוא ממנהל ה-Pause שלך כש-Pause נפתח.</summary>
    public void NotifyPauseOpened()
    {
        _pauseActive = true;
        _overlayWasOpenBeforePause = _visible;              // נזכור אם ההוראות היו פתוחות
        if (_visible) CloseOverlay(countAsFirstClose: false); // נסגור בלי לשחרר נעילת ההתחלה
    }

    /// <summary>לקרוא ממנהל ה-Pause שלך כש-Pause נסגר.</summary>
    public void NotifyPauseClosed()
    {
        _pauseActive = false;

        // אם עדיין יש נעילת התחלה – נחזיר את מסך ההוראות כדי שלא "ייתקע"
        if (_initialLockActive && reopenOverlayAfterPauseIfInitialLock && _overlayWasOpenBeforePause)
        {
            SetVisible(true);   // מחזיר את ה-Overlay; timeScale עדיין 0 עד לחיצה על Y
        }

        _overlayWasOpenBeforePause = false;
    }

    // --------- Internal ---------
    void CloseOverlay(bool countAsFirstClose)
    {
        SetVisible(false);

        // שחרור ה-lock הראשוני יקרה רק בסגירה ע"י Y (countAsFirstClose=true)
        if (countAsFirstClose && _initialLockActive)
        {
            _initialLockActive = false;

            // לשחרר timeScale
            if (pauseTimeScaleAtStart)
                Time.timeScale = _savedTimeScale;

            // להחזיר קומפוננטות
            SetBehavioursEnabled(behavioursToDisableDuringInitialLock, true);

            // לחשוף UI
            SetObjectsActive(objectsToHideUntilFirstClose, true);

            // להתחיל טיימר אם רלוונטי
            if (startTimerOnFirstClose && countdownTimer != null && !string.IsNullOrEmpty(startTimerMethod))
                countdownTimer.SendMessage(startTimerMethod, SendMessageOptions.DontRequireReceiver);

            if (!_firstClosedFired) { _firstClosedFired = true; onFirstClose?.Invoke(); }
        }
    }

    void SetVisible(bool v, bool instant = false)
    {
        _visible = v;

        if (overlayRoot != null && !overlayRoot.activeSelf && overlayGroup != null)
            overlayRoot.SetActive(true); // נשאיר פעיל אם יש CanvasGroup

        if (overlayGroup != null)
        {
            overlayGroup.alpha = v ? 1f : 0f;
            overlayGroup.blocksRaycasts = v;
            overlayGroup.interactable = v;
        }
        else if (overlayRoot != null)
        {
            overlayRoot.SetActive(v);
        }

        if (v) onOpen?.Invoke(); else onClose?.Invoke();
    }

    void SetObjectsActive(GameObject[] objs, bool active)
    {
        if (objs == null) return;
        foreach (var go in objs)
            if (go) go.SetActive(active);
    }

    void SetBehavioursEnabled(Behaviour[] list, bool enabled)
    {
        if (list == null) return;
        foreach (var b in list)
            if (b) b.enabled = enabled;
    }

    // --------- Input helpers ---------
    bool PressedThisFrame_Y()
    {
        if (_pauseActive) return false; // חסום בזמן Pause

        // 1) Input System
#if ENABLE_INPUT_SYSTEM
        if (yAction != null && yAction.action.WasPressedThisFrame())
            return true;
#endif
        // 2) XR polling: Y = secondaryButton ביד שמאל
        bool secDown = false;
        var left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        if (left.isValid)
            left.TryGetFeatureValue(XRCommon.secondaryButton, out secDown);
        bool pressed = secDown && !_prevSecondaryLeft;
        _prevSecondaryLeft = secDown;
        if (pressed) return true;

        // 3) לעורך – מקש Y
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Y)) return true;
#endif
        return false;
    }
}
