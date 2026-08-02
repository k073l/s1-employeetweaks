using System.Diagnostics;
using System.Reflection;
using MelonLoader;

namespace EmployeeTweaks.Helpers;

public class Logger
{
    public string? Namesection { get; set; }

    public bool RaiseTrace { get; set; }
    public bool RaiseDebug { get; set; }

    private MelonLogger.Instance? _melonLogger;

    public Logger(string? namesection = null)
    {
        if (namesection == null)
        {
            Namesection = typeof(Logger).Namespace?.Split('.').FirstOrDefault();
        }
        else
        {
            if (!string.IsNullOrEmpty(RootNamespace) && !namesection.StartsWith(RootNamespace))
                Namesection = $"{RootNamespace}.{namesection}";
            else
                Namesection = namesection;
        }

        SetupInstance();
    }

    public Logger(MelonLogger.Instance melonLogger)
    {
        var nameField = typeof(MelonLogger.Instance).GetField("Name",
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (nameField != null)
            Namesection = nameField.GetValue(melonLogger) as string;
        SetupInstance();
    }

    public static implicit operator Logger(MelonLogger.Instance melonLogger) => new(melonLogger);

    #region Shorthands

    public void Trace(params object[] args) => Log(LogLevel.Trace, null, args);
    public void Debug(params object[] args) => Log(LogLevel.Debug, null, args);
    public void T(params object[] args) => Log(LogLevel.Trace, null, args);
    public void D(params object[] args) => Log(LogLevel.Debug, null, args);

    public void Info(params object[] args) => Log(LogLevel.Info, null, args);
    public void Msg(params object[] args) => Log(LogLevel.Info, null, args);
    public void I(params object[] args) => Log(LogLevel.Info, null, args);
    public void M(params object[] args) => Log(LogLevel.Info, null, args);

    public void Warning(params object[] args) => Log(LogLevel.Warning, null, args);
    public void Warn(params object[] args) => Log(LogLevel.Warning, null, args);
    public void W(params object[] args) => Log(LogLevel.Warning, null, args);

    public void Error(params object[] args) => Log(LogLevel.Error, null, args);
    public void Err(params object[] args) => Log(LogLevel.Error, null, args);
    public void E(params object[] args) => Log(LogLevel.Error, null, args);

    public void Fatal(params object[] args) => Log(LogLevel.BigError, null, args);
    public void BigError(params object[] args) => Log(LogLevel.BigError, null, args);
    public void F(params object[] args) => Log(LogLevel.BigError, null, args);
    public void BE(params object[] args) => Log(LogLevel.BigError, null, args);


    public void InfoColored(object txtColor, params object[] args) => Log(LogLevel.Info, txtColor, args);
    public void MsgColored(object txtColor, params object[] args) => Log(LogLevel.Info, txtColor, args);
    public void IC(object txtColor, params object[] args) => Log(LogLevel.Info, txtColor, args);
    public void MC(object txtColor, params object[] args) => Log(LogLevel.Info, txtColor, args);


    public void Log(LogLevel level, string message) => Log(level, null, message);

    #endregion

    #region Reflection cache (one-time init)

    private static bool _initialized;
    private static readonly object InitLock = new();

    private static ConstructorInfo? _melonLoggerCtor;

    private static object? _defaultTextColor;
    private static Type? _defaultTextColorType;
    private static object? _melonNsColor;

    private static MethodInfo? _msgMethod;

    private static void EnsureInitialized()
    {
        if (_initialized) return;

        lock (InitLock)
        {
            if (_initialized) return;
            var ml = typeof(MelonLogger);

            var defaultTextField = ml.GetField("DefaultTextColor", BindingFlags.Public | BindingFlags.Static);
            _defaultTextColorType = defaultTextField?.FieldType;
            if (_defaultTextColorType == null)
            {
                MelonLogger.Error("Could not determine the type of DefaultTextColor");
                return;
            }

            _defaultTextColor = defaultTextField?.GetValue(null);

            var mlInstance = typeof(MelonLogger.Instance);
            _melonLoggerCtor = mlInstance.GetConstructor(
                [typeof(string), _defaultTextColorType]
            );

            // Find MelonBase derived type from the current assembly
            var melonBaseType = typeof(MelonBase);
            var currentAssembly = Assembly.GetExecutingAssembly();
            Type[] types;
            try
            {
                types = currentAssembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.OfType<Type>().ToArray();
            }
            var melonDerivedType = types.FirstOrDefault(t => melonBaseType.IsAssignableFrom(t) && !t.IsAbstract);

            var melonType = typeof(Melon<>).MakeGenericType(melonDerivedType ??
                                                            throw new InvalidOperationException(
                                                                "No MelonBase derived type found in the current assembly."));
            var instanceProp = melonType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            if (instanceProp != null)
            {
                var consoleColorProp = instanceProp.PropertyType
                    .GetProperty("ConsoleColor", BindingFlags.Public | BindingFlags.Instance);
                if (consoleColorProp != null)
                    _melonNsColor = consoleColorProp.GetValue(instanceProp.GetValue(null));
            }

            if (_melonNsColor == null)
            {
                // fallback to melonlogger's default ns color
                var defaultNsField = ml.GetField("DefaultMelonColor", BindingFlags.Public | BindingFlags.Static);
                if (defaultNsField != null)
                    _melonNsColor = defaultNsField.GetValue(null);
            }

            _msgMethod = mlInstance.GetMethod("Msg", BindingFlags.Public | BindingFlags.Instance, null,
                [_defaultTextColorType, typeof(string)],
                null);

            if (_melonLoggerCtor == null)
                MelonLogger.Error("Could not find constructor for MelonLogger.Instance(string, <colorType>)");
            if (_defaultTextColor == null)
                MelonLogger.Error("Could not find DefaultTextColor");
            if (_melonNsColor == null)
                MelonLogger.Error("Could not find Melon<>.Instance.ConsoleColor or MelonLogger.DefaultMelonColor");
            if (_msgMethod == null)
                MelonLogger.Error("Could not find MelonLogger.Instance.Msg(<colorType>, string)");

            _initialized = true;
        }
    }

    #endregion

    #region Helpers

    private static string GetCallerInfo()
    {
        var stackTrace = new StackTrace();
        for (var i = 3; i < stackTrace.FrameCount; i++)
        {
            var frame = stackTrace.GetFrame(i);
            var method = frame.GetMethod();
            if (method?.DeclaringType != null)
                return $"{method.DeclaringType.FullName}.{method.Name}";
        }

        return "unknown";
    }

    private void SetupInstance()
    {
        if (string.IsNullOrEmpty(Namesection))
            Namesection = "Logger";

        EnsureInitialized();

        var instance = _melonLoggerCtor?.Invoke([Namesection, _melonNsColor]);
        if (instance == null)
            throw new NullReferenceException("Could not create MelonLogger.Instance");
        _melonLogger = (MelonLogger.Instance)instance;
    }

    private static string? RootNamespace => typeof(Logger).Namespace?.Split('.').FirstOrDefault();

    #endregion

    #region Core dispatch

    private void Log(LogLevel level, object? txtColor, params object[] args)
    {
        if (args.Length == 0) return;

        string message;
        if (args.Length == 1)
            message = args[0]?.ToString() ?? "";
        else
        {
            var format = args[0]?.ToString() ?? "";
            message = string.Format(format, args[1..]);
        }

        var txt = txtColor ?? _defaultTextColor;

        switch (level)
        {
            case LogLevel.Trace:
                var traceMsg = $"[TRACE {GetCallerInfo()}] {message}";
                if (RaiseTrace)
                    _msgMethod?.Invoke(_melonLogger, [txt, traceMsg]);
                else
                    MelonDebug.Msg(traceMsg);

                break;

            case LogLevel.Debug:
                var debugMsg = $"[DEBUG {GetCallerInfo()}] {message}";
                if (RaiseDebug)
                    _msgMethod?.Invoke(_melonLogger, [txt, debugMsg]);
                else
                    MelonDebug.Msg(debugMsg);

                break;

            default:
            case LogLevel.Info:
                _msgMethod?.Invoke(_melonLogger, [txt, message]);
                break;

            case LogLevel.Warning:
                _melonLogger?.Warning(message);
                break;

            case LogLevel.Error:
                _melonLogger?.Error(message);
                break;

            case LogLevel.BigError:
                _melonLogger?.BigError(message);
                break;
        }
    }

    #endregion
}

public enum LogLevel
{
    Trace,
    Debug,
    Info,
    Warning,
    Error,
    BigError,
}