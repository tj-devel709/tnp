﻿using System;
using TNPSupport.AbstractSyntax;
using TNPSupport.CodeGeneration;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace ILCodeGeneration
{
	public class HelloWorldGenerator : ICodeGenerator
	{
		public async Task Generate(ICodeGenerators environment, IASTNode node)
		{
			if (environment is CodeGeneratorsIL env && node is HelloWorldNode hello)
				await Generate (env, hello);
		}

		async Task Generate (CodeGeneratorsIL env, HelloWorldNode node)
		{
			await Task.Run (() => {
				var assembly = env.Environment.Assembly;
				if (assembly is null)
					throw new Exception ("no assembly?!");
				var typeSystem = assembly.MainModule.TypeSystem;

				var cl = MakeClass (typeSystem);
				env.Environment.AddType (cl);
				var ctor = MakeCtor (typeSystem);
				cl.Methods.Add (ctor);
				MakeCtorBody (assembly, typeSystem, cl, ctor);
				var main = MakeMain (typeSystem);
				cl.Methods.Add (main);
				MakeMainBody (assembly, typeSystem, cl, main);
				assembly.EntryPoint = main;
			});
		}


		MethodDefinition MakeCtor (TypeSystem typeSystem)
		{
			var ctor = new MethodDefinition (".ctor", MethodAttributes.Public | MethodAttributes.HideBySig
				| MethodAttributes.RTSpecialName | MethodAttributes.SpecialName, typeSystem.Void);
			return ctor;
		}

		void MakeCtorBody (AssemblyDefinition assembly, TypeSystem typeSystem, TypeDefinition cl, MethodDefinition m)
		{
			var il = m.Body.GetILProcessor ();
			if (cl.BaseType.DefaultCtor (out var baseCtor)) {
				il.Emit (OpCodes.Ldarg_0);
				il.Emit (OpCodes.Call, assembly.MainModule.ImportReference (baseCtor));
			}
			il.Emit (OpCodes.Ret);
		}

		MethodDefinition MakeMain (TypeSystem typeSystem)
		{
			var main = new MethodDefinition ("Main", MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig, typeSystem.Void);
			var parameter = new ParameterDefinition ("args", ParameterAttributes.None, typeSystem.String.MakeArrayType ());
			main.Parameters.Add (parameter);
			return main;
		}

		void MakeMainBody (AssemblyDefinition assembly, TypeSystem typeSystem, TypeDefinition cl, MethodDefinition m)
		{
			var il = m.Body.GetILProcessor ();
			il.Emit (OpCodes.Ldstr, "hello, world.");
			var writeLine = typeof (System.Console).ResolveMethod ("WriteLine",
				System.Reflection.BindingFlags.Default | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public,
				"System.String");
			il.Emit (OpCodes.Call, assembly.MainModule.ImportReference (writeLine));
			il.Emit (OpCodes.Ret);
		}

		TypeDefinition MakeClass (TypeSystem typeSystem)
		{
			var cl = new TypeDefinition ("DefaultNamespace", "DefaultClass",
				TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.Public
				| TypeAttributes.Sealed, typeSystem.Object);
			return cl;
		}


		public bool Matches(IASTNode node)
		{
			return node is HelloWorldNode;
		}
	}
}

