using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace java.serialize
{
    class ClassDataDesc
    {
        private readonly List<ClassDetails> _classDetails;

        public ClassDataDesc()
        {
            this._classDetails = new List<ClassDetails>();
        }

        public ClassDataDesc(ClassDataDesc cdd)
        {
            this._classDetails = new List<ClassDetails>();

            foreach(ClassDetails cd in cdd._classDetails)
            {
                this._classDetails.Add(new ClassDetails(cd));
            }
        }

        private ClassDataDesc(List<ClassDetails> cd)
        {
            this._classDetails = cd;
        }

        public List<ClassDetails> ClassDetails { get { return _classDetails;  } }

        public ClassDataDesc buildClassDataDescFromIndex(int index)
        {
            var cd = new List<ClassDetails>();
            for (int i = index; i < this._classDetails.Count; i++)
                cd.Add(this._classDetails[i]);
            return new ClassDataDesc(cd);
        }

        public void AddSuperClassDesc(ClassDataDesc scdd)
        {
            if (scdd != null)
            {
                for (int i = 0; i < scdd.GetClassCount(); i++)
                {
                    this._classDetails.Add(scdd.GetClassDetails(i));
                }
            }
        }

        public void AddClass(String className)
        {
            this._classDetails.Add(new ClassDetails(className));
        }

        public void SetLastClassHandle(int handle)
        {
            this._classDetails[this._classDetails.Count - 1].SetHandle(handle);
        }

        public void SetLastClassDescFlags(byte classDescFlags)
        {
            this._classDetails[this._classDetails.Count - 1].SetClassDescFlags(classDescFlags);
        }

        public void AddFieldToLastClass(byte typeCode)
        {
            this._classDetails[this._classDetails.Count - 1].AddField(new ClassField(typeCode));
        }

        public void SetLastFieldName(String name)
        {
            this._classDetails[this._classDetails.Count - 1].SetLastFieldName(name);
        }

        public void SetLastFieldClassName1(String cn1)
        {
            this._classDetails[this._classDetails.Count - 1].SetLastFieldClassName1(cn1);
        }

        public ClassDetails GetClassDetails(int index)
        {
            return this._classDetails[index];
        }

        public int GetClassCount()
        {
            return this._classDetails.Count;
        }

        public override string ToString()
        {
            string val = "";

            foreach (var detail in _classDetails)
            {
                val += detail + " ";
            }


            return string.Format("[{0}]", val);
        }
    }
}
