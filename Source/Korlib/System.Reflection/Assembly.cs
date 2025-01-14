﻿/*
 * (c) 2014 MOSA - The Managed Operating System Alliance
 *
 * Licensed under the terms of the New BSD License.
 *
 * Authors:
 *  Phil Garcia (tgiphil) <phil@thinkedge.com>
 *  Stefan Andres Charsley (charsleysa) <charsleysa@gmail.com>
 */

using System;
using System.Collections;
using System.Collections.Generic;

namespace System.Reflection
{
	[Serializable]
	public abstract class Assembly
	{
		/// <summary>
		/// Gets a collection that contains this assembly's custom attributes.
		/// </summary>
		public virtual IEnumerable<CustomAttributeData> CustomAttributes
		{
			get { return new CustomAttributeData[0]; }
		}

		/// <summary>
		/// Gets a collection of the types defined in this assembly.
		/// </summary>
		public abstract IEnumerable<TypeInfo> DefinedTypes { get; }

		/// <summary>
		/// 
		/// </summary>
		public virtual IEnumerable<Type> ExportedTypes
		{
			get { return new Type[0]; }
		}

		/// <summary>
		/// Gets the display name of the assembly.
		/// </summary>
		public virtual string FullName
		{
			get { return ""; }
		}

		/// <summary>
		/// Determines whether this assembly and the specified object are equal.
		/// </summary>
		/// <param name="obj">The object to compare with this instance.</param>
		/// <returns>True if o is equal to this instance; otherwise, False.</returns>
		public override bool Equals(object obj)
		{
			if (!(obj is Assembly))
				return false;

			return ((Assembly)obj).FullName == this.FullName;
		}

		public override int GetHashCode()
		{
			return this.FullName.GetHashCode();
		}
	}
}