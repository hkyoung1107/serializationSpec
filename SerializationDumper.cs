using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace java.serialize
{
    class SerializationDumper
    {
        private readonly ByteBuffer2 _bb;
        private int _handleValue;
        private string _indent = "";
        private readonly List<ClassDataDesc> _classDataDescriptions;
        private readonly List<ClassDataDesc> _classData;

        private Dictionary<int, object> _globalObjects;

        public List<ClassDataDesc> ClassData
        {
            get { return _classData;  }
            
        }

        public bool InitOk { get; private set; }

        public SerializationDumper(string path)
        {
            InitOk = File.Exists(path);
            var data = File.ReadAllBytes(path);
            _bb = new ByteBuffer2(data);

            if (!InitOk)
                return;

            _indent = "";
            _handleValue = 0x7e0000;
            _classDataDescriptions = new List<ClassDataDesc>();
            _classData = new List<ClassDataDesc>();
            _globalObjects = new Dictionary<int, object>();
        }

        private void increaseIndent()
        {
            _indent += "  ";
        }

        private void decreaseIndent()
        {
            _indent = _indent.Substring(0, _indent.Length - 2);
        }

        private void print(string contents)
        {
            Console.WriteLine(_indent + contents);
        }

        private int newHandle()
        {
            int handleValue = this._handleValue;

            print("newHandle 0xxxx");

            this._handleValue += 1;

            return handleValue;
        }

        public bool ParseStream()
        {
            var b1 = _bb.GetByte();
            var b2 = _bb.GetByte();
            if (b1 != 0xac || b2 != 0xed)
                return false;

            var b3 = _bb.GetInt16BE();

            if (b3 != 5) return false;

            print("Contents");
            increaseIndent();
            while (_bb.RemainedSize > 0)
            {
                var element = readContentElement();
                if (element != null && element is ClassDataDesc)
                {
                    _classData.Add((ClassDataDesc) element);
                }
                else
                {
                    int a = 0;
                    a++;
                }
            }

            decreaseIndent();

            return true;
        }

        private object readContentElement()
        {
            switch (_bb.Peek())
            {
                case 0x73: return readNewObject();
                case 0x76: return readNewClass();
                case 0x75: return readNewArray();
                case 0x74:
                case 0x7C: return readNewString();
                case 0x7E: return readNewEnum();
                case 0x72:
                case 0x7D: return readNewClassDesc();
                case 0x71: return readPrevObject();
                case 0x70: return readNullReference();
                case 0x77: return readBlockData();
                case 0x7A: return readLongBlockData();
                case 0x78:
                case 0x79:
                case 0x7B:
                default:
                    throw new Exception("Error: Illegal content element type.");
            }

            return null;
        }

        private object readNewEnum()
        {
            var b1 = _bb.GetByte();
            print("TC_ENUM - 0x" + string.Format("{0:X2}", b1));
            if (b1 != 126)
            {
                throw new Exception("Error: Illegal value for TC_ENUM (should be 0x7e)");
            }

            increaseIndent();

            readClassDesc();
            newHandle();
            readNewString();

            decreaseIndent();

            throw new Exception("Not Implemented yet!!!");
            return null;
        }

        private ClassDataDesc readNewObject()
        {
            var b1 = _bb.GetByte();
            print("TC_OBJECT - 0x {0}");

            if (b1 != 0x73)
                throw new Exception("Error: Illegal value for TC_OBJECT (should be 0x73)");

            increaseIndent();

            ClassDataDesc cdd = readClassDesc();
            var handle = newHandle();
            var dpCdd = deepCopyClassDesc(cdd);
            readClassData(dpCdd);

            decreaseIndent();
            _globalObjects[handle] = dpCdd;
            return dpCdd;
        }

        private ClassDataDesc readClassDesc()
        {
            switch (_bb.Peek())
            {
                case 0x72:
                case 0x7d:
                    return readNewClassDesc();
                case 0x70:
                    return (ClassDataDesc)readNullReference();
                case 0x71:
                    return (ClassDataDesc)readPrevObject();
                    //int refHandle = readPrevObject();
                    //foreach (ClassDataDesc cdd in this._classDataDescriptions)
                    //{
                    //    for (int classIndex = 0; classIndex < cdd.GetClassCount(); classIndex++)
                    //    {
                    //        if (cdd.GetClassDetails(classIndex).GetHandle() == refHandle)
                    //            return cdd.buildClassDataDescFromIndex(classIndex);
                    //    }
                    //}
                    throw new Exception("Error: Invalid classDesc reference");
            }

            return null;
        }

        private ClassDataDesc deepCopyClassDesc(ClassDataDesc cdd)
        {
            var result = new ClassDataDesc(cdd);

            return result;
        }

        private ClassDataDesc readNewClassDesc()
        {
            switch (_bb.Peek())
            {
                case 0x72:
                    ClassDataDesc cdd = readTC_CLASSDESC();
                    this._classDataDescriptions.Add(cdd);
                    return cdd;
                case 0x7d:
                    return readTC_PROXYCLASSDESC();
            }

            print("Invalid newClassDesc type 0x" + string.Format("{0:X2}", _bb.Peek()));

            throw new Exception("Error illegal newClassDesc type.");
        }

        private ClassDataDesc readTC_CLASSDESC()
        {
            ClassDataDesc cdd = new ClassDataDesc();

            var b1 = _bb.GetByte();
            print("TC_CLASSDESC - 0xb1");
            if (b1 != 0x72)
                throw new Exception("Error: Illegal value for TC_CLASSDESC (should be 0x72)");

            increaseIndent();

            print("className");
            increaseIndent();

            var value = readUtf();
            cdd.AddClass(value);
            decreaseIndent();

            print("serialVersionUID - 0x" + string.Format("{0:X2}", _bb.GetByte()) + " "
                + string.Format("{0:X2}", _bb.GetByte()) + " " + string.Format("{0:X2}", _bb.GetByte()) + " "
                + string.Format("{0:X2}", _bb.GetByte()) + " " + string.Format("{0:X2}", _bb.GetByte()) + " "
                + string.Format("{0:X2}", _bb.GetByte()) + " " + string.Format("{0:X2}", _bb.GetByte()) + " "
                + string.Format("{0:X2}", _bb.GetByte()));

            var handle = newHandle();
            _globalObjects[handle] = cdd;
            //cdd.SetLastClassHandle(handle);
            readClassDescInfo(cdd);

            decreaseIndent();

            return cdd;
        }

        private ClassDataDesc readTC_PROXYCLASSDESC()
        {
            var cdd = new ClassDataDesc();

            var b1 = _bb.GetByte();
            print("TC_PROXYCLASSDESC - 0x" + string.Format("{0:X2}", b1));
            if (b1 != 125)
                throw new Exception("Error: Illegal value for TC_PROXYCLASSDESC (should be 0x7d)");

            increaseIndent();

            var handle = newHandle();
            _globalObjects[handle] = cdd;

            readProxyClassDescInfo(cdd);
            decreaseIndent();

            return cdd;
        }


        private void readClassDescInfo(ClassDataDesc cdd)
        {
            string classDescFlags = "";

            var b1 = _bb.GetByte();
            if ((b1 & 0x1) == 1)
                classDescFlags = classDescFlags + "SC_WRITE_METHOD | ";

            if ((b1 & 0x2) == 2)
                classDescFlags = classDescFlags + "SC_SERIALIZABLE | ";

            if ((b1 & 0x4) == 4)
                classDescFlags = classDescFlags + "SC_EXTERNALIZABLE | ";

            if ((b1 & 0x8) == 8)
                classDescFlags = classDescFlags + "SC_BLOCKDATA | ";

            if (classDescFlags.Length > 0)
                classDescFlags = classDescFlags.Substring(0, classDescFlags.Length - 3);

            print("classDescFlags - 0x" + string.Format("{0:X2}", b1) + " - " + classDescFlags);

            cdd.SetLastClassDescFlags(b1);
            if ((b1 & 0x2) == 2)
            {
                if ((b1 & 0x4) == 4)
                    throw new Exception("Error: Illegal classDescFlags, SC_SERIALIZABLE is not compatible with SC_EXTERNALIZABLE.");

                if ((b1 & 0x8) == 8)
                    throw new Exception("Error: Illegal classDescFlags, SC_SERIALIZABLE is not compatible with SC_BLOCKDATA.");
            }
            else if ((b1 & 0x4) == 4)
            {
                if ((b1 & 0x1) == 1)
                    throw new Exception("Error: Illegal classDescFlags, SC_EXTERNALIZABLE is not compatible with SC_WRITE_METHOD.");
            }
            else if (b1 != 0)
            {
                throw new Exception("Error: Illegal classDescFlags, must include either SC_SERIALIZABLE or SC_EXTERNALIZABLE.");
            }

            readFields(cdd);

            readClassAnnotation();

            readSuperClassDesc();
            //cdd.AddSuperClassDesc(readSuperClassDesc());
        }

        private void readProxyClassDescInfo(ClassDataDesc cdd)
        {
            print("proxyInterfaceNames");

            var b1 = _bb.GetInt32BE();

            increaseIndent();
            for (int i = 0; i < b1; i++)
            {
                print(i + ":");

                readUtf();
                decreaseIndent();
            }

            readClassAnnotation();

            cdd.AddSuperClassDesc(readSuperClassDesc());
        }

        // 클래스 시작과 끝
        private void readClassAnnotation()
        {
            print("classAnnotations");
            increaseIndent();

            while (_bb.Peek() != 120)
            {
                readContentElement();
            }

            _bb.GetByte();
            print("TC_ENDBLOCKDATA - 0x78");

            decreaseIndent();
        }

        private ClassDataDesc readSuperClassDesc()
        {
            print("superClassDesc");
            increaseIndent();

            ClassDataDesc cdd = readClassDesc();

            return cdd;
        }

        private void readFields(ClassDataDesc cdd)
        {
            var count = _bb.GetInt16BE();
            print("fieldCOunt - " + count + " - 0xxxxx");

            if (count > 0)
            {
                print("Fields");
                increaseIndent();

                for (int i = 0; i < count; i++)
                {
                    print(i + ":");
                    increaseIndent();
                    readFieldDesc(cdd);
                    decreaseIndent();
                }

                decreaseIndent();
            }
        }

        private void readFieldDesc(ClassDataDesc cdd)
        {
            var b1 = _bb.GetByte();
            cdd.AddFieldToLastClass(b1);
            switch ((char)b1)
            {
                case 'B':
                    print("Byte - B - 0x" + string.Format("{0:X2}", b1));
                    break;
                case 'C':
                    print("Char - C - 0x" + string.Format("{0:X2}", b1));
                    break;
                case 'D':
                    print("Double - D - 0x" + string.Format("{0:X2}", b1));
                    break;
                case 'F':
                    print("Float - F - 0x" + string.Format("{0:X2}", b1));
                    break;
                case 'I':
                    print("Int - I - 0x" + string.Format("{0:X2}", b1));
                    break;
                case 'J':
                    print("Long - L - 0x" + string.Format("{0:X2}", b1));
                    break;
                case 'S':
                    print("Short - S - 0x" + string.Format("{0:X2}", b1));
                    break;
                case 'Z':
                    print("Bool - Z - 0x" + string.Format("{0:X2}", b1));
                    break;
                case '[':
                    print("Array - [ - 0x" + string.Format("{0:X2}", b1));
                    break;
                case 'L':
                    print("Object - L - 0x" + string.Format("{0:X2}", b1));
                    break;
                default:
                    throw new Exception("Error: Illegal field type code ('"
                        + (char)b1 + "', 0x" + string.Format("{0:X2}", b1) + ")");
            }

            print("fieldName");
            increaseIndent();

            var name = readUtf();
            cdd.SetLastFieldName(name);
            decreaseIndent();

            string value = "";
            if (((char)b1 == '[') || ((char)b1 == 'L'))
            {
                print("className1");
                increaseIndent();

                value = readNewString();
                cdd.SetLastFieldClassName1(value);
                decreaseIndent();
            }
        }

        // readNewObject에서만 사용
        private void readClassData(ClassDataDesc cdd)
        {
            // ClassDetails cd;
            print("<<classdata>>");
            increaseIndent();

            if (cdd != null)
            {
                for (int classIndex = cdd.GetClassCount() - 1; classIndex >= 0; --classIndex)
                {
                    var cd = cdd.GetClassDetails(classIndex);

                    print(cd.ClassName);
                    increaseIndent();

                    if (cd.IsSC_SERIALIZABLE())
                    {
                        print("values - final");
                        increaseIndent();

                        foreach (ClassField cf in cd.GetFields())
                            readClassDataField(cf);

                        decreaseIndent();
                    }

                    // TODO ArrayList의 경우 field값인 size를 지우기 위해 flga 사용
                    if ((cd.IsSC_SERIALIZABLE() && cd.IsSC_WRITE_METHOD()) ||
                        (cd.IsSC_EXTERNALIZABLE() && cd.IsSC_BLOCKDATA()))
                    {
                        int flag = 0;
                        print("objectAnnotation");
                        increaseIndent();
                        while (_bb.Peek() != 0x78)
                        {
                            var field = readContentElement();
                           
                            if (flag == 1)
                            {
                                if (field is ClassField)
                                    cd.AddField((ClassField)field);
                                else
                                    cd.AddField(field);
                            }
                            else
                            {
                                cd.DeleteFields(0);
                            }
                            flag = 1;
                        }

                        _bb.Skip(1);
                        print("TC_ENDBLOCKDATA - 0x78");
                    }

                    if ((cd.IsSC_EXTERNALIZABLE()) && (!cd.IsSC_BLOCKDATA()))
                    {
                        print("externalContents");
                        increaseIndent();
                        print("Unable to parse externalContents as the format is specific to the implementation class.");
                        throw new Exception("Error: Unable to parse externalContents element.");
                    }

                    decreaseIndent();
                }
            }
            else
            {
                print("N/A");
            }

            decreaseIndent();
        }

        private void readClassDataField(ClassField cf)
        {
            print(cf.GetName());
            increaseIndent();
            var value = readFieldValue(cf.GetTypeCode());
            cf.SetValue(value);

            decreaseIndent();
        }

        private object readFieldValue(byte typeCode)
        {
            switch ((char)typeCode)
            {
                case 'B': return readByteField();
                case 'C': return readCharField();
                case 'D': return readDoubleField();
                case 'F': return readFloatField();
                case 'I': return readIntField();
                case 'J': return readLongField();
                case 'S': return readShortField();
                case 'Z': return readBooleanField();
                case '[': return readArrayField();
                case 'L': return readObjectField();
            }

            return null;
        }

        // TODO
        //  return cdd or cd ??
        private object readNewArray()
        {
            var b1 = _bb.GetByte();

            print("TC_ARRAY - 0x" + string.Format("{0:X2}", b1));
            if (b1 != 0x75)
                throw new Exception("Error: Illegal value for TC_ARRAY (should be 0x75)");

            increaseIndent();

            ClassDataDesc cdd = readClassDesc();
            if (cdd.GetClassCount() != 1)
                throw new Exception("Error: Array class description made up of more than one class.");

            ClassDetails cd = cdd.GetClassDetails(0);
            if (cd.ClassName[0] != '[')
                throw new Exception("Error: Array class name does not begin with '['.");

            var handle = newHandle();
            _globalObjects[handle] = cdd;

            var size = _bb.GetInt32BE();

            print("Values");
            increaseIndent();

            for (int i = 0; i < size; i++)
            {
                print("Index " + i + ":");
                increaseIndent();

                var cf = new ClassField((byte)cd.ClassName[1]);
                var value = readFieldValue(cf.GetTypeCode());
                cf.SetValue(value);
                cd.AddField(cf);
                decreaseIndent();
            }

            decreaseIndent();

            decreaseIndent();

            return cd;
        }

        private object readNewClass()
        {
            var b1 = _bb.GetByte();
            print("TC_CLASS - 0x" + string.Format("{0:X2}", b1));

            if (b1 != 118)
                throw new Exception("Error: Illegal value for TC_CLASS (should be 0x76)");

            // TODO check
            var cdd = readClassDesc();

            var handle = newHandle();
            _globalObjects[handle] = cdd;

            return cdd;
        }

        private object readPrevObject()
        {
            var b1 = _bb.GetByte();
            print("TC_REFERENCE - 0x" + string.Format("{0:X2}", b1));

            if (b1 != 0x71)
                throw new Exception("Error: Illegal value for TC_REFERENCE (should be 0x71)");

            increaseIndent();

            var handle = _bb.GetInt32BE();
            print("Handle - " + handle + " - 0xxxxx");

            decreaseIndent();

            return _globalObjects[handle];
            // return handle;
        }


        private object readNullReference()
        {
            var b1 = _bb.GetByte();

            print("TC_NULL - 0x" + string.Format("{0:X2}", b1));
            if (b1 != 0x70)
                throw new Exception("Error: Illegal value for TC_NULL (should be 0x70)");

            return null;
        }

        private object readBlockData()
        {
            string contents = "";

            var b1 = _bb.GetByte();

            print("TC_BLOCKDATA - 0x" + string.Format("{0:X2}", b1));
            if (b1 != 0x77)
                throw new Exception("Error: Illegal value for TC_BLOCKDATA (should be 0x77)");

            increaseIndent();

            int len = _bb.GetByte();

            print("Length - " + len + " - 0x" + string.Format("{0:X2}", (byte)(len & 0xFF)));
            for (int i = 0; i < len; i++)
                contents = contents + string.Format("{0:X2}", _bb.GetByte());

            print("Contents - 0x" + contents);

            return contents;
        }

        private object readLongBlockData()
        {
            string contents = "";

            var b1 = _bb.GetByte();

            print("TC_BLOCKDATALONG - 0x" + string.Format("{0:X2}", b1));
            if (b1 != 0x7a)
                throw new Exception("Error: Illegal value for TC_BLOCKDATA (should be 0x77)");

            increaseIndent();

            var len = _bb.GetInt32BE();
            print("Length - " + len + " - 0x" + string.Format("{0:X2}", (byte)(len & 0xFF)));

            for (long l = 0L; l < len; l += 1L)
                contents = contents + string.Format("{0:X2}", _bb.GetByte());

            print("Contents - 0x" + contents);

            decreaseIndent();

            return contents;
        }

        private string readNewString()
        {
            switch (_bb.Peek())
            {
                case 0x74:
                    return readTC_STRING();
                case 124:
                    return readTC_LONGSTRING();
                case 113:
                    readPrevObject();
                    return "[TC_REF]";
            }

            print("Invalid newString type 0x" + string.Format("{0:X2}", _bb.Peek()));
            throw new Exception("Error illegal newString type.");
        }

        private string readTC_STRING()
        {
            var b1 = _bb.GetByte();

            print("TC_string - 0x" + string.Format("{0:X2}", b1));

            increaseIndent();

            var val = readUtf();

            var handle = newHandle();
            _globalObjects[handle] = val;

            decreaseIndent();

            return val;
        }

        private string readTC_LONGSTRING()
        {
            var b1 = _bb.GetByte();

            print("TC_LONGSTRING - 0x" + string.Format("{0:X2}", b1));
            if (b1 != 0x7c)
            {
                throw new Exception("Error: Illegal value for TC_LONGSTRING (should be 0x7c)");
            }

            increaseIndent();
            
            string val = readLongUtf();

            var handle = newHandle();
            _globalObjects[handle] = val;

            decreaseIndent();

            return val;
        }

        private string readUtf()
        {
            string content = "";
            string hex = "";

            var len = _bb.GetInt16BE();
            print("Length - " + len + " - 0x" + string.Format("{0:X2}", (byte)(len & 0xFF)));
            for (int i = 0; i < len; i++)
            {
                var b1 = _bb.GetByte();
                content = content + (char)b1;
                hex = hex + string.Format("{0:X2}", b1);
            }
            print("Value - " + content + " - 0x" + hex);

            return content;
        }

        private string readLongUtf()
        {
            string content = "";
            string hex = "";

            var len = _bb.GetInt64BE();
            for (long l = 0L; l < len; l += 1L)
            {
                var b1 = _bb.GetByte();
                content = content + b1;
                hex = hex + string.Format("{0:X2}", b1);
            }

            print("Value - " + content + " - 0x" + hex);

            return content;
        }


        private byte readByteField()
        {
            var b1 = _bb.GetByte();
            if ((b1 >= 32) && (b1 <= 126))
            {
                print("(byte)" + b1 + " (ASCII: " + (char)b1 + ") - 0x" + string.Format("{0:X2}", b1));
            }
            else
            {
                print("(byte)" + b1 + " - 0x" + string.Format("{0:X2}", b1));
            }

            return b1;
        }

        private char readCharField()
        {
            var b1 = _bb.GetByte();
            print("(char)" + (char)b1 + " - 0x" + string.Format("{0:X2}", b1));

            return (char)b1;
        }

        private double readDoubleField()
        {
            var b = _bb.GetDouble();
            var b1 = Convert.ToInt32(b);
            var b2 = Convert.ToInt32(b);

            print("(double)xxxxxxxxxxxxxxxxxxxxxxx");
            // print("(double)" + string.Format(" 0x{0:X2}", b1));

            return b;
        }

        private float readFloatField()
        {
            // TODO later
            var b = _bb.GetInt32BE();
            print("(float)xxxxxxxxxxxxxxxxxxxxxxx");
            //print("(float)" + string.Format(" 0x{0:X2}", b));

            return (float)b;
        }

        private int readIntField()
        {
            var b = _bb.GetInt32BE();
            print("(int)" + string.Format(" 0x{0:X2}", b));

            return b;
        }

        private long readLongField()
        {
            var b = _bb.GetInt64BE();
            print("(long)" + string.Format(" 0x{0:X2}", b));

            return b;
        }

        private short readShortField()
        {
            var b1 = _bb.GetInt16BE();
            print("(short)" + " " + string.Format("0x {0:X2}", b1));

            return b1;
        }

        private bool readBooleanField()
        {
            var b1 = _bb.GetByte();
            print("(boolean)" + (b1 == 0 ? "false" : "true") + " - 0x" + string.Format("{0:X2}", b1));

            return b1 != 0;
        }

        private object readArrayField()
        {
            print("(array)");
            increaseIndent();

            switch (_bb.Peek())
            {
                case 112: return readNullReference();
                case 117: return readNewArray();
                case 113: return readPrevObject();
            }

            decreaseIndent();
            return null;
        }

        private object readObjectField()
        {
            print("(object)");
            increaseIndent();
            switch (_bb.Peek())
            {
                case 0x73: return readNewObject();
                case 0x71: return readPrevObject();
                case 0x70: return readNullReference();
                case 0x74: return readTC_STRING();
                case 0x76: return readNewClass();
                case 0x75: return readNewArray();
                default:
                    return null;
                // throw new Exception("Error: Unexpected identifier for object field value 0x" + _bb.Peek());
            }

            decreaseIndent();

            return null;
        }
    }


}
