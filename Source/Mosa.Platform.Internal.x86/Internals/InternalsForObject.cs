﻿/*
 * (c) 2014 MOSA - The Managed Operating System Alliance
 *
 * Licensed under the terms of the New BSD License.
 *
 * Authors:
 *  Stefan Andres Charsley (charsleysa) <charsleysa@gmail.com>
 */

using System;
using System.Runtime.InteropServices;
using System.Reflection;

namespace Mosa.Platform.Internal.x86
{
	public unsafe static class InternalsForObject
	{
		public static object MemberwiseClone(void* obj)
		{
			return null;
		}

		public static Type GetType(void* obj)
		{
			// Get the handle of the object
			RuntimeTypeHandle handle = GetTypeHandle(obj);

			// Iterate through all the assemblies and look for the type handle
			foreach (RuntimeAssembly assembly in Runtime.Assemblies)
			{
				foreach (RuntimeType type in assembly.typeList)
				{
					// If its not a match then skip
					if (type.TypeHandle != handle)
						continue;

					// If we get here then its a match so return it
					return type;
				}
			}

			// If we didn't find a match then return null
			return null;
		}

		private static RuntimeTypeHandle GetTypeHandle(void* obj)
		{
			// TypeDefinition is located at the beginning of object (i.e. *obj )
			RuntimeTypeHandle handle = new RuntimeTypeHandle();
			((uint*)&handle)[0] = ((uint*)obj)[0];
			return handle;
		}
	}
}