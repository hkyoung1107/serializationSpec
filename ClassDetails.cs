using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace java.serialize
{
    class ClassDetails
    {
        private readonly string _className;
        private int _refHandle;
        private byte _classDescFlags;
        private readonly List<ClassField> _fieldDescriptions;

        public ClassDetails(string className)
        {
            this._className = className;
            this._refHandle = -1;
            this._classDescFlags = 0;
            this._fieldDescriptions = new List<ClassField>();
        }

        public ClassDetails(ClassDetails cd)
        {
            this._className = string.Copy(cd.ClassName);
            this._refHandle = cd.GetHandle();
            this._classDescFlags = cd._classDescFlags;

            this._fieldDescriptions = new List<ClassField>();
            foreach (ClassField cf in cd.GetFields())
            {
                this._fieldDescriptions.Add(new ClassField(cf));
            }
        }

        public string ClassName
        {
            get { return this._className; }
        }

        //public ClassField this[int index]
        //{
        //    get { return this._fieldDescriptions[index]; }
        //}

        public void SetHandle(int handle)
        {
            this._refHandle = handle;
        }

        public int GetHandle()
        {
            return this._refHandle;
        }

        public void SetClassDescFlags(byte classDescFlags)
        {
            this._classDescFlags = classDescFlags;
        }

        public bool IsSC_SERIALIZABLE()
        {
            return (this._classDescFlags & 0x2) == 2;
        }

        public bool IsSC_EXTERNALIZABLE()
        {
            return (this._classDescFlags & 0x4) == 4;
        }

        public bool IsSC_WRITE_METHOD()
        {
            return (this._classDescFlags & 0x1) == 1;
        }

        public bool IsSC_BLOCKDATA()
        {
            return (this._classDescFlags & 0x8) == 8;
        }

        public void AddField(ClassField cf)
        {
            this._fieldDescriptions.Add(cf);
        }

        public List<ClassField> GetFields()
        {
            return this._fieldDescriptions;
        }

        public void DeleteFields(int index)
        {
            _fieldDescriptions.RemoveAt(index);
        }

    public void SetLastFieldName(string name)
        {
            ((ClassField)this._fieldDescriptions[this._fieldDescriptions.Count-1]).SetName(name);
        }

        public void SetLastFieldClassName1(string cn1)
        {
            ((ClassField)this._fieldDescriptions[this._fieldDescriptions.Count - 1]).SetClassName1(cn1);
        }

        public override string ToString()
        {
            string val = "";

            foreach (var cf in _fieldDescriptions)
            {
                val += cf + " ";
            }

            return string.Format("classname\"{0}\" <{1}>", ClassName, val);
        }
    }

}
