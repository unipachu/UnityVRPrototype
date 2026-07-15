using TMPro;
using UnityEngine;

/// <summary>
/// Used to show console logs in a TMP Text object.
/// </summary>
public class ConsoleLogsToTMPText : MonoBehaviour
{
    [SerializeField] int MaxConsoleTextLength = 1000;

    [Header("Log Filtering")]
    [SerializeField] bool showLogMessages = true;
    [SerializeField] bool showLogWarnings = true;
    [SerializeField] bool showLogErrors = true;
    [SerializeField] bool showLogExceptions = true;
    [SerializeField] bool showLogAsserts = true;

    [Header("Refs")]
    [Tooltip("Text where we want the console logs to be displayed.")]
    [SerializeField] TMP_Text consoleLogsText;

    void OnEnable()
    {
        Application.logMessageReceived += OnLogMessageRecieved;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= OnLogMessageRecieved;
    }

    void OnLogMessageRecieved(string logString, string stackTrace, LogType logType)
    {
        if (!ShouldDisplayLog(logType))
            return;

        UpdateConsoleTextColor(logType);

        consoleLogsText.text = logString + "\n" + consoleLogsText.text;

        if (consoleLogsText.text.Length > MaxConsoleTextLength)
        {
            consoleLogsText.text = consoleLogsText.text.Substring(0, MaxConsoleTextLength);
        }
    }

    bool ShouldDisplayLog(LogType logType)
    {
        return logType switch
        {
            LogType.Log => showLogMessages,
            LogType.Warning => showLogWarnings,
            LogType.Error => showLogErrors,
            LogType.Exception => showLogExceptions,
            LogType.Assert => showLogAsserts,
            _ => false
        };
    }

    /// <summary>
    /// Set text color to yellow if logType is a warning and the text is not red. If the logType is an error, an exception, or an assert, set the text color to red.
    /// </summary>
    void UpdateConsoleTextColor(LogType logType)
    {
        if (logType == LogType.Warning && consoleLogsText.color != Color.red)
        {
            consoleLogsText.color = Color.yellow;
        }
        else if (logType == LogType.Error ||
                 logType == LogType.Exception ||
                 logType == LogType.Assert)
        {
            consoleLogsText.color = Color.red;
        }
    }
}
