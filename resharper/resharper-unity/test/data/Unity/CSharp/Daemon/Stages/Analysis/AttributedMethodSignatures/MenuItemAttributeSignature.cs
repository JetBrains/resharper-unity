using UnityEditor;

public class ValidSignatures
{
    [MenuItem("MyMenu/Log Selected Transform Name")]
    static void LogSelectedTransformName()
    {
    }

    [MenuItem("MyMenu/Log Selected Transform Name")]
    static void OptionalMenuCommandArgument(MenuCommand command)
    {
    }

    [MenuItem("MyMenu/Log Selected Transform Name", true)]
    static bool ValidateLogSelectedTransformName()
    {
        return Selection.activeTransform != null;
    }

    [MenuItem("MyMenu/Log Selected Transform Name", validate = true)]
    static bool ValidateLogSelectedTransformName(MenuCommand command)
    {
        return Selection.activeTransform != null;
    }

    [MenuItem("MyMenu/Log Selected Transform Name", priority = 100, validate = true)]
    static bool ValidateLogSelectedTransformName()
    {
        return Selection.activeTransform != null;
    }

    [MenuItem("MyMenu/Log Selected Transform Name", priority = 100, validate = true)]
    static bool ValidateLogSelectedTransformName(MenuCommand command)
    {
        return Selection.activeTransform != null;
    }
}

public class MissingStatic
{
    [MenuItem("MyMenu/Log Selected Transform Name")]
    void LogSelectedTransformName()
    {
    }

    [MenuItem("MyMenu/Log Selected Transform Name")]
    void LogSelectedTransformName2(MenuCommand command)
    {
    }

    [MenuItem("MyMenu/Log Selected Transform Name", true)]
    bool ValidateLogSelectedTransformName()
    {
        return Selection.activeTransform != null;
    }

    [MenuItem("MyMenu/Log Selected Transform Name", validate = true)]
    bool ValidateLogSelectedTransformName2()
    {
        return Selection.activeTransform != null;
    }
}

public class IncorrectReturnType
{
    [MenuItem("MyMenu/Log Selected Transform Name")]
    bool LogSelectedTransformName()
    {
    }

    [MenuItem("MyMenu/Log Selected Transform Name")]
    bool LogSelectedTransformName2(MenuCommand menuCommand)
    {
    }

    [MenuItem("MyMenu/Log Selected Transform Name", false)]
    bool ValidateLogSelectedTransformName()
    {
        return Selection.activeTransform != null;
    }

    [MenuItem("MyMenu/Log Selected Transform Name", validate = false)]
    bool ValidateLogSelectedTransformName2()
    {
        return Selection.activeTransform != null;
    }
}

public class DuplicateMenuItemShortCutProblemAnalyzer
{
    [MenuItem("MyMenu/Do Something with a Shortcut Key %g")]
    static void LogSelectedTransformName()
    {
    }

    [MenuItem("MyMenu/Do Something with a Shortcut Key %g")]
    static void LogSelectedTransformName1()
    {
    }

    const string myConst = "MyMenu/Do Something with a Shortcut Key %g";

    [MenuItem(myConst)]
    static void LogSelectedTransformName2()
    {
    }

    [MenuItem($"{myConst}")]
    static void LogSelectedTransformName2()
    {
    }
}
