﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.OData.Extensions;

namespace Microsoft.AspNetCore.OData.Common
{
	internal static class TypeHelper
	{
		internal static bool IsSubclassOfRawGeneric(this TypeInfo toCheck, TypeInfo generic)
		{
			while (toCheck != null && toCheck != typeof(object).GetTypeInfo())
			{
				if (generic == toCheck)
				{
					return true;
				}
				var toCheckDefinition = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition().GetTypeInfo() : toCheck;
				if (generic == toCheckDefinition)
				{
					return true;
				}
				toCheck = toCheck.BaseType.GetTypeInfo();
			}
			return false;
		}

		/// <summary>
		///     Returns type of T if the type implements IEnumerable of T, otherwise, return null.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		internal static Type GetImplementedIEnumerableType(Type type)
		{
			if (type == null)
			{
				throw Error.ArgumentNull("type");
			}

			foreach (var interfaceType in type.GetInterfaces().Concat(new[] { type }))
			{
				var gt = interfaceType.GetGenericArguments();

				if (gt.Count() == 1)
				{
					return gt[0];
				}
			}

			return null;
		}

		// TODO: Move this into AssembliesResolver and cache results
		internal static IEnumerable<Type> GetLoadedTypes(AssembliesResolver assembliesResolver)
		{
			var result = new List<Type>();
			foreach (var assembly in assembliesResolver.Assemblies)
			{
				// Go through all assemblies referenced by the application and search for types matching a predicate
				var assemblyParts =
					DefaultAssemblyPartDiscoveryProvider.DiscoverAssemblyParts(assembly.FullName)
						.Select(s => (s as AssemblyPart).Assembly);
				foreach (var assemblyPart in assemblyParts)
				{
					Type[] exportedTypes = null;
					if (assemblyPart == null || assemblyPart.IsDynamic)
					{
						// can't call GetTypes on a null (or dynamic?) assembly
						continue;
					}

					try
					{
						exportedTypes = assemblyPart.GetTypes();
					}
					catch (ReflectionTypeLoadException ex)
					{
						exportedTypes = ex.Types;
					}
					catch
					{
						continue;
					}

					if (exportedTypes != null)
					{
						result.AddRange(exportedTypes.Where(t => t != null && t.GetTypeInfo().IsVisible));
					}
				}

			}
			return result;
		}

		public static Type GetUnderlyingTypeOrSelf(Type type)
		{
			return Nullable.GetUnderlyingType(type) ?? type;
		}

		public static bool IsEnum(Type type)
		{
			var underlyingTypeOrSelf = GetUnderlyingTypeOrSelf(type);
			return underlyingTypeOrSelf.GetTypeInfo().IsEnum;
		}

		public static Type GetInnerElementType(this Type type)
		{
			Type elementType;
			type.IsCollection(out elementType);
			Contract.Assert(elementType != null);

			return elementType;
		}
	}
}