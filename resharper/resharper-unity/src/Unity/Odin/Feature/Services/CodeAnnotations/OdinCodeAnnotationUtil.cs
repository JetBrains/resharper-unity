using System;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Odin.Feature.Services.CodeAnnotations;

public class OdinCodeAnnotationUtil
{
    public static bool IsApplicable(ConstantValue constantValue)
    {
        if (constantValue.IsDouble())
            return true;

        if (constantValue.IsFloat())
            return true;

        if (constantValue.IsInteger())
            return true;

        if (constantValue.IsString())
            return true;
        
        return false;
    }

    public static long GetMaxValue(ConstantValue constantValue)
    {
        if (constantValue.IsDouble())
            return Convert.ToInt64(Math.Ceiling(constantValue.DoubleValue));
        
        if (constantValue.IsFloat())
            return Convert.ToInt64(Math.Ceiling(constantValue.FloatValue));

        if (constantValue.IsInteger())
            return constantValue.IntValue;

        if (constantValue.IsString())
            return long.MaxValue;

        throw new InvalidOperationException("Unexpected constant");
    }
    
    public static long GetMinValue(ConstantValue constantValue)
    {
        if (constantValue.IsDouble())
            return Convert.ToInt64(Math.Floor(constantValue.DoubleValue));
        
        if (constantValue.IsFloat())
            return Convert.ToInt64(Math.Floor(constantValue.FloatValue));

        if (constantValue.IsInteger())
            return constantValue.IntValue;

        if (constantValue.IsString())
            return long.MinValue;

        throw new InvalidOperationException("Unexpected constant");
    }
}