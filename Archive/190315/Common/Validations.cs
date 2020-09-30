using System;
using System.Windows.Controls;
using System.ComponentModel;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace Common
{
    public class StringRule : ValidationRule
    {
        public int MinLength { get; set; }
        public int MaxLength { get; set; }
        public bool AllowEmpty { get; set; }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            int len = ((string)value).Trim().Length;
            if (len == 0 && !AllowEmpty)
                return new ValidationResult(false, "Пустое значение недопустимо");
            if (len < MinLength || len > MaxLength)
                return new ValidationResult(false, "Длина не укладывается в диапазон");
            return ValidationResult.ValidResult;
        }
        public StringRule()
        {
            MinLength = 0; MaxLength = Int32.MaxValue;
            AllowEmpty = false;
        }
    }
    public class DoubleRule : ValidationRule
    {
        public double MinValue { get; set; }
        public double MaxValue { get; set; }
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            double val = 0;
            try
            {
                val = Convert.ToDouble(value);
                if (val < MinValue || val > MaxValue)
                    return new ValidationResult(false, "Значение не укладывается в диапазон");
                return ValidationResult.ValidResult;
            }
            catch
            {
                return new ValidationResult(false, "Ошибка преобразования");
            }
        }
        public DoubleRule()
        {
            MinValue = float.MinValue; MaxValue = float.MaxValue;
        }
    }
    public class IntRule : ValidationRule
    {
        public int MinValue { get; set; }
        public int MaxValue { get; set; }
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            int val = 0;
            try
            {
                val = Convert.ToInt32(value);
                if (val < MinValue || val > MaxValue)
                    return new ValidationResult(false, "Значение не укладывается в диапазон");
                return ValidationResult.ValidResult;
            }
            catch
            {
                return new ValidationResult(false, "Ошибка преобразования");
            }
        }
        public IntRule()
        {
            MinValue = int.MinValue; MaxValue = int.MaxValue;
        }
    }

    public class ValidationDate : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            try
            {
                string str = Convert.ToString(value).Trim();
                if (str.Length > 0)
                {
                    DateTime dt = Convert.ToDateTime(str);
                    //if (!DateTime.TryParseExact(str, "dd.MM.yyyy", CultureInfo.GetCultureInfo("Ru-ru"), DateTimeStyles.None, out dt))
//                        return new ValidationResult(false, "Ошибка преобразования даты");
                }
            }
            catch
            {
                return new ValidationResult(false, "Ошибка преобразования даты");
            }
            return new ValidationResult(true, null);
        }
    }
    //public class ValidationText : ValidationRule
    //{
    //    public int MaxLength = 255; 
    //    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    //    {
    //        if (((string)value).Trim().Length > MaxLength)
    //                new ValidationResult(false, "Превышение максимальной длины");
    //        return new ValidationResult(true, null);
    //    }
    //}
    //public class PersonValidator : IDataErrorInfo
    //{
    //    public string Error
    //    {
    //        get
    //        {
    //            return this[String.Empty];
    //        }
    //    }
    //    public string this[string propertyName]
    //    {
    //        get
    //        {
    //            return "1111";
    //        }
    //    }
    //}
}
