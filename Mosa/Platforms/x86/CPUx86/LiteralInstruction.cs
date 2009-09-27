﻿/*
 * (c) 2008 MOSA - The Managed Operating System Alliance
 *
 * Licensed under the terms of the New BSD License.
 *
 * Authors:
 *  Michael Ruck (<mailto:sharpos@michaelruck.de>)
 */

using System;
using System.Collections.Generic;
using System.Text;

using Mosa.Runtime.CompilerFramework;
using IR2 = Mosa.Runtime.CompilerFramework.IR2;
using Mosa.Runtime.Metadata.Signatures;

namespace Mosa.Platforms.x86.CPUx86
{
    /// <summary>
    /// Intermediate representation of the literal instruction.
    /// </summary>
	public sealed class LiteralInstruction : BaseInstruction
    {
        #region Construction

		/// <summary>
		/// Initializes a new instance of <see cref="LiteralInstruction"/>.
		/// </summary>
        public LiteralInstruction() :
            base()
        {
        }

        #endregion // Construction
    }
}