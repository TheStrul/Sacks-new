using System.Diagnostics;

namespace ModernWinForms.Animation;

/// <summary>
/// Provides smooth animation support for control properties.
/// </summary>
public sealed class AnimationEngine : IDisposable
{
    private readonly Control _control;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly Stopwatch _stopwatch = new();
    private double _startValue;
    private double _targetValue;
    private double _currentValue;
    private int _duration;
    private Action<double>? _updateCallback;
    private Action? _completedCallback;
    private EasingFunction _easingFunction = EasingFunctions.EaseOutCubic;
    private bool _isRunning;

    /// <summary>
    /// Easing function delegate.
    /// </summary>
    /// <param name="t">Time progress (0 to 1).</param>
    /// <returns>Eased value (0 to 1).</returns>
    public delegate double EasingFunction(double t);

    /// <summary>
    /// Initializes a new instance of the AnimationEngine class.
    /// </summary>
    /// <param name="control">The control to animate.</param>
    public AnimationEngine(Control control)
    {
        _control = control ?? throw new ArgumentNullException(nameof(control));
        _timer = new System.Windows.Forms.Timer { Interval = 16 }; // ~60 FPS
        _timer.Tick += OnTimerTick;
    }

    /// <summary>
    /// Gets the current animated value.
    /// </summary>
    public double CurrentValue => _currentValue;

    /// <summary>
    /// Gets whether the animation is currently running.
    /// </summary>
    public bool IsRunning => _isRunning;

    /// <summary>
    /// Starts an animation from current value to target value.
    /// </summary>
    /// <param name="targetValue">The target value.</param>
    /// <param name="durationMs">Duration in milliseconds.</param>
    /// <param name="updateCallback">Called on each frame with current value.</param>
    /// <param name="completedCallback">Called when animation completes.</param>
    /// <param name="easingFunction">Optional easing function.</param>
    public void Animate(
        double targetValue,
        int durationMs,
        Action<double> updateCallback,
        Action? completedCallback = null,
        EasingFunction? easingFunction = null)
    {
        Stop();

        _startValue = _currentValue;
        _targetValue = targetValue;
        _duration = durationMs;
        _updateCallback = updateCallback ?? throw new ArgumentNullException(nameof(updateCallback));
        _completedCallback = completedCallback;
        _easingFunction = easingFunction ?? EasingFunctions.EaseOutCubic;
        _isRunning = true;

        _stopwatch.Restart();
        _timer.Start();
    }

    /// <summary>
    /// Stops the current animation.
    /// </summary>
    public void Stop()
    {
        if (_isRunning)
        {
            _timer.Stop();
            _stopwatch.Stop();
            _isRunning = false;
        }
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        var elapsed = _stopwatch.ElapsedMilliseconds;
        if (elapsed >= _duration)
        {
            _currentValue = _targetValue;
            _updateCallback?.Invoke(_currentValue);
            Stop();
            _completedCallback?.Invoke();
            _control.Invalidate();
            return;
        }

        var progress = (double)elapsed / _duration;
        var easedProgress = _easingFunction(progress);
        _currentValue = _startValue + ((_targetValue - _startValue) * easedProgress);
        _updateCallback?.Invoke(_currentValue);
        _control.Invalidate();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Stop();
        _timer.Tick -= OnTimerTick;
        _timer.Dispose();
    }
}

/// <summary>
/// Common easing functions for animations.
/// </summary>
public static class EasingFunctions
{
    /// <summary>
    /// Linear easing (no acceleration).
    /// </summary>
    public static double Linear(double t) => t;

    /// <summary>
    /// Ease out cubic (deceleration).
    /// </summary>
    public static double EaseOutCubic(double t) => 1 - Math.Pow(1 - t, 3);

    /// <summary>
    /// Ease in cubic (acceleration).
    /// </summary>
    public static double EaseInCubic(double t) => t * t * t;

    /// <summary>
    /// Ease in-out cubic (acceleration then deceleration).
    /// </summary>
    public static double EaseInOutCubic(double t) =>
        t < 0.5 ? 4 * t * t * t : 1 - Math.Pow(-2 * t + 2, 3) / 2;

    /// <summary>
    /// Ease out quad (gentle deceleration).
    /// </summary>
    public static double EaseOutQuad(double t) => 1 - (1 - t) * (1 - t);

    /// <summary>
    /// Ease in quad (gentle acceleration).
    /// </summary>
    public static double EaseInQuad(double t) => t * t;

    /// <summary>
    /// Ease out back (overshoot and settle).
    /// </summary>
    public static double EaseOutBack(double t)
    {
        const double c1 = 1.70158;
        const double c3 = c1 + 1;
        return 1 + (c3 * Math.Pow(t - 1, 3)) + (c1 * Math.Pow(t - 1, 2));
    }
}
