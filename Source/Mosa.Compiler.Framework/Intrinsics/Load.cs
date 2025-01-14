﻿/*
 * (c) 2014 MOSA - The Managed Operating System Alliance
 *
 * Licensed under the terms of the New BSD License.
 *
 * Authors:
 *  Phil Garcia (tgiphil) <phil@thinkedge.com>
 */

using Mosa.Compiler.Common;
using Mosa.Compiler.Framework.IR;

namespace Mosa.Compiler.Framework.Intrinsics
{
	[ReplacementTarget("Mosa.Internal.Native::Load32")]
	public sealed class Load : IIntrinsicInternalMethod
	{
		/// <summary>
		/// Replaces the intrinsic call site
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="methodCompiler">The method compiler.</param>
		void IIntrinsicInternalMethod.ReplaceIntrinsicCall(Context context, BaseMethodCompiler methodCompiler)
		{
			if (context.OperandCount == 1)
			{
				context.SetInstruction(IRInstruction.Load, context.Result, context.Operand1, Operand.CreateConstantSignedInt(methodCompiler.TypeSystem, 0));
			}
			else if (context.OperandCount == 2)
			{
				context.SetInstruction(IRInstruction.Load, context.Result, context.Operand1, context.Operand2);
			}
			else
			{
				throw new InvalidCompilerException();
			}
		}
	}
}