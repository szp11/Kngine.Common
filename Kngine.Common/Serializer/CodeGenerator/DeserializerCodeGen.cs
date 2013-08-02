using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Diagnostics;
using System.Linq.Expressions;

namespace Kngine.Serializer
{
    static class DeserializtionCodeGen
    {
        static Func<string> ai = Expression.Lambda<Func<string>>(Expression.Constant(string.Empty)).Compile();
        static MethodInfo mi = ai.Method;


        public static DynamicMethod GenerateDynamicDeserializerStub(Type type)
        {
            var dm = new DynamicMethod("Deserialize", null, new Type[] { typeof(MemoryStream), type.MakeByRefType(), typeof(Serializer) }, typeof(Serializer), true);
            dm.DefineParameter(1, ParameterAttributes.None, "stream");
            dm.DefineParameter(2, ParameterAttributes.Out, "value");
            dm.DefineParameter(3, ParameterAttributes.None, "s");

            return dm;
        }

        public static MethodBuilder GenerateStaticDeserializerStub(TypeBuilder tb, Type type)
        {
            var mb = tb.DefineMethod("Deserialize", MethodAttributes.Public | MethodAttributes.Static, null, new Type[] { typeof(MemoryStream), type.MakeByRefType(), typeof(Serializer) });
            mb.DefineParameter(1, ParameterAttributes.None, "stream");
            mb.DefineParameter(2, ParameterAttributes.Out, "value");
            mb.DefineParameter(3, ParameterAttributes.None, "s");
            return mb;
        }

        public static void GenerateDeserializerBody(CodeGenContext ctx, Type type, ILGenerator il)
        {
            // arg0: stream, arg1: out value, arg2: serializer

            if (type.IsArray)
                GenDeserializerBodyForArray(ctx, type, il);
            else
                GenDeserializerBody(ctx, type, il);
        }

        public static void GenDeserializerBody(CodeGenContext ctx, Type type, ILGenerator il)
        {
            if (type.IsClass)
            {
                // instantiate empty class
                il.Emit(OpCodes.Ldarg_1);

                if (type == typeof(string))
                {
                    il.Emit(OpCodes.Ldtoken, type);
                    il.Emit(OpCodes.Call, mi);
                    il.Emit(OpCodes.Stind_Ref);
                }
                else if (type.IsValueType || type.GetConstructor(Type.EmptyTypes) != null)
                {
                    var constructor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                    il.Emit(OpCodes.Newobj, constructor);
                    il.Emit(OpCodes.Stind_Ref);
                }
                else
                {
                    var gtfh = typeof(Type).GetMethod("GetTypeFromHandle", BindingFlags.Public | BindingFlags.Static);
                    var guo = typeof(System.Runtime.Serialization.FormatterServices).GetMethod("GetUninitializedObject", BindingFlags.Public | BindingFlags.Static);

                    il.Emit(OpCodes.Ldtoken, type);
                    il.Emit(OpCodes.Call, gtfh);
                    il.Emit(OpCodes.Call, guo);
                    il.Emit(OpCodes.Castclass, type);

                    il.Emit(OpCodes.Stind_Ref);
                }
            }

            var fields = Serializer.GetFieldInfos(type);

            foreach (var field in fields)
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);


                if (type.IsClass)
                    il.Emit(OpCodes.Ldind_Ref);

                il.Emit(OpCodes.Ldflda, field);
                il.Emit(OpCodes.Ldarg_2);
                GenDeserializerCall(ctx, il, field.FieldType);
            }

            il.Emit(OpCodes.Ret);
        }

        public static void GenDeserializerBodyForArray(CodeGenContext ctx, Type type, ILGenerator il)
        {
            var elemType = type.GetElementType();

            var lenLocal = il.DeclareLocal(typeof(uint));

            // read array len
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldloca_S, lenLocal);
            il.Emit(OpCodes.Ldarg_2);
            il.EmitCall(OpCodes.Call, ctx.GetReaderMethodInfo(typeof(uint)), null);

            var notNullLabel = il.DefineLabel();

            /* if len == 0, return null */
            il.Emit(OpCodes.Ldloc_S, lenLocal);
            il.Emit(OpCodes.Brtrue_S, notNullLabel);

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Stind_Ref);
            il.Emit(OpCodes.Ret);

            il.MarkLabel(notNullLabel);

            var arrLocal = il.DeclareLocal(type);

            // create new array with len - 1
            il.Emit(OpCodes.Ldloc_S, lenLocal);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Sub);
            il.Emit(OpCodes.Newarr, elemType);
            il.Emit(OpCodes.Stloc_S, arrLocal);

            // declare i
            var idxLocal = il.DeclareLocal(typeof(int));

            // i = 0
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc_S, idxLocal);

            var loopBodyLabel = il.DefineLabel();
            var loopCheckLabel = il.DefineLabel();

            il.Emit(OpCodes.Br_S, loopCheckLabel);

            // loop body
            il.MarkLabel(loopBodyLabel);

            // read element to arr[i]
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldloc_S, arrLocal);
            il.Emit(OpCodes.Ldloc_S, idxLocal);
            il.Emit(OpCodes.Ldelema, elemType);
            il.Emit(OpCodes.Ldarg_2);
            GenDeserializerCall(ctx, il, elemType);

            // i = i + 1
            il.Emit(OpCodes.Ldloc_S, idxLocal);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc_S, idxLocal);

            il.MarkLabel(loopCheckLabel);

            // loop condition
            il.Emit(OpCodes.Ldloc_S, idxLocal);
            il.Emit(OpCodes.Ldloc_S, arrLocal);
            il.Emit(OpCodes.Ldlen);
            il.Emit(OpCodes.Conv_I4);
            il.Emit(OpCodes.Clt);
            il.Emit(OpCodes.Brtrue_S, loopBodyLabel);


            // store new array to the out value
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldloc_S, arrLocal);
            il.Emit(OpCodes.Stind_Ref);

            il.Emit(OpCodes.Ret);
        }

        public static void GenDeserializerCall(CodeGenContext ctx, ILGenerator il, Type type)
        {
            // We can call the Deserializer method directly for:
            // - Value types
            // - Array types
            // - Sealed types with static Deserializer method, as the method will handle null
            // Other reference types go through the DeserializesSwitch

            bool direct;

            if (type.IsValueType || type.IsArray)
                direct = true;
            else if (type.IsSealed && ctx.IsDynamic(type) == false)
                direct = true;
            else
                direct = false;

            if (direct)
                il.EmitCall(OpCodes.Call, ctx.GetReaderMethodInfo(type), null);
            else
                il.EmitCall(OpCodes.Call, ctx.DeserializerSwitchMethodInfo, null);
        }


        public static void GenerateDeserializerSwitch(CodeGenContext ctx, ILGenerator il, IDictionary<Type, TypeData> map)
        {
            // arg0: stream, arg1: out object, arg2: serailizer

            //D(il, "deser switch");

            var idLocal = il.DeclareLocal(typeof(ushort));

            // read typeID
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldloca_S, idLocal);
            il.Emit(OpCodes.Ldarg_2);
            il.EmitCall(OpCodes.Call, ctx.GetReaderMethodInfo(typeof(ushort)), null);


            // +1 for 0 (null)
            var jumpTable = new Label[map.Count + 1];
            jumpTable[0] = il.DefineLabel();
            foreach (var kvp in map)
                jumpTable[kvp.Value.TypeID] = il.DefineLabel();

            il.Emit(OpCodes.Ldloc_S, idLocal);
            il.Emit(OpCodes.Switch, jumpTable);

            //D(il, "eihx");
            il.ThrowException(typeof(Exception));

            /* null case */
            il.MarkLabel(jumpTable[0]);

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Stind_Ref);
            il.Emit(OpCodes.Ret);

            /* cases for types */
            foreach (var kvp in map)
            {
                var type = kvp.Key;
                var data = kvp.Value;

                il.MarkLabel(jumpTable[data.TypeID]);

                var local = il.DeclareLocal(type);

                // call deserializer for this typeID
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldloca_S, local);
                il.Emit(OpCodes.Ldarg_2);
                if (data.WriterMethodInfo.IsGenericMethodDefinition)
                {
                    Debug.Assert(type.IsGenericType);

                    var genArgs = type.GetGenericArguments();

                    il.EmitCall(OpCodes.Call, data.ReaderMethodInfo.MakeGenericMethod(genArgs), null);
                }
                else
                {
                    il.EmitCall(OpCodes.Call, data.ReaderMethodInfo, null);
                }

                // write result object to out object
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldloc_S, local);
                if (type.IsValueType)
                    il.Emit(OpCodes.Box, type);
                il.Emit(OpCodes.Stind_Ref);

                //D(il, "deser switch done");

                il.Emit(OpCodes.Ret);
            }
        }
    }
}
