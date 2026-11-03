using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Xunit;

public class ArchitectureTests
{
    static IEnumerable<TypeDefinition> Types(IEnumerable<TypeDefinition> types)
    { foreach (var type in types) { yield return type; foreach (var nested in Types(type.NestedTypes)) yield return nested; } }
    static string Owner(TypeDefinition type) { while (type.DeclaringType != null) type = type.DeclaringType; return type.FullName; }
    static IEnumerable<TypeReference> Expand(TypeReference type)
    {
        if (type == null) yield break;
        yield return type;
        if (type is TypeSpecification spec) foreach (var t in Expand(spec.ElementType)) yield return t;
        if (type is GenericInstanceType generic) foreach (var arg in generic.GenericArguments) foreach (var t in Expand(arg)) yield return t;
    }
    static IEnumerable<TypeReference> ArgumentTypes(CustomAttributeArgument argument)
    {
        foreach (var t in Expand(argument.Type)) yield return t;
        if (argument.Value is TypeReference type)
            foreach (var t in Expand(type)) yield return t;
        if (argument.Value is CustomAttributeArgument[] items)
            foreach (var item in items)
                foreach (var t in ArgumentTypes(item)) yield return t;
    }
    static IEnumerable<TypeReference> Attributes(ICustomAttributeProvider provider)
    {
        if (!provider.HasCustomAttributes) yield break;
        foreach (var attribute in provider.CustomAttributes)
        {
            foreach (var t in Expand(attribute.AttributeType)) yield return t;
            foreach (var t in Expand(attribute.Constructor.DeclaringType)) yield return t;
            foreach (var p in attribute.Constructor.Parameters)
                foreach (var t in Expand(p.ParameterType)) yield return t;
            foreach (var argument in attribute.ConstructorArguments)
                foreach (var t in ArgumentTypes(argument)) yield return t;
            foreach (var argument in attribute.Fields.Concat(attribute.Properties))
                foreach (var t in ArgumentTypes(argument.Argument)) yield return t;
        }
    }
    static IEnumerable<TypeReference> Constraints(IEnumerable<GenericParameter> parameters)
    {
        foreach (var parameter in parameters)
        {
            foreach (var t in Attributes(parameter)) yield return t;
            foreach (var constraint in parameter.Constraints)
            {
                foreach (var t in Expand(constraint.ConstraintType)) yield return t;
                foreach (var t in Attributes(constraint)) yield return t;
            }
        }
    }
    static IEnumerable<TypeReference> References(TypeDefinition type)
    {
        foreach (var t in Attributes(type)) yield return t;
        foreach (var t in Constraints(type.GenericParameters)) yield return t;
        foreach (var t in Expand(type.BaseType)) yield return t;
        foreach (var i in type.Interfaces) foreach (var t in Expand(i.InterfaceType)) yield return t;
        foreach (var f in type.Fields) {
            foreach (var t in Expand(f.FieldType)) yield return t;
            foreach (var t in Attributes(f)) yield return t;
        }
        foreach (var p in type.Properties) {
            foreach (var t in Expand(p.PropertyType)) yield return t;
            foreach (var t in Attributes(p)) yield return t;
        }
        foreach (var e in type.Events) {
            foreach (var t in Expand(e.EventType)) yield return t;
            foreach (var t in Attributes(e)) yield return t;
        }
        foreach (var m in type.Methods)
        {
            foreach (var t in Attributes(m)) yield return t;
            foreach (var t in Attributes(m.MethodReturnType)) yield return t;
            foreach (var t in Constraints(m.GenericParameters)) yield return t;
            foreach (var t in Expand(m.ReturnType)) yield return t;
            foreach (var p in m.Parameters) {
                foreach (var t in Expand(p.ParameterType)) yield return t;
                foreach (var t in Attributes(p)) yield return t;
            }
            if (!m.HasBody) continue;
            foreach (var v in m.Body.Variables) foreach (var t in Expand(v.VariableType)) yield return t;
            foreach (var h in m.Body.ExceptionHandlers) foreach (var t in Expand(h.CatchType)) yield return t;
            foreach (var i in m.Body.Instructions)
            {
                if (i.Operand is TypeReference tr) foreach (var t in Expand(tr)) yield return t;
                if (i.Operand is MemberReference mr) foreach (var t in Expand(mr.DeclaringType)) yield return t;
                if (i.Operand is MethodReference method)
                {
                    foreach (var t in Expand(method.ReturnType)) yield return t;
                    foreach (var p in method.Parameters) foreach (var t in Expand(p.ParameterType)) yield return t;
                    if (method is GenericInstanceMethod gm) foreach (var a in gm.GenericArguments) foreach (var t in Expand(a)) yield return t;
                }
                if (i.Operand is FieldReference field) foreach (var t in Expand(field.FieldType)) yield return t;
            }
        }
    }
    static bool Forbidden(TypeReference t) => t.Namespace == "Npgsql" || t.Namespace.StartsWith("Npgsql.") || t.Namespace == "SteeLog.Infrastructure" || t.Namespace.StartsWith("SteeLog.Infrastructure.") || t.Namespace == "SteeLog.Datos" || t.Namespace.StartsWith("SteeLog.Datos.") || t.Namespace == "iTextSharp" || t.Namespace.StartsWith("iTextSharp.");
    static SortedSet<string> Violations(AssemblyDefinition assembly)
    {
        var result = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var type in Types(assembly.MainModule.Types))
        {
            var owner = Owner(type);
            if (owner.StartsWith("SteeLog.CompositionRoot.", StringComparison.Ordinal)) continue;
            foreach (var target in References(type).Where(Forbidden)) result.Add(owner + " -> " + target.FullName);
        }
        return result;
    }
    static AssemblyDefinition Read(string path) => AssemblyDefinition.ReadAssembly(path);
    static string HostPath => Environment.GetEnvironmentVariable("STEELOG_HOST_ASSEMBLY") ?? throw new InvalidOperationException("STEELOG_HOST_ASSEMBLY debe apuntar al host compilado.");
    [Fact]
    public void Assembly_references_respect_the_layer_graph()
    {
        var dir = AppDomain.CurrentDomain.BaseDirectory;
        Assert.Equal("SteeLog.Application", typeof(SteeLog.Core.Application.AssemblyMarker).Assembly.GetName().Name);
        foreach (var pair in new[] { ("SteeLog.Domain.dll", new string[0]), ("SteeLog.Application.dll", new[] { "SteeLog.Domain" }), ("SteeLog.Infrastructure.dll", new[] { "SteeLog.Application", "SteeLog.Domain" }) })
            using (var a = Read(Path.Combine(dir, pair.Item1)))
            {
                var own = a.MainModule.AssemblyReferences.Select(r => r.Name).Where(n => n.StartsWith("SteeLog.")).Except(pair.Item2).ToArray(); Assert.Empty(own);
                if (pair.Item1 != "SteeLog.Infrastructure.dll")
                {
                    var allowed = new[] { "netstandard", "mscorlib", "System.Runtime", "System", "System.Core" }.Concat(pair.Item2);
                    Assert.Empty(a.MainModule.AssemblyReferences.Select(r => r.Name).Except(allowed));
                }
                else
                {
                    Assert.DoesNotContain(a.MainModule.AssemblyReferences, r => r.Name.StartsWith("System.Windows.Forms") || r.Name.StartsWith("Guna"));
                }
            }
    }
    [Fact]
    public void Project_references_match_declared_graph()
    {
        var root = Directory.GetParent(Path.GetDirectoryName(HostPath)).Parent.Parent.FullName;
        var graph = new[] {
            ("Domain", new string[0]),
            ("Application", new[] { "SteeLog.Domain.csproj" }),
            ("Infrastructure", new[] { "SteeLog.Application.csproj", "SteeLog.Domain.csproj" })
        };
        foreach (var node in graph)
        {
            var project = Path.Combine(root, "SteeLog." + node.Item1, "SteeLog." + node.Item1 + ".csproj");
            var document = System.Xml.Linq.XDocument.Load(project);
            var actual = document.Descendants().Where(e => e.Name.LocalName == "ProjectReference")
                .Select(e => Path.GetFileName((string)e.Attribute("Include"))).OrderBy(x => x).ToArray();
            Assert.Empty(actual.Except(node.Item2));
            if (node.Item1 == "Application") Assert.Contains("SteeLog.Domain.csproj", actual);
            if (node.Item1 == "Infrastructure") Assert.Contains("SteeLog.Application.csproj", actual);
        }
    }
    [Fact]
    public void Host_legacy_type_dependencies_match_explicit_retirement_inventory()
    {
        using (var host = Read(HostPath))
        {
            var actual = Violations(host);
            var inventory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LegacyDependencies.txt");
            var entries = File.ReadAllLines(inventory).Where(x => !string.IsNullOrWhiteSpace(x) && !x.StartsWith("#")).ToArray();
            Assert.All(entries, x => Assert.Matches(@"^F[3-8] \| [^*]+ -> [^*]+$", x));
            Assert.Equal(entries.Length, entries.Distinct().Count());
            var expected = entries.Select(x => x.Substring(x.IndexOf(" | ", StringComparison.Ordinal) + 3));
            Assert.Equal(expected.OrderBy(x => x, StringComparer.Ordinal), actual);
        }
    }
    [Fact]
    public void Control_detects_generic_constraints_and_attribute_type_arguments()
    {
        using (var assembly = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("Control", new Version(1, 0)), "Control", ModuleKind.Dll))
        {
            var module = assembly.MainModule;
            var bad = new TypeReference("Npgsql", "Forbidden", module, module.TypeSystem.CoreLibrary);
            var constraintOwner = new TypeDefinition("Control", "Constraint", TypeAttributes.Public, module.TypeSystem.Object);
            var generic = new GenericParameter("T", constraintOwner);
            generic.Constraints.Add(new GenericParameterConstraint(bad));
            constraintOwner.GenericParameters.Add(generic);
            module.Types.Add(constraintOwner);
            var attributeOwner = new TypeDefinition("Control", "Attribute", TypeAttributes.Public, module.TypeSystem.Object);
            var attributeType = new TypeReference("Control", "MarkerAttribute", module, module.TypeSystem.CoreLibrary);
            var constructor = new MethodReference(".ctor", module.TypeSystem.Void, attributeType);
            var attribute = new CustomAttribute(constructor);
            attribute.ConstructorArguments.Add(new CustomAttributeArgument(module.ImportReference(typeof(Type)), bad));
            attributeOwner.CustomAttributes.Add(attribute);
            module.Types.Add(attributeOwner);
            var violations = Violations(assembly);
            Assert.Contains("Control.Constraint -> Npgsql.Forbidden", violations);
            Assert.Contains("Control.Attribute -> Npgsql.Forbidden", violations);
        }
    }
    [Fact]
    public void Control_detects_forbidden_access_even_in_async_nested_state_machine()
    {
        using (var assembly = Read(typeof(ArchitectureTests).Assembly.Location))
        {
            var violations = Violations(assembly);
            Assert.Contains(violations, x => x.StartsWith(typeof(ForbiddenControl).FullName + " -> Npgsql.", StringComparison.Ordinal));
        }
    }
}

class ForbiddenControl { public async System.Threading.Tasks.Task Run() { await System.Threading.Tasks.Task.Yield(); using (var c = new Npgsql.NpgsqlConnection()) { } } }
