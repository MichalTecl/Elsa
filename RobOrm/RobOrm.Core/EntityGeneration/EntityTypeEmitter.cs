using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace RobOrm.Core.EntityGeneration
{
    /// <summary>
    /// Generates (Reflection.Emit) a sealed class implementing the entity interface. The class has the same shape
    /// as the class formerly generated as C# code by Robowire: a backing field and a public property for every
    /// interface property; column setters mark the entity dirty when the value changes.
    /// </summary>
    internal static class EntityTypeEmitter
    {
        private const string DYNAMIC_ASSEMBLY_NAME = "RobOrm.DynamicEntities";
        private const string TYPE_INFO_FIELD_NAME = "s_typeInfo";

        private const MethodAttributes ACCESSOR_ATTRIBUTES =
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName
            | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.NewSlot;

        private static readonly object s_lock = new object();

        private static readonly ConstructorInfo s_baseConstructor =
            typeof(EntityBase).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(EntityTypeInfo) }, null);

        private static readonly MethodInfo s_setIsDirtyMethod = typeof(EntityBase).GetProperty(nameof(EntityBase.IsDirty)).GetSetMethod();

        private static readonly MethodInfo s_getSingleReferenceMethod =
            typeof(EntityBase).GetMethod("GetSingleReference", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly MethodInfo s_getReferenceCollectionMethod =
            typeof(EntityBase).GetMethod("GetReferenceCollection", BindingFlags.Instance | BindingFlags.NonPublic);

        private static ModuleBuilder s_module;
        private static int s_typeCounter;

        public static Type Emit(EntityTypeInfo info)
        {
            lock (s_lock)
            {
                if (s_module == null)
                {
                    var assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(DYNAMIC_ASSEMBLY_NAME), AssemblyBuilderAccess.Run);
                    s_module = assembly.DefineDynamicModule(DYNAMIC_ASSEMBLY_NAME);
                }

                return EmitType(info);
            }
        }

        private static Type EmitType(EntityTypeInfo info)
        {
            var entityType = info.EntityType;

            var interfaces = new[] { entityType }
                .Concat(entityType.GetInterfaces())
                .Distinct()
                .Where(i => !i.IsAssignableFrom(typeof(EntityBase)))
                .ToList();

            foreach (var iface in interfaces)
            {
                ValidateInterface(entityType, iface);
            }

            var typeName = $"{DYNAMIC_ASSEMBLY_NAME}.{entityType.Name}_Impl_{Interlocked.Increment(ref s_typeCounter)}";

            var typeBuilder = s_module.DefineType(
                typeName,
                TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class | TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit,
                typeof(EntityBase));

            foreach (var iface in interfaces)
            {
                typeBuilder.AddInterfaceImplementation(iface);
            }

            var typeInfoField = typeBuilder.DefineField(TYPE_INFO_FIELD_NAME, typeof(EntityTypeInfo), FieldAttributes.Private | FieldAttributes.Static);

            EmitConstructor(typeBuilder, typeInfoField);

            var properties = new Dictionary<string, EmittedProperty>(StringComparer.Ordinal);

            // Same member order as the former generated class: plain and column properties, primary key, references
            foreach (var property in info.Properties)
            {
                AddProperty(properties, EmitFieldProperty(typeBuilder, property.Name, property.PropertyType, property.IsColumn));
            }

            AddProperty(properties, EmitFieldProperty(typeBuilder, info.PrimaryKeyName, info.PrimaryKeyType, false));

            foreach (var reference in info.References)
            {
                AddProperty(properties, EmitReferenceProperty(typeBuilder, reference));
            }

            ImplementInterfaces(typeBuilder, entityType, interfaces, properties);

            var implementationType = typeBuilder.CreateType();

            implementationType.GetField(TYPE_INFO_FIELD_NAME, BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, info);

            return implementationType;
        }

        private static void ValidateInterface(Type entityType, Type iface)
        {
            var method = iface.GetMethods().FirstOrDefault(m => !m.IsSpecialName);
            if (method != null)
            {
                throw new InvalidOperationException($"Cannot generate entity {entityType.FullName}: interface {iface.FullName} declares method {method.Name}. Entity interfaces can contain properties only.");
            }

            var evt = iface.GetEvents().FirstOrDefault();
            if (evt != null)
            {
                throw new InvalidOperationException($"Cannot generate entity {entityType.FullName}: interface {iface.FullName} declares event {evt.Name}. Entity interfaces can contain properties only.");
            }

            var indexer = iface.GetProperties().FirstOrDefault(p => p.GetIndexParameters().Length > 0);
            if (indexer != null)
            {
                throw new InvalidOperationException($"Cannot generate entity {entityType.FullName}: interface {iface.FullName} declares an indexer. Entity interfaces can contain properties only.");
            }
        }

        private static void EmitConstructor(TypeBuilder typeBuilder, FieldInfo typeInfoField)
        {
            var ctor = typeBuilder.DefineConstructor(
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                CallingConventions.Standard,
                Type.EmptyTypes);

            var il = ctor.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldsfld, typeInfoField);
            il.Emit(OpCodes.Call, s_baseConstructor);
            il.Emit(OpCodes.Ret);
        }

        private static EmittedProperty EmitFieldProperty(TypeBuilder typeBuilder, string name, Type propertyType, bool setsDirtyFlag)
        {
            var field = typeBuilder.DefineField("_" + name, propertyType, FieldAttributes.Private);

            var property = typeBuilder.DefineProperty(name, PropertyAttributes.None, propertyType, null);

            // get { return _field; }
            var getter = typeBuilder.DefineMethod("get_" + name, ACCESSOR_ATTRIBUTES, propertyType, Type.EmptyTypes);
            var il = getter.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, field);
            il.Emit(OpCodes.Ret);
            property.SetGetMethod(getter);

            var setter = typeBuilder.DefineMethod("set_" + name, ACCESSOR_ATTRIBUTES, typeof(void), new[] { propertyType });
            setter.DefineParameter(1, ParameterAttributes.None, "value");
            il = setter.GetILGenerator();

            if (setsDirtyFlag)
            {
                // if (_field == value) return;
                // IsDirty = true;
                var assign = il.DefineLabel();
                var equality = typeof(EntityPropertyEquality<>).MakeGenericType(propertyType)
                    .GetMethod(nameof(EntityPropertyEquality<object>.AreEqual), BindingFlags.Public | BindingFlags.Static);

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, field);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Call, equality);
                il.Emit(OpCodes.Brfalse_S, assign);
                il.Emit(OpCodes.Ret);
                il.MarkLabel(assign);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Call, s_setIsDirtyMethod);
            }

            // _field = value;
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stfld, field);
            il.Emit(OpCodes.Ret);
            property.SetSetMethod(setter);

            return new EmittedProperty(name, propertyType, getter, setter);
        }

        private static EmittedProperty EmitReferenceProperty(TypeBuilder typeBuilder, EntityReferenceInfo reference)
        {
            var helper = (reference.IsCollection ? s_getReferenceCollectionMethod : s_getSingleReferenceMethod).MakeGenericMethod(reference.EntityType);

            if (!reference.PropertyType.IsAssignableFrom(helper.ReturnType))
            {
                throw new InvalidOperationException(
                          $"Invalid definition of {reference.Source.DeclaringType?.Name}.{reference.Name}: the type {reference.PropertyType} cannot hold {helper.ReturnType}");
            }

            var property = typeBuilder.DefineProperty(reference.Name, PropertyAttributes.None, reference.PropertyType, null);

            // get { return GetSingleReference<TRef>(index); }  /  get { return GetReferenceCollection<TRef>(index); }
            var getter = typeBuilder.DefineMethod("get_" + reference.Name, ACCESSOR_ATTRIBUTES, reference.PropertyType, Type.EmptyTypes);
            var il = getter.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, reference.Index);
            il.Emit(OpCodes.Call, helper);
            il.Emit(OpCodes.Ret);
            property.SetGetMethod(getter);

            return new EmittedProperty(reference.Name, reference.PropertyType, getter, null);
        }

        private static void AddProperty(Dictionary<string, EmittedProperty> properties, EmittedProperty property)
        {
            if (properties.ContainsKey(property.Name))
            {
                throw new InvalidOperationException($"Duplicate entity property {property.Name}");
            }

            properties.Add(property.Name, property);
        }

        private static void ImplementInterfaces(
            TypeBuilder typeBuilder,
            Type entityType,
            IEnumerable<Type> interfaces,
            Dictionary<string, EmittedProperty> properties)
        {
            foreach (var iface in interfaces)
            {
                foreach (var interfaceProperty in iface.GetProperties())
                {
                    EmittedProperty implementation;
                    if (!properties.TryGetValue(interfaceProperty.Name, out implementation))
                    {
                        throw new InvalidOperationException($"Cannot generate entity {entityType.FullName}: property {iface.FullName}.{interfaceProperty.Name} cannot be implemented (properties without getter are not supported)");
                    }

                    if (implementation.PropertyType != interfaceProperty.PropertyType)
                    {
                        throw new InvalidOperationException($"Cannot generate entity {entityType.FullName}: property {interfaceProperty.Name} is declared with different types ({implementation.PropertyType} and {interfaceProperty.PropertyType} in {iface.FullName})");
                    }

                    var interfaceGetter = interfaceProperty.GetGetMethod();
                    if (interfaceGetter != null)
                    {
                        typeBuilder.DefineMethodOverride(implementation.Getter, interfaceGetter);
                    }

                    var interfaceSetter = interfaceProperty.GetSetMethod();
                    if (interfaceSetter != null)
                    {
                        if (implementation.Setter == null)
                        {
                            throw new InvalidOperationException($"Cannot generate entity {entityType.FullName}: property {iface.FullName}.{interfaceProperty.Name} must be read-only");
                        }

                        typeBuilder.DefineMethodOverride(implementation.Setter, interfaceSetter);
                    }
                }
            }
        }

        private sealed class EmittedProperty
        {
            public EmittedProperty(string name, Type propertyType, MethodInfo getter, MethodInfo setter)
            {
                Name = name;
                PropertyType = propertyType;
                Getter = getter;
                Setter = setter;
            }

            public string Name { get; }

            public Type PropertyType { get; }

            public MethodInfo Getter { get; }

            public MethodInfo Setter { get; }
        }
    }
}
