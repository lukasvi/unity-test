﻿/*     INFINITY CODE 2013-2018      */
/*   http://www.infinity-code.com   */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using UnityEngine;

/// <summary>
/// The wrapper for JSON value.
/// </summary>
public class OnlineMapsJSONValue : OnlineMapsJSONItem
{
    public override OnlineMapsJSONItem this[string key]
    {
        get { return null; }
    }

    public override OnlineMapsJSONItem this[int index]
    {
        get { return null; }
    }

    /// <summary>
    /// Gets / sets the current value
    /// </summary>
    public object value
    {
        get { return _value; }
        set
        {
#if !UNITY_WP_8_1 || UNITY_EDITOR
            if (value == null || value is DBNull)
#else
            if (value == null)
#endif
            {
                _type = ValueType.NULL;
                _value = value;
            }
            else if (value is string)
            {
                _type = ValueType.STRING;
                _value = value;
            }
            else if (value is double)
            {
                _type = ValueType.DOUBLE;
                _value = (double)value;
            }
            else if (value is float)
            {
                _type = ValueType.DOUBLE;
                _value = (double)(float)value;
            }
            else if (value is bool)
            {
                _type = ValueType.BOOLEAN;
                _value = value;
            }
            else if (value is long)
            {
                _type = ValueType.LONG;
                _value = value;
            }
            else if (value is int || value is short || value is byte)
            {
                _type = ValueType.LONG;
                _value = Convert.ChangeType(value, typeof(long));
            }
            else throw new Exception("Unknown type of value.");
        }
    }

    /// <summary>
    /// Get the type of value
    /// </summary>
    public ValueType type
    {
        get { return _type; }
    }

    private ValueType _type;
    private object _value;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="value">Value</param>
    public OnlineMapsJSONValue(object value)
    {
        this.value = value;
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="value">Value</param>
    /// <param name="type">Type of value</param>
    public OnlineMapsJSONValue(object value, ValueType type)
    {
        _value = value;
        _type = type;
    }

    public override object Deserialize(Type type)
    {
        return Value(type);
    }

    public override OnlineMapsJSONItem GetAll(string key)
    {
        return null;
    }

    public override void ToJSON(StringBuilder b)
    {
        if (_type == ValueType.STRING) WriteString(b);
        else if (_type == ValueType.NULL) b.Append("null");
        else if (_type == ValueType.BOOLEAN) b.Append((bool) _value ? "true" : "false");
        else b.Append(value);
    }

    public override IEnumerator<OnlineMapsJSONItem> GetEnumerator()
    {
        yield return this;
    }

    public override string ToString()
    {
        return value.ToString();
    }

    public override object Value(Type t)
    {
        if (_type == ValueType.NULL || _value == null)
        {
            if (OnlineMapsReflectionHelper.IsValueType(t)) return Activator.CreateInstance(t);
            return null;
        }

        if (t == typeof(string)) return Convert.ChangeType(_value, t);

        if (_type == ValueType.BOOLEAN)
        {
            if (t == typeof(bool)) return Convert.ChangeType(_value, t);
        }
        else if (_type == ValueType.DOUBLE)
        {
            if (t == typeof(double)) return Convert.ChangeType(_value, t);
            if (t == typeof(float)) return Convert.ChangeType((double)_value, t);
        }
        else if (_type == ValueType.LONG)
        {
            if (t == typeof(long)) return Convert.ChangeType(_value, t);

            try
            {
                return Convert.ChangeType((long)_value, t);
            }
            catch (Exception e)
            {
                Debug.Log(e.Message + "\n" + e.StackTrace);
                return null;
            }
        }
        else if (_type == ValueType.STRING)
        {
            MethodInfo method = OnlineMapsReflectionHelper.GetMethod(t, "Parse", new[] { typeof(string) });
            if (method != null) return method.Invoke(null, new[] {_value});
            return null;
        }
        StringBuilder builder = new StringBuilder();
        ToJSON(builder);
        throw new InvalidCastException(t.FullName + "\n" + builder);
    }

    public override T Value<T>()
    {
        return (T) Value(typeof (T));
    }

    private void WriteString(StringBuilder b)
    {
        b.Append('\"');

        string s = value as string;

        int runIndex = -1;
        int l = s.Length;
        for (var index = 0; index < l; ++index)
        {
            var c = s[index];

            if (c >= ' ' && c < 128 && c != '\"' && c != '\\')
            {
                if (runIndex == -1) runIndex = index;

                continue;
            }

            if (runIndex != -1)
            {
                b.Append(s, runIndex, index - runIndex);
                runIndex = -1;
            }

            switch (c)
            {
                case '\t': b.Append("\\t"); break;
                case '\r': b.Append("\\r"); break;
                case '\n': b.Append("\\n"); break;
                case '"':
                case '\\': b.Append('\\'); b.Append(c); break;
                default:
                    b.Append("\\u");
                    b.Append(((int)c).ToString("X4", NumberFormatInfo.InvariantInfo));
                    break;
            }
        }

        if (runIndex != -1) b.Append(s, runIndex, s.Length - runIndex);
        b.Append('\"');
    }

    public static implicit operator string(OnlineMapsJSONValue val)
    {
        return val.ToString();
    }

    /// <summary>
    /// Type of value
    /// </summary>
    public enum ValueType
    {
        DOUBLE,
        LONG,
        STRING,
        BOOLEAN,
        NULL
    }
}