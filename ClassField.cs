using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace java.serialize
{
    class ClassField
    {
        public enum ValueType
        {
            Byte = 'B',
            Char = 'C',
            Double = 'D',
            Float = 'F',
            Integer = 'I',
            Long = 'J',
            Short = 'S',
            Bool = 'Z',
            ArrayList = '[',
            Object = 'L'
        }

        private readonly ValueType _typeCode;
        private string _name;
        private string _className1;
        private object _value;

        public ClassField(byte typeCode)
        {
            this._typeCode = (ValueType)((char)typeCode);
            this._name = "";
            _className1 = "";
            this._value = null;
        }

        public ClassField(ClassField cf)
        {
            this._typeCode = cf._typeCode;
            this._name = string.Copy(cf._name);
            _className1 = string.Copy(cf._className1);
            this._value = null;
        }

        public byte GetTypeCode()
        {
            return (byte)this._typeCode;
        }

        public void SetName(string name)
        {
            this._name = name;
        }

        public string GetName()
        {
            return string.Copy(this._name);
        }

        public void SetValue(object value)
        {
            _value = value;
        }

        public object GetValue()
        {
            return _value;
        }

        public void SetClassName1(string cn1)
        {
            this._className1 = string.Copy(cn1);
        }

        public override string ToString()
        {
            string val = "";
            
            switch (_typeCode)
            {
                case ValueType.Byte:
                case ValueType.Char:
                case ValueType.Double:
                case ValueType.Float:
                case ValueType.Integer:
                case ValueType.Long:
                case ValueType.Short:
                case ValueType.Bool:
                    val = _value.ToString();
                    break;
                case ValueType.ArrayList:
                    break;
                case ValueType.Object:
                    if (_value == null)
                        val = "null";
                    else if (_value is ClassDataDesc)
                        val = ((ClassDataDesc)_value).ToString();
                    else
                        val = _value.ToString();

                        break;
            }

            return string.Format("(n: {0}, v: {1})", _name, val);
        }
    }
}
