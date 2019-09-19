using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml;
using Newtonsoft.Json;

namespace SerializationPerformanceTest.Testers
{
    class DataContractBinarySerializationTester<TTestObject> : SerializationTester<TTestObject>
    {
        private readonly DataContractSerializer serializer;
        private readonly XmlBinaryReaderSession xmlBinaryReaderSession;
        private readonly TrackingXmlBinaryWriterSession xmlBinaryWriterSession;
        private XmlDictionaryReader xmlDictionaryReader;
        private XmlDictionaryWriter xmlDictionaryWriter;
        private readonly IXmlDictionary xmlDictionary;
        private int dictionaryId;
        private readonly MemoryStream stream;

        public DataContractBinarySerializationTester(TTestObject testObject)
            : base(testObject)
        {
            serializer = new DataContractSerializer(typeof(TTestObject));
            xmlBinaryReaderSession = new XmlBinaryReaderSession();
            xmlBinaryWriterSession = new TrackingXmlBinaryWriterSession();
            xmlDictionary = new XmlDictionary();
            stream = new MemoryStream(130000);
        }

        protected override TTestObject Deserialize()
        {
            base.MemoryStream.Seek(0, 0);
            if (xmlDictionaryReader == null)
            {
                xmlDictionaryReader = XmlDictionaryReader.CreateBinaryReader(base.MemoryStream, xmlDictionary, XmlDictionaryReaderQuotas.Max, xmlBinaryReaderSession);
            }
            else
            {
                ((IXmlBinaryReaderInitializer)xmlDictionaryReader).SetInput(base.MemoryStream, xmlDictionary, XmlDictionaryReaderQuotas.Max, xmlBinaryReaderSession, null);
            }

            return (TTestObject)serializer.ReadObject(xmlDictionaryReader);
        }
        
        protected override MemoryStream Serialize()
        {
            if (xmlDictionaryWriter == null)
            {
                xmlDictionaryWriter = XmlDictionaryWriter.CreateBinaryWriter(stream, xmlDictionary, xmlBinaryWriterSession, false);
            }
            else
            {
                ((IXmlBinaryWriterInitializer)xmlDictionaryWriter).SetOutput(stream, xmlDictionary, xmlBinaryWriterSession, false);
            }

            serializer.WriteObject(xmlDictionaryWriter, base.TestObject);
            xmlDictionaryWriter.Flush();
            xmlDictionaryWriter.Close();
            if (xmlBinaryWriterSession.HasNewStrings)
            {
                foreach(var newString in xmlBinaryWriterSession.NewStrings)
                {
                    xmlBinaryReaderSession.Add(dictionaryId, newString.Value);
                    dictionaryId++;
                }

                xmlBinaryWriterSession.ClearNew();
            }

            return stream;
        }

        private class TrackingXmlBinaryWriterSession : XmlBinaryWriterSession
        {
            List<XmlDictionaryString> newStrings;

            public bool HasNewStrings
            {
                get { return newStrings != null && newStrings.Count > 0; }
            }

            public IList<XmlDictionaryString> NewStrings => newStrings;

            public void ClearNew()
            {
                newStrings.Clear();
            }

            public override bool TryAdd(XmlDictionaryString value, out int key)
            {
                if (base.TryAdd(value, out key))
                {
                    if (newStrings == null)
                    {
                        newStrings = new List<XmlDictionaryString>();
                    }

                    newStrings.Add(value);
                    return true;
                }

                return false;
            }
        }
    }
}