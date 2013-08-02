//#define GENERATE_DEBUGGING_ASSEMBLY

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Diagnostics;

namespace Kngine.Serializer
{
    public partial class Serializer
    {
        Dictionary<Type, ushort> s_typeIDMap;

        delegate void SerializerSwitch(Stream stream, object ob, Serializer s);
        delegate void DeserializerSwitch(Stream stream, out object ob, Serializer s);

        SerializerSwitch s_serializerSwitch;
        DeserializerSwitch s_deserializerSwitch;

        bool s_initialized;

        /// <summary>
        /// Serialize string as UTF16 to avoid encoding / decoding. Just convert byte[] to char[]
        /// </summary>
        public bool SerializeStringAsUTF16 = false;

        /*//////////////////////////////////////////////////////////////////////////////////////////////////*/

        public void Uninitialize()
        {
            s_serializerSwitch = null;
            s_deserializerSwitch = null;
            s_typeIDMap = null;
            s_initialized = false;
        }

        public void Initialize(params Type[] rootTypes)
        {
            if (s_initialized) throw new InvalidOperationException("NetSerializer already initialized");

            var types = CollectTypes(rootTypes);

            //GenerateAssembly(types);

            s_typeIDMap = GenerateDynamic(types);

            s_initialized = true;
        }


        public void Initialize(bool generatedebugAssembly, params Type[] rootTypes)
        {
            if (s_initialized) throw new InvalidOperationException("NetSerializer already initialized");

            var types = CollectTypes(rootTypes);

            if (generatedebugAssembly) GenerateAssembly(types);

            s_typeIDMap = GenerateDynamic(types);

            s_initialized = true;
        }

        /*//////////////////////////////////////////////////////////////////////////////////////////////////*/

        public byte[] Serialize(object data)
        {
            MemoryStream MS = new MemoryStream();
            Serialize(MS, data);
            return MS.ToArray();
        }

        public void Serialize(Stream stream, object data)
        {
            if (!s_initialized) throw new InvalidOperationException("NetSerializer not initialized");

            s_serializerSwitch(stream, data, this);
        }


        public object Deserialize(Stream stream)
        {
            if (!s_initialized) throw new InvalidOperationException("NetSerializer not initialized");

            object o;
            s_deserializerSwitch(stream, out o, this);
            return o;
        }

        public object Deserialize(byte[] data)
        {
            if (!s_initialized) throw new InvalidOperationException("NetSerializer not initialized");

            var MS = new MemoryStream(data, false);

            object o;
            s_deserializerSwitch(MS, out o, this);
            return o;
        }

        /*//////////////////////////////////////////////////////////////////////////////////////////////////*/

        static void CollectTypes(Type type, HashSet<Type> typeSet)
        {
            if (typeSet.Contains(type))
                return;

            if (type.IsAbstract)
                return;

            if (type.IsInterface)
                return;

            if (!type.IsSerializable)
                throw new NotSupportedException(String.Format("Type {0} is not marked as Serializable", type.FullName));

            typeSet.Add(type);

            if (type.IsArray)
            {
                CollectTypes(type.GetElementType(), typeSet);
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                var args = type.GetGenericArguments();

                Debug.Assert(args.Length == 2);

                // Dictionary<K,V> is stored as KeyValuePair<K,V>[]

                var arrayType = typeof(KeyValuePair<,>).MakeGenericType(args).MakeArrayType();

                CollectTypes(arrayType, typeSet);
            }
            else
            {
                var fields = GetFieldInfos(type);

                foreach (var field in fields)
                    CollectTypes(field.FieldType, typeSet);
            }
        }

        static Type[] CollectTypes(Type[] rootTypes, bool order = true)
        {
            var primitives = new Type[] {
				typeof(bool),
				typeof(byte), typeof(sbyte),
				typeof(char),
				typeof(ushort), typeof(short),
				typeof(uint), typeof(int),
				typeof(ulong), typeof(long),
				typeof(float), typeof(double),
				typeof(string),
			};

            var typeSet = new HashSet<Type>(primitives);

            foreach (var type in rootTypes)
                CollectTypes(type, typeSet);

            if (order)
            {
                return typeSet
                    .OrderBy(t => t.FullName, StringComparer.Ordinal)
                    .ToArray();
            }
            else
                return typeSet.ToArray();
        }

        Dictionary<Type, TypeData> GenerateTypeData(Type[] types)
        {
            var map = new Dictionary<Type, TypeData>(types.Length);

            // TypeID 0 is reserved for null
            ushort typeID = 1;
            foreach (var type in types)
            {
                var writer = Primitives.GetWritePrimitive(type);
                var reader = Primitives.GetReadPrimitive(type);

                if ((writer != null) != (reader != null))
                    throw new InvalidOperationException(String.Format("Missing a read or write primitive for {0}", type.FullName));

                var isStatic = writer != null;

                if (type.IsPrimitive && isStatic == false)
                    throw new InvalidOperationException(String.Format("Missing primitive read/write methods for {0}", type.FullName));

                var td = new TypeData(typeID++);

                if (isStatic)
                {
                    td.WriterMethodInfo = writer;
                    td.ReaderMethodInfo = reader;
                    td.IsDynamic = false;
                }
                else
                {
                    if (typeof(System.Runtime.Serialization.ISerializable).IsAssignableFrom(type))
                        throw new InvalidOperationException(String.Format("Cannot serialize {0}: ISerializable not supported", type.FullName));

                    td.IsDynamic = true;
                }

                map[type] = td;
            }

            return map;
        }

        Dictionary<Type, ushort> GenerateDynamic(Type[] types)
        {
            Dictionary<Type, TypeData> map = GenerateTypeData(types);

            var nonStaticTypes = map.Where(kvp => kvp.Value.IsDynamic).Select(kvp => kvp.Key).ToArray();

            /* generate stubs */
            foreach (var type in nonStaticTypes)
            {
                var dm = SerializtionCodeGen.GenerateDynamicSerializerStub(type);
                map[type].WriterMethodInfo = dm;
                map[type].WriterILGen = dm.GetILGenerator();
            }

            foreach (var type in nonStaticTypes)
            {
                var dm = DeserializtionCodeGen.GenerateDynamicDeserializerStub(type);
                map[type].ReaderMethodInfo = dm;
                map[type].ReaderILGen = dm.GetILGenerator();
            }

            var serializerSwitchMethod = new DynamicMethod("SerializerSwitch", null,
                new Type[] { typeof(Stream), typeof(object), typeof(Serializer) },
                typeof(Serializer), true);
            serializerSwitchMethod.DefineParameter(1, ParameterAttributes.None, "stream");
            serializerSwitchMethod.DefineParameter(2, ParameterAttributes.None, "value");
            serializerSwitchMethod.DefineParameter(3, ParameterAttributes.None, "s");
            var serializerSwitchMethodInfo = serializerSwitchMethod;

            var deserializerSwitchMethod = new DynamicMethod("DeserializerSwitch", null,
                new Type[] { typeof(Stream), typeof(object).MakeByRefType(), typeof(Serializer) },
                typeof(Serializer), true);
            deserializerSwitchMethod.DefineParameter(1, ParameterAttributes.None, "stream");
            deserializerSwitchMethod.DefineParameter(2, ParameterAttributes.Out, "value");
            deserializerSwitchMethod.DefineParameter(3, ParameterAttributes.None, "s");
            var deserializerSwitchMethodInfo = deserializerSwitchMethod;

            var ctx = new CodeGenContext(map, serializerSwitchMethodInfo, deserializerSwitchMethodInfo);

            /* generate bodies */
            foreach (var type in nonStaticTypes)
                SerializtionCodeGen.GenerateSerializerBody(ctx, type, map[type].WriterILGen);

            foreach (var type in nonStaticTypes)
                DeserializtionCodeGen.GenerateDeserializerBody(ctx, type, map[type].ReaderILGen);

            var ilGen = serializerSwitchMethod.GetILGenerator();
            SerializtionCodeGen.GenerateSerializerSwitch(ctx, ilGen, map);
            s_serializerSwitch = (SerializerSwitch)serializerSwitchMethod.CreateDelegate(typeof(SerializerSwitch));

            ilGen = deserializerSwitchMethod.GetILGenerator();
            DeserializtionCodeGen.GenerateDeserializerSwitch(ctx, ilGen, map);
            s_deserializerSwitch = (DeserializerSwitch)deserializerSwitchMethod.CreateDelegate(typeof(DeserializerSwitch));

            return map.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.TypeID);
        }


        void GenerateAssembly(Type[] types)
        {
            Dictionary<Type, TypeData> map = GenerateTypeData(types);

            var nonStaticTypes = map.Where(kvp => kvp.Value.IsDynamic).Select(kvp => kvp.Key);

            var ab = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("NetSerializerDebug"), AssemblyBuilderAccess.RunAndSave);
            var modb = ab.DefineDynamicModule("NetSerializerDebug.dll");
            var tb = modb.DefineType("NetSerializer", TypeAttributes.Public);

            /* generate stubs */
            foreach (var type in nonStaticTypes)
            {
                var mb = SerializtionCodeGen.GenerateStaticSerializerStub(tb, type);
                map[type].WriterMethodInfo = mb;
                map[type].WriterILGen = mb.GetILGenerator();
            }

            foreach (var type in nonStaticTypes)
            {
                var dm = DeserializtionCodeGen.GenerateStaticDeserializerStub(tb, type);
                map[type].ReaderMethodInfo = dm;
                map[type].ReaderILGen = dm.GetILGenerator();
            }

            var serializerSwitchMethod = tb.DefineMethod("SerializerSwitch", MethodAttributes.Public | MethodAttributes.Static, null,
                new Type[] { typeof(Stream), typeof(object), typeof(Serializer) });
            serializerSwitchMethod.DefineParameter(1, ParameterAttributes.None, "stream");
            serializerSwitchMethod.DefineParameter(2, ParameterAttributes.None, "value");
            serializerSwitchMethod.DefineParameter(3, ParameterAttributes.None, "s");
            var serializerSwitchMethodInfo = serializerSwitchMethod;

            var deserializerSwitchMethod = tb.DefineMethod("DeserializerSwitch", MethodAttributes.Public | MethodAttributes.Static, null,
                new Type[] { typeof(Stream), typeof(object).MakeByRefType(), typeof(Serializer) });
            deserializerSwitchMethod.DefineParameter(1, ParameterAttributes.None, "stream");
            deserializerSwitchMethod.DefineParameter(2, ParameterAttributes.Out, "value");
            deserializerSwitchMethod.DefineParameter(3, ParameterAttributes.None, "s");
            var deserializerSwitchMethodInfo = deserializerSwitchMethod;

            var ctx = new CodeGenContext(map, serializerSwitchMethodInfo, deserializerSwitchMethodInfo);

            /* generate bodies */
            foreach (var type in nonStaticTypes)
                SerializtionCodeGen.GenerateSerializerBody(ctx, type, map[type].WriterILGen);

            foreach (var type in nonStaticTypes)
                DeserializtionCodeGen.GenerateDeserializerBody(ctx, type, map[type].ReaderILGen);

            var ilGen = serializerSwitchMethod.GetILGenerator();
            SerializtionCodeGen.GenerateSerializerSwitch(ctx, ilGen, map);

            ilGen = deserializerSwitchMethod.GetILGenerator();
            DeserializtionCodeGen.GenerateDeserializerSwitch(ctx, ilGen, map);

            tb.CreateType();
            ab.Save("Kngine.SerializerDebug.dll");
        }

        /* called from the dynamically generated code */


        public static ushort GetTypeID(object ob, Serializer s)
        {
            ushort id;

            if (ob == null)
                return 0;

            if (s.s_typeIDMap.TryGetValue(ob.GetType(), out id) == false)
                throw new InvalidOperationException(String.Format("Unknown type {0}", ob.GetType().FullName));

            return id;
        }



        public static IEnumerable<FieldInfo> GetFieldInfos(Type type)
        {
            Debug.Assert(type.IsSerializable);

            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(fi => (fi.Attributes & FieldAttributes.NotSerialized) == 0)
                .OrderBy(f => f.Name, StringComparer.Ordinal);

            if (type.BaseType == null)
            {
                return fields;
            }
            else
            {
                var baseFields = GetFieldInfos(type.BaseType);
                return baseFields.Concat(fields);
            }
        }

    }
}
