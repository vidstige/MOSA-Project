/*
 * (c) 2008 MOSA - The Managed Operating System Alliance
 *
 * Licensed under the terms of the New BSD License.
 *
 * Authors:
 *  Michael Ruck (grover) <sharpos@michaelruck.de>
 *  Simon Wollwage (rootnode) <kintaro@think-in-co.de>
 *  Scott Balmos <sbalmos@fastmail.fm>
 *  Phil Garcia (tgiphil) <phil@thinkedge.com>
 *  Stefan Andres Charsley (charsleysa) <charsleysa@gmail.com>
 */

using Mosa.Compiler.Common;
using Mosa.Compiler.Framework;
using Mosa.Compiler.Framework.IR;
using System;
using System.Diagnostics;

namespace Mosa.Platform.x86.Stages
{
	/// <summary>
	/// Transforms 64-bit arithmetic to 32-bit operations.
	/// </summary>
	/// <remarks>
	/// This stage translates all 64-bit operations to appropriate 32-bit operations on
	/// architectures without appropriate 64-bit integral operations.
	/// </remarks>
	public sealed class LongOperandTransformationStage : BaseTransformationStage, IIRVisitor
	{
		private Operand constantZero;
		private Operand constantByte1;

		protected override void Run()
		{
			constantZero = Operand.CreateConstantSignedInt(TypeSystem, 0);
			constantByte1 = Operand.CreateConstantUnsignedByte(TypeSystem, 1);

			base.Run();
		}

		#region Utility Methods

		public static void SplitLongOperand(BaseMethodCompiler methodCompiler, Operand operand, out Operand operandLow, out Operand operandHigh, Operand constantZero)
		{
			if (operand.Is64BitInteger)
			{
				methodCompiler.VirtualRegisters.SplitLongOperand(methodCompiler.TypeSystem, operand, 4, 0);
				operandLow = operand.Low;
				operandHigh = operand.High;
				return;
			}
			else
			{
				operandLow = operand;
				operandHigh = constantZero != null ? constantZero : Operand.CreateConstantSignedInt(methodCompiler.TypeSystem, 0);
				return;
			}

			//throw new InvalidProgramException("@can not split" + operand.ToString());
		}

		private void SplitLongOperand(Operand operand, out Operand operandLow, out Operand operandHigh)
		{
			SplitLongOperand(MethodCompiler, operand, out operandLow, out operandHigh, constantZero);
		}

		private void ReplaceWithDivisionCall(Context context, string methodName)
		{
			var type = TypeSystem.GetTypeByName("Mosa.Platform.Internal.x86", "Division");

			Debug.Assert(type != null, "Cannot find type: Mosa.Platform.Internal.x86.Division type");

			var method = type.FindMethodByName(methodName);

			Debug.Assert(method != null, "Cannot find method: " + methodName);

			context.ReplaceInstructionOnly(IRInstruction.Call);
			context.SetOperand(0, Operand.CreateSymbolFromMethod(TypeSystem, method));
			context.OperandCount = 1;
			context.MosaMethod = method;
		}

		/// <summary>
		/// Expands the add instruction for 64-bit operands.
		/// </summary>
		/// <param name="context">The context.</param>
		private void ExpandAdd(Context context)
		{
			Operand op0H, op1H, op2H, op0L, op1L, op2L;
			SplitLongOperand(context.Result, out op0L, out op0H);
			SplitLongOperand(context.Operand1, out op1L, out op1H);
			SplitLongOperand(context.Operand2, out op2L, out op2H);

			Operand eaxH = AllocateVirtualRegister(TypeSystem.BuiltIn.I4);
			Operand eaxL = AllocateVirtualRegister(TypeSystem.BuiltIn.U4);

			context.SetInstruction(X86.Mov, eaxL, op1L);
			context.AppendInstruction(X86.Add, eaxL, eaxL, op2L);
			context.AppendInstruction(X86.Mov, op0L, eaxL);
			context.AppendInstruction(X86.Mov, eaxH, op1H);
			context.AppendInstruction(X86.Adc, eaxH, eaxH, op2H);
			context.AppendInstruction(X86.Mov, op0H, eaxH);
		}

		/// <summary>
		/// Expands the sub instruction for 64-bit operands.
		/// </summary>
		/// <param name="context">The context.</param>
		private void ExpandSub(Context context)
		{
			Operand op0H, op1H, op2H, op0L, op1L, op2L;
			SplitLongOperand(context.Result, out op0L, out op0H);
			SplitLongOperand(context.Operand1, out op1L, out op1H);
			SplitLongOperand(context.Operand2, out op2L, out op2H);

			Operand eaxH = AllocateVirtualRegister(TypeSystem.BuiltIn.I4);
			Operand eaxL = AllocateVirtualRegister(TypeSystem.BuiltIn.U4);

			context.SetInstruction(X86.Mov, eaxL, op1L);
			context.AppendInstruction(X86.Sub, eaxL, eaxL, op2L);
			context.AppendInstruction(X86.Mov, op0L, eaxL);
			context.AppendInstruction(X86.Mov, eaxH, op1H);
			context.AppendInstruction(X86.Sbb, eaxH, eaxH, op2H);
			context.AppendInstruction(X86.Mov, op0H, eaxH);
		}

		/// <summary>
		/// Expands the mul instruction for 64-bit operands.
		/// </summary>
		/// <param name="context">The context.</param>
		private void ExpandMul(Context context)
		{
			Operand op0H, op1H, op2H, op0L, op1L, op2L;
			SplitLongOperand(context.Result, out op0L, out op0H);
			SplitLongOperand(context.Operand1, out op1L, out op1H);
			SplitLongOperand(context.Operand2, out op2L, out op2H);

			//Operand eax = AllocateVirtualRegister(typeSystem.BuiltIn.I4);
			//Operand edx = AllocateVirtualRegister(typeSystem.BuiltIn.I4);
			//Operand ebx = AllocateVirtualRegister(typeSystem.BuiltIn.I4);
			Operand eax = Operand.CreateCPURegister(TypeSystem.BuiltIn.I4, GeneralPurposeRegister.EAX);
			Operand edx = Operand.CreateCPURegister(TypeSystem.BuiltIn.I4, GeneralPurposeRegister.EDX);
			Operand ebx = Operand.CreateCPURegister(TypeSystem.BuiltIn.I4, GeneralPurposeRegister.EBX);

			Operand v16 = AllocateVirtualRegister(TypeSystem.BuiltIn.I4);
			Operand v20 = AllocateVirtualRegister(TypeSystem.BuiltIn.I4);
			Operand v12 = AllocateVirtualRegister(TypeSystem.BuiltIn.I4);

			// unoptimized
			context.SetInstruction(X86.Mov, eax, op2L);
			context.AppendInstruction(X86.Mov, v20, eax);
			context.AppendInstruction(X86.Mov, eax, v20);
			context.AppendInstruction2(X86.Mul, edx, eax, eax, op1L);
			context.AppendInstruction(X86.Mov, v16, eax);
			context.AppendInstruction(X86.Mov, v12, edx);
			context.AppendInstruction(X86.Mov, eax, op1L);
			context.AppendInstruction(X86.Mov, ebx, eax);
			context.AppendInstruction(X86.IMul, ebx, ebx, op2H);
			context.AppendInstruction(X86.Mov, eax, v12);
			context.AppendInstruction(X86.Add, eax, eax, ebx);
			context.AppendInstruction(X86.Mov, ebx, op2L);
			context.AppendInstruction(X86.IMul, ebx, ebx, op1H);
			context.AppendInstruction(X86.Add, eax, eax, ebx);
			context.AppendInstruction(X86.Mov, v12, eax);
			context.AppendInstruction(X86.Mov, op0L, v16);
			context.AppendInstruction(X86.Mov, op0H, v12);
		}

		/// <summary>
		/// Expands the div.
		/// </summary>
		/// <param name="context">The context.</param>
		private void ExpandDiv(Context context)
		{
			Operand op0H, op1H, op2H, op0L, op1L, op2L;
			var result = context.Result;
			var op1 = context.Operand1;
			var op2 = context.Operand2;

			SplitLongOperand(result, out op0L, out op0H);
			SplitLongOperand(op1, out op1L, out op1H);
			SplitLongOperand(op2, out op2L, out op2H);

			ReplaceWithDivisionCall(context, "sdiv64");
			context.Result = result;
			context.Operand2 = op1;
			context.Operand3 = op2;
			context.OperandCount = 3;
			context.ResultCount = 1;
		}

		/// <summary>
		/// Expands the rem.
		/// </summary>
		/// <param name="context">The context.</param>
		private void ExpandRem(Context context)
		{
			Operand op0H, op1H, op2H, op0L, op1L, op2L;
			var result = context.Result;
			var op1 = context.Operand1;
			var op2 = context.Operand2;

			SplitLongOperand(result, out op0L, out op0H);
			SplitLongOperand(op1, out op1L, out op1H);
			SplitLongOperand(op2, out op2L, out op2H);

			ReplaceWithDivisionCall(context, "smod64");
			context.Result = result;
			context.Operand2 = op1;
			context.Operand3 = op2;
			context.OperandCount = 3;
			context.ResultCount = 1;
		}

		/// <summary>
		/// Expands the udiv instruction.
		/// </summary>
		/// <param name="context">The context.</param>
		private void ExpandUDiv(Context context)
		{
			Operand op0H, op1H, op2H, op0L, op1L, op2L;
			var result = context.Result;
			var op1 = context.Operand1;
			var op2 = context.Operand2;

			SplitLongOperand(result, out op0L, out op0H);
			SplitLongOperand(op1, out op1L, out op1H);
			SplitLongOperand(op2, out op2L, out op2H);

			ReplaceWithDivisionCall(context, "udiv64");
			context.Result = result;
			context.Operand2 = op1;
			context.Operand3 = op2;
			context.OperandCount = 3;
			context.ResultCount = 1;
		}

		/// <summary>
		/// Expands the urem instruction.
		/// </summary>
		/// <param name="context">The context.</param>
		private void ExpandURem(Context context)
		{
			Operand op0H, op1H, op2H, op0L, op1L, op2L;
			var result = context.Result;
			var op1 = context.Operand1;
			var op2 = context.Operand2;

			SplitLongOperand(result, out op0L, out op0H);
			SplitLongOperand(op1, out op1L, out op1H);
			SplitLongOperand(op2, out op2L, out op2H);

			ReplaceWithDivisionCall(context, "umod64");
			context.Result = result;
			context.Operand2 = op1;
			context.Operand3 = op2;
			context.OperandCount = 3;
			context.ResultCount = 1;
		}

		/// <summary>
		/// Expands the arithmetic shift right instruction for 64-bits.
		/// </summary>
		/// <param name="context">The context.</param>
		private void ExpandArithmeticShiftRight(Context context)
		{
			Operand count = context.Operand2;

			Operand op0H, op1H, op0L, op1L;
			SplitLongOperand(context.Result, out op0L, out op0H);
			SplitLongOperand(context.Operand1, out op1L, out op1H);

			Operand eax = AllocateVirtualRegister(TypeSystem.BuiltIn.I4);
			Operand edx = AllocateVirtualRegister(TypeSystem.BuiltIn.I4);
			Operand ecx = AllocateVirtualRegister(TypeSystem.BuiltIn.I4);

			Context[] newBlocks = CreateNewBlocksWithContexts(6);
			Context nextBlock = Split(context);

			context.SetInstruction(X86.Jmp, newBlocks[0].BasicBlock);
			LinkBlocks(context, newBlocks[0]);

			newBlocks[0].AppendInstruction(X86.Mov, ecx, count);
			newBlocks[0].AppendInstruction(X86.Mov, edx, op1H);
			newBlocks[0].AppendInstruction(X86.Mov, eax, op1L);
			newBlocks[0].AppendInstruction(X86.Cmp, null, ecx, Operand.CreateConstantSignedInt(TypeSystem, (int)64));
			newBlocks[0].AppendInstruction(X86.Branch, ConditionCode.UnsignedGreaterOrEqual, newBlocks[4].BasicBlock);
			newBlocks[0].AppendInstruction(X86.Jmp, newBlocks[1].BasicBlock);
			LinkBlocks(newBlocks[0], newBlocks[4], newBlocks[1]);

			newBlocks[1].AppendInstruction(X86.Cmp, null, ecx, Operand.CreateConstantSignedInt(TypeSystem, 32));
			newBlocks[1].AppendInstruction(X86.Branch, ConditionCode.UnsignedGreaterOrEqual, newBlocks[3].BasicBlock);
			newBlocks[1].AppendInstruction(X86.Jmp, newBlocks[2].BasicBlock);
			LinkBlocks(newBlocks[1], newBlocks[3], newBlocks[2]);

			newBlocks[2].AppendInstruction(X86.Shrd, eax, eax, edx, ecx);
			newBlocks[2].AppendInstruction(X86.Sar, edx, edx, ecx);
			newBlocks[2].AppendInstruction(X86.Jmp, newBlocks[5].BasicBlock);
			LinkBlocks(newBlocks[2], newBlocks[5]);

			newBlocks[3].AppendInstruction(X86.Mov, eax, edx);
			newBlocks[3].AppendInstruction(X86.Sar, edx, edx, Operand.CreateConstantSignedInt(TypeSystem, (int)0x1F));
			newBlocks[3].AppendInstruction(X86.And, ecx, ecx, Operand.CreateConstantSignedInt(TypeSystem, (int)0x1F));
			newBlocks[3].AppendInstruction(X86.Sar, eax, eax, ecx);
			newBlocks[3].AppendInstruction(X86.Jmp, newBlocks[5].BasicBlock);
			LinkBlocks(newBlocks[3], nextBlock);

			newBlocks[4].AppendInstruction(X86.Sar, edx, edx, Operand.CreateConstantSignedInt(TypeSystem, (int)0x1F));
			newBlocks[4].AppendInstruction(X86.Mov, eax, edx);
			newBlocks[4].AppendInstruction(X86.Jmp, newBlocks[5].BasicBlock);
			LinkBlocks(newBlocks[4], newBlocks[5]);

			newBlocks[5].AppendInstruction(X86.Mov, op0H, edx);
			newBlocks[5].AppendInstruction(X86.Mov, op0L, eax);
			newBlocks[5].AppendInstruction(X86.Jmp, nextBlock.BasicBlock);
			LinkBlocks(newBlocks[5], nextBlock);
		}

		/// <summary>
		/// Expands the shift left instruction for 64-bits.
		/// </summary>
		/// <param name="context">The context.</param>
		private void ExpandShiftLeft(Context context)
		{
			Operand count = context.Operand2;

			Operand op0H, op1H, op0L, op1L;
			SplitLongOperand(context.Result, out op0L, out op0H);
			SplitLongOperand(context.Operand1, out op1L, out op1H);

			Operand eax = AllocateVirtualRegister(TypeSystem.BuiltIn.I4);
			Operand edx = AllocateVirtualRegister(TypeSystem.BuiltIn.I4);
			Operand ecx = AllocateVirtualRegister(TypeSystem.BuiltIn.I4);

			Context nextBlock = Split(context);
			Context[] newBlocks = CreateNewBlocksWithContexts(6);

			context.SetInstruction(X86.Jmp, newBlocks[0].BasicBlock);
			LinkBlocks(context, newBlocks[0]);

			newBlocks[0].AppendInstruction(X86.Mov, ecx, count);
			newBlocks[0].AppendInstruction(X86.Mov, edx, op1H);
			newBlocks[0].AppendInstruction(X86.Mov, eax, op1L);
			newBlocks[0].AppendInstruction(X86.Cmp, null, ecx, Operand.CreateConstantSignedInt(TypeSystem, (int)64));
			newBlocks[0].AppendInstruction(X86.Branch, ConditionCode.UnsignedGreaterOrEqual, newBlocks[4].BasicBlock);
			newBlocks[0].AppendInstruction(X86.Jmp, newBlocks[1].BasicBlock);
			LinkBlocks(newBlocks[0], newBlocks[4], newBlocks[1]);

			newBlocks[1].AppendInstruction(X86.Cmp, null, ecx, Operand.CreateConstantSignedInt(TypeSystem, (int)32));
			newBlocks[1].AppendInstruction(X86.Branch, ConditionCode.UnsignedGreaterOrEqual, newBlocks[3].BasicBlock);
			newBlocks[1].AppendInstruction(X86.Jmp, newBlocks[2].BasicBlock);
			LinkBlocks(newBlocks[1], newBlocks[3], newBlocks[2]);

			newBlocks[2].AppendInstruction(X86.Shld, edx, edx, eax, ecx);
			newBlocks[2].AppendInstruction(X86.Shl, eax, eax, ecx);
			newBlocks[2].AppendInstruction(X86.Jmp, newBlocks[5].BasicBlock);
			LinkBlocks(newBlocks[2], newBlocks[5]);

			newBlocks[3].AppendInstruction(X86.Mov, edx, eax);
			newBlocks[3].AppendInstruction(X86.Mov, eax, constantZero);
			newBlocks[3].AppendInstruction(X86.And, ecx, ecx, Operand.CreateConstantSignedInt(TypeSystem, 0x1F));
			newBlocks[3].AppendInstruction(X86.Shl, edx, edx, ecx);
			newBlocks[3].AppendInstruction(X86.Jmp, newBlocks[5].BasicBlock);
			LinkBlocks(newBlocks[3], newBlocks[5]);

			newBlocks[4].AppendInstruction(X86.Mov, eax, constantZero);
			newBlocks[4].AppendInstruction(X86.Mov, edx, constantZero);
			newBlocks[4].AppendInstruction(X86.Jmp, newBlocks[5].BasicBlock);
			LinkBlocks(newBlocks[4], newBlocks[5]);

			newBlocks[5].AppendInstruction(X86.Mov, op0H, edx);
			newBlocks[5].AppendInstruction(X86.Mov, op0L, eax);
			newBlocks[5].AppendInstruction(X86.Jmp, nextBlock.BasicBlock);
			LinkBlocks(newBlocks[5], nextBlock);
		}

		/// <summary>
		/// Expands the shift right instruction for 64-bits.
		/// </summary>
		/// <param name="context">The context.</param>
		private void ExpandShiftRight(Context context)
		{
			Operand count = context.Operand2;

			Operand op0H, op1H, op0L, op1L;
			SplitLongOperand(context.Result, out op0L, out op0H);
			SplitLongOperand(context.Operand1, out op1L, out op1H);

			//Operand eax = AllocateVirtualRegister(TypeSystem.BuiltIn.I4);
			//Operand edx = AllocateVirtualRegister(TypeSystem.BuiltIn.I4);
			Operand ecx = AllocateVirtualRegister(TypeSystem.BuiltIn.I4);

			Context nextBlock = Split(context);
			Context[] newBlocks = CreateNewBlocksWithContexts(4);

			context.SetInstruction(X86.Mov, ecx, count);
			context.AppendInstruction(X86.Cmp, null, ecx, Operand.CreateConstantSignedInt(TypeSystem, (int)64));
			context.AppendInstruction(X86.Branch, ConditionCode.UnsignedGreaterOrEqual, newBlocks[3].BasicBlock);
			context.AppendInstruction(X86.Jmp, newBlocks[0].BasicBlock);
			LinkBlocks(context, newBlocks[3], newBlocks[0]);

			newBlocks[0].AppendInstruction(X86.Cmp, null, ecx, Operand.CreateConstantSignedInt(TypeSystem, (int)32));
			newBlocks[0].AppendInstruction(X86.Branch, ConditionCode.UnsignedGreaterOrEqual, newBlocks[2].BasicBlock);
			newBlocks[0].AppendInstruction(X86.Jmp, newBlocks[1].BasicBlock);
			LinkBlocks(newBlocks[0], newBlocks[2], newBlocks[1]);

			newBlocks[1].AppendInstruction(X86.Mov, op0H, op1H);
			newBlocks[1].AppendInstruction(X86.Mov, op0L, op1L);
			newBlocks[1].AppendInstruction(X86.Shrd, op0L, op0L, op0H, ecx);
			newBlocks[1].AppendInstruction(X86.Shr, op0H, op0H, ecx);
			newBlocks[1].AppendInstruction(X86.Jmp, nextBlock.BasicBlock);
			LinkBlocks(newBlocks[1], nextBlock.BasicBlock);

			newBlocks[2].AppendInstruction(X86.Mov, op0L, op1H);
			newBlocks[2].AppendInstruction(X86.Mov, op0H, Operand.CreateConstantSignedInt(TypeSystem, (int)0x0));
			newBlocks[2].AppendInstruction(X86.And, ecx, ecx, Operand.CreateConstantSignedInt(TypeSystem, (int)0x1F));
			newBlocks[2].AppendInstruction(X86.Sar, op0L, op0L, ecx);
			newBlocks[2].AppendInstruction(X86.Jmp, nextBlock.BasicBlock);
			LinkBlocks(newBlocks[2], nextBlock.BasicBlock);

			newBlocks[3].AppendInstruction(X86.Mov, op0H, Operand.CreateConstantSignedInt(TypeSystem, (int)0x0));
			newBlocks[3].AppendInstruction(X86.Mov, op0L, op0H);
			newBlocks[3].AppendInstruction(X86.Jmp, nextBlock.BasicBlock);
			LinkBlocks(newBlocks[3], nextBlock.BasicBlock);
		}

		/// <summary>
		/// Expands the neg instruction for 64-bits.
		/// </summary>
		/// <param name="context">The context.</param>
		private void ExpandNeg(Context context)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Expands the not instruction for 64-bits.
		/// </summary>
		/// <param name="context">The context.</param>
		private void ExpandNot(Context context)
		{
			Operand op0H, op1H, op0L, op1L;
			SplitLongOperand(context.Result, out op0L, out op0H);
			SplitLongOperand(context.Operand1, out op1L, out op1H);

			Operand eax = AllocateVirtualRegister(TypeSystem.BuiltIn.U4);

			context.SetInstruction(X86.Mov, eax, op1H);
			context.AppendInstruction(X86.Not, eax, eax);
			context.AppendInstruction(X86.Mov, op0H, eax);

			context.AppendInstruction(X86.Mov, eax, op1L);
			context.AppendInstruction(X86.Not, eax, eax);
			context.AppendInstruction(X86.Mov, op0L, eax);
		}

		/// <summary>
		/// Expands the and instruction for 64-bits.
		/// </summary>
		/// <param name="context">The context.</param>
		private void ExpandAnd(Context context)
		{
			Operand op0H, op1H, op2H, op0L, op1L, op2L;
			SplitLongOperand(context.Result, out op0L, out op0H);
			SplitLongOperand(context.Operand1, out op1L, out op1H);
			SplitLongOperand(context.Operand2, out op2L, out op2H);

			if (!context.Result.Is64BitInteger)
			{
				context.SetInstruction(X86.Mov, op0L, op1L);
				context.AppendInstruction(X86.And, op0L, op0L, op2L);
			}
			else
			{
				context.SetInstruction(X86.Mov, op0H, op1H);
				context.AppendInstruction(X86.Mov, op0L, op1L);
				context.AppendInstruction(X86.And, op0H, op0H, op2H);
				context.AppendInstruction(X86.And, op0L, op0L, op2L);
			}
		}

		/// <summary>
		/// Expands the or instruction for 64-bits.
		/// </summary>
		/// <param name="context">The context.</param>
		private void ExpandOr(Context context)
		{
			Operand op0H, op1H, op2H, op0L, op1L, op2L;
			SplitLongOperand(context.Result, out op0L, out op0H);
			SplitLongOperand(context.Operand1, out op1L, out op1H);
			SplitLongOperand(context.Operand2, out op2L, out op2H);

			context.SetInstruction(X86.Mov, op0H, op1H);
			context.AppendInstruction(X86.Mov, op0L, op1L);
			context.AppendInstruction(X86.Or, op0H, op0H, op2H);
			context.AppendInstruction(X86.Or, op0L, op0L, op2L);
		}

		/// <summary>
		/// Expands the neg instruction for 64-bits.
		/// </summary>
		/// <param name="context">The context.</param>
		private void ExpandXor(Context context)
		{
			Operand op0H, op1H, op2H, op0L, op1L, op2L;
			SplitLongOperand(context.Result, out op0L, out op0H);
			SplitLongOperand(context.Operand1, out op1L, out op1H);
			SplitLongOperand(context.Operand2, out op2L, out op2H);

			context.SetInstruction(X86.Mov, op0H, op1H);
			context.AppendInstruction(X86.Mov, op0L, op1L);
			context.AppendInstruction(X86.Xor, op0H, op0H, op2H);
			context.AppendInstruction(X86.Xor, op0L, op0L, op2L);
		}

		/// <summary>
		/// Expands the move instruction for 64-bits.
		/// </summary>
		/// <param name="context">The context.</param>
		private void ExpandMove(Context context)
		{
			Operand op0L, op0H, op1L, op1H;

			if (context.Result.Is64BitInteger)
			{
				SplitLongOperand(context.Result, out op0L, out op0H);
				SplitLongOperand(context.Operand1, out op1L, out op1H);

				context.SetInstruction(X86.Mov, op0L, op1L);
				context.AppendInstruction(X86.Mov, op0H, op1H);
			}
			else
			{
				SplitLongOperand(context.Operand1, out op1L, out op1H);
				context.SetInstruction(X86.Mov, context.Result, op1L);
			}
		}

		/// <summary>
		/// Expands the unsigned move instruction for 64-bits.
		/// </summary>
		/// <param name="context">The context.</param>
		private void ExpandUnsignedMove(Context context)
		{
			Operand op0 = context.Result;
			Operand op1 = context.Operand1;

			Operand op0L, op0H, op1L, op1H;
			SplitLongOperand(op0, out op0L, out op0H);
			SplitLongOperand(op1, out op1L, out op1H);

			if (op1.IsInt)
			{
				context.SetInstruction(X86.Mov, op0L, op1L);
				context.AppendInstruction(X86.Mov, op0H, constantZero);
			}
			else if (op1.IsBoolean || op1.IsChar || op1.IsU1 || op1.IsU2)
			{
				context.SetInstruction(X86.Movzx, op0L, op1L);
				context.AppendInstruction(X86.Mov, op0H, constantZero);
			}
			else if (op1.IsU8)
			{
				context.SetInstruction(X86.Mov, op0L, op1L);
				context.AppendInstruction(X86.Mov, op0H, op1H);
			}
			else
			{
				throw new NotSupportedException();
			}
		}

		/// <summary>
		/// Expands the signed move instruction for 64-bits.
		/// </summary>
		/// <param name="context">The context.</param>
		private void ExpandSignedMove(Context context)
		{
			Operand op0 = context.Result;
			Operand op1 = context.Operand1;
			Debug.Assert(op0 != null, @"I8 not in a memory operand!");

			Operand op0L, op0H;
			SplitLongOperand(op0, out op0L, out op0H);

			if (op1.IsBoolean)
			{
				context.SetInstruction(X86.Movzx, op0L, op1);
				context.AppendInstruction(X86.Mov, op0H, constantZero);
			}
			else if (op1.IsI1 || op1.IsI2)
			{
				Operand v1 = AllocateVirtualRegister(TypeSystem.BuiltIn.I4);
				Operand v2 = AllocateVirtualRegister(TypeSystem.BuiltIn.U4);
				Operand v3 = AllocateVirtualRegister(TypeSystem.BuiltIn.U4);

				context.SetInstruction(X86.Movsx, v1, op1);
				context.AppendInstruction2(X86.Cdq, v3, v2, v1);
				context.AppendInstruction(X86.Mov, op0L, v2);
				context.AppendInstruction(X86.Mov, op0H, v3);
			}
			else if (op1.IsI4 || op1.IsPointer || op1.IsI || op1.IsU)
			{
				Operand v1 = AllocateVirtualRegister(TypeSystem.BuiltIn.I4);
				Operand v2 = AllocateVirtualRegister(TypeSystem.BuiltIn.U4);
				Operand v3 = AllocateVirtualRegister(TypeSystem.BuiltIn.U4);

				context.SetInstruction(X86.Mov, v1, op1);
				context.AppendInstruction2(X86.Cdq, v3, v2, v1);
				context.AppendInstruction(X86.Mov, op0L, v2);
				context.AppendInstruction(X86.Mov, op0H, v3);
			}
			else if (op1.IsI8)
			{
				context.SetInstruction(X86.Mov, op0, op1);
			}
			else if (op1.IsU1 || op1.IsU2)
			{
				Operand v1 = AllocateVirtualRegister(TypeSystem.BuiltIn.U4);
				Operand v2 = AllocateVirtualRegister(TypeSystem.BuiltIn.U4);
				Operand v3 = AllocateVirtualRegister(TypeSystem.BuiltIn.U4);

				context.SetInstruction(X86.Movzx, v1, op1);
				context.AppendInstruction2(X86.Cdq, v3, v2, v1);
				context.AppendInstruction(X86.Mov, op0L, v2);
				context.AppendInstruction(X86.Mov, op0H, constantZero);
			}
			else
			{
				throw new InvalidCompilerException();
			}
		}

		/// <summary>
		/// Expands the load instruction for 64-bits.
		/// </summary>
		/// <param name="context">The context.</param>
		private void ExpandLoad(Context context)
		{
			Operand address = context.Operand1;
			Operand offset = context.Operand2;

			Operand op0L, op0H;
			SplitLongOperand(context.Result, out op0L, out op0H);

			Operand v1 = AllocateVirtualRegister(TypeSystem.BuiltIn.U4);
			Operand v2 = AllocateVirtualRegister(TypeSystem.BuiltIn.U4);
			Operand v3 = AllocateVirtualRegister(TypeSystem.BuiltIn.U4);

			if (offset.IsConstant && offset.IsConstantZero)
			{
				context.SetInstruction(X86.Mov, v1, address);
			}
			else
			{
				context.SetInstruction(X86.Add, v1, address, offset);
			}
			context.AppendInstruction(X86.Mov, v2, Operand.CreateMemoryAddress(TypeSystem.BuiltIn.U4, v1, 0));
			context.AppendInstruction(X86.Mov, op0L, v2);
			context.AppendInstruction(X86.Mov, v3, Operand.CreateMemoryAddress(TypeSystem.BuiltIn.U4, v1, 4));
			context.AppendInstruction(X86.Mov, op0H, v3);
		}

		/// <summary>
		/// Expands the store instruction for 64-bits.
		/// </summary>
		/// <param name="context">The context.</param>
		private void ExpandStore(Context context)
		{
			Operand address = context.Operand1;
			Operand offset = context.Operand2;

			Operand op0L, op0H;
			SplitLongOperand(context.Operand3, out op0L, out op0H);

			Operand v1 = AllocateVirtualRegister(TypeSystem.BuiltIn.U4);

			// Fortunately in 32-bit mode, we can't have 64-bit offsets, so this plan add should suffice.
			if (offset.IsConstant && offset.IsConstantOne)
			{
				context.SetInstruction(X86.Mov, v1, address);
			}
			else
			{
				context.SetInstruction(X86.Add, v1, address, offset);
			}

			context.AppendInstruction(X86.Mov, Operand.CreateMemoryAddress(TypeSystem.BuiltIn.U4, v1, 0), op0L);
			context.AppendInstruction(X86.Mov, Operand.CreateMemoryAddress(TypeSystem.BuiltIn.U4, v1, 4), op0H);
		}

		/// <summary>
		/// Expands the binary branch instruction for 64-bits.
		/// </summary>
		/// <param name="context">The context.</param>
		private void ExpandBinaryBranch(Context context)
		{
			//Debug.Assert(context.BranchTargets.Length == 1);

			Operand op1L, op1H, op2L, op2H;
			SplitLongOperand(context.Operand1, out op1L, out op1H);
			SplitLongOperand(context.Operand2, out op2L, out op2H);

			BasicBlock target = BasicBlocks.GetByLabel(context.BranchTargets[0]);
			ConditionCode conditionCode = context.ConditionCode;

			Context nextBlock = Split(context);
			Context[] newBlocks = CreateNewBlocksWithContexts(2);

			// FIXME: If the conditional branch and unconditional branch are the same, this could cause a problem
			target.PreviousBlocks.Remove(context.BasicBlock);

			// The block is being split on the condition, so the new next block has too one many next blocks!
			nextBlock.BasicBlock.NextBlocks.Remove(target);

			// Compare high dwords
			context.SetInstruction(X86.Cmp, null, op1H, op2H);
			context.AppendInstruction(X86.Branch, ConditionCode.Equal, newBlocks[1].BasicBlock);
			context.AppendInstruction(X86.Jmp, newBlocks[0].BasicBlock);
			LinkBlocks(context, newBlocks[0], newBlocks[1]);

			// Branch if check already gave results
			newBlocks[0].AppendInstruction(X86.Branch, conditionCode, target);
			newBlocks[0].AppendInstruction(X86.Jmp, nextBlock.BasicBlock);
			LinkBlocks(newBlocks[0], target, nextBlock.BasicBlock);

			// Compare low dwords
			newBlocks[1].AppendInstruction(X86.Cmp, null, op1L, op2L);
			newBlocks[1].AppendInstruction(X86.Branch, conditionCode.GetUnsigned(), target);
			newBlocks[1].AppendInstruction(X86.Jmp, nextBlock.BasicBlock);
			LinkBlocks(newBlocks[1], target, nextBlock.BasicBlock);
		}

		/// <summary>
		/// Expands the binary comparison instruction for 64-bits.
		/// </summary>
		/// <param name="context">The context.</param>
		private void ExpandComparison(Context context)
		{
			Operand op0 = context.Result;
			Operand op1 = context.Operand1;
			Operand op2 = context.Operand2;

			Debug.Assert(op1 != null && op2 != null, @"IntegerCompareInstruction operand not memory!");
			Debug.Assert(op0.IsMemoryAddress || op0.IsRegister, @"IntegerCompareInstruction result not memory and not register!");

			Operand op1L, op1H, op2L, op2H;
			SplitLongOperand(op1, out op1L, out op1H);
			SplitLongOperand(op2, out op2L, out op2H);

			ConditionCode conditionCode = context.ConditionCode;

			Context nextBlock = Split(context);
			Context[] newBlocks = CreateNewBlocksWithContexts(4);

			// Compare high dwords
			context.SetInstruction(X86.Cmp, null, op1H, op2H);
			context.AppendInstruction(X86.Branch, ConditionCode.Equal, newBlocks[1].BasicBlock);
			context.AppendInstruction(X86.Jmp, newBlocks[0].BasicBlock);
			LinkBlocks(context, newBlocks[0], newBlocks[1]);

			// Branch if check already gave results
			newBlocks[0].AppendInstruction(X86.Branch, conditionCode, newBlocks[2].BasicBlock);
			newBlocks[0].AppendInstruction(X86.Jmp, newBlocks[3].BasicBlock);
			LinkBlocks(newBlocks[0], newBlocks[2], newBlocks[3]);

			// Compare low dwords
			newBlocks[1].AppendInstruction(X86.Cmp, null, op1L, op2L);
			newBlocks[1].AppendInstruction(X86.Branch, conditionCode.GetUnsigned(), newBlocks[2].BasicBlock);
			newBlocks[1].AppendInstruction(X86.Jmp, newBlocks[3].BasicBlock);
			LinkBlocks(newBlocks[1], newBlocks[2], newBlocks[3]);

			// Success
			newBlocks[2].AppendInstruction(X86.Mov, op0, Operand.CreateConstantSignedInt(TypeSystem, (int)1));
			newBlocks[2].AppendInstruction(X86.Jmp, nextBlock.BasicBlock);
			LinkBlocks(newBlocks[2], nextBlock);

			// Failed
			newBlocks[3].AppendInstruction(X86.Mov, op0, constantZero);
			newBlocks[3].AppendInstruction(X86.Jmp, nextBlock.BasicBlock);
			LinkBlocks(newBlocks[3], nextBlock);
		}

		/// <summary>
		/// Ares the any64 bit.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <returns></returns>
		public static bool AreAny64Bit(Context context)
		{
			if (context.Result.Is64BitInteger)
				return true;

			foreach (var operand in context.Operands)
				if (operand.Is64BitInteger)
					return true;

			return false;
		}

		#endregion Utility Methods

		#region IIRVisitor

		/// <summary>
		/// Visitation function for ArithmeticShiftRightInstruction.
		/// </summary>
		/// <param name="context">The context.</param>
		void IIRVisitor.ArithmeticShiftRight(Context context)
		{
			if (context.Operand1.Is64BitInteger)
			{
				ExpandArithmeticShiftRight(context);
			}
		}

		/// <summary>
		/// Visitation function for IntegerCompareInstruction.
		/// </summary>
		/// <param name="context">The context.</param>
		void IIRVisitor.IntegerCompareBranch(Context context)
		{
			if (context.Operand1.Is64BitInteger || context.Operand2.Is64BitInteger)
			{
				ExpandBinaryBranch(context);
			}
		}

		/// <summary>
		/// Visitation function for IntegerCompare.
		/// </summary>
		/// <param name="context">The context.</param>
		void IIRVisitor.IntegerCompare(Context context)
		{
			if (context.Operand1.Is64BitInteger)
			{
				ExpandComparison(context);
			}
		}

		/// <summary>
		/// Visitation function for Load.
		/// </summary>
		/// <param name="context">The context.</param>
		void IIRVisitor.Load(Context context)
		{
			if (context.Operand1.Is64BitInteger || context.Result.Is64BitInteger)
			{
				ExpandLoad(context);
			}
		}

		void IIRVisitor.CompoundLoad(Context context)
		{
		}

		/// <summary>
		/// Visitation function for Load Zero Extended.
		/// </summary>
		/// <param name="context">The context.</param>
		void IIRVisitor.LoadZeroExtended(Context context)
		{
			if (AreAny64Bit(context))
			{
				return;
			}
		}

		/// <summary>
		/// Visitation function for Load Sign Extended.
		/// </summary>
		/// <param name="context">The context.</param>
		void IIRVisitor.LoadSignExtended(Context context)
		{
			// TODO
		}

		/// <summary>
		/// Visitation function for LogicalAndInstruction.
		/// </summary>
		/// <param name="context">The context.</param>
		void IIRVisitor.LogicalAnd(Context context)
		{
			if (AreAny64Bit(context))
			{
				ExpandAnd(context);
			}
		}

		/// <summary>
		/// Visitation function for LogicalOrInstruction.
		/// </summary>
		/// <param name="context">The context.</param>
		void IIRVisitor.LogicalOr(Context context)
		{
			if (AreAny64Bit(context))
			{
				ExpandOr(context);
			}
		}

		/// <summary>
		/// Visitation function for LogicalXorInstruction.
		/// </summary>
		/// <param name="context">The context.</param>
		void IIRVisitor.LogicalXor(Context context)
		{
			if (AreAny64Bit(context))
			{
				ExpandXor(context);
			}
		}

		/// <summary>
		/// Visitation function for LogicalNotInstruction.
		/// </summary>
		/// <param name="context">The context.</param>
		void IIRVisitor.LogicalNot(Context context)
		{
			if (AreAny64Bit(context))
			{
				ExpandNot(context);
			}
		}

		/// <summary>
		/// Visitation function for MoveInstruction.
		/// </summary>
		/// <param name="context">The context.</param>
		void IIRVisitor.Move(Context context)
		{
			if (AreAny64Bit(context))
			{
				ExpandMove(context);
			}
		}

		void IIRVisitor.CompoundMove(Context context)
		{
		}

		/// <summary>
		/// Visitation function for ShiftLeftInstruction.
		/// </summary>
		/// <param name="context">The context.</param>
		void IIRVisitor.ShiftLeft(Context context)
		{
			if (AreAny64Bit(context))
			{
				ExpandShiftLeft(context);
			}
		}

		/// <summary>
		/// Visitation function for ShiftRightInstruction.
		/// </summary>
		/// <param name="context">The context.</param>
		void IIRVisitor.ShiftRight(Context context)
		{
			if (AreAny64Bit(context))
			{
				ExpandShiftRight(context);
			}
		}

		/// <summary>
		/// Visitation function for SignExtendedMoveInstruction.
		/// </summary>
		/// <param name="context">The context.</param>
		void IIRVisitor.SignExtendedMove(Context context)
		{
			if (AreAny64Bit(context))
			{
				ExpandSignedMove(context);
			}
		}

		/// <summary>
		/// Visitation function for StoreInstruction.
		/// </summary>
		/// <param name="context">The context.</param>
		void IIRVisitor.Store(Context context)
		{
			if ((context.MosaType ?? context.Operand3.Type).IsUI8)
			{
				ExpandStore(context);
			}
		}

		void IIRVisitor.CompoundStore(Context context)
		{
		}

		/// <summary>
		/// Visitation function for DivSInstruction.
		/// </summary>
		/// <param name="context">The context.</param>
		void IIRVisitor.DivSigned(Context context)
		{
			if (AreAny64Bit(context))
			{
				ExpandDiv(context);
			}
		}

		/// <summary>
		/// Visitation function for DivUInstruction.
		/// </summary>
		/// <param name="context">The context.</param>
		void IIRVisitor.DivUnsigned(Context context)
		{
			if (AreAny64Bit(context))
			{
				ExpandUDiv(context);
			}
		}

		/// <summary>
		/// Visitation function for MulSInstruction.
		/// </summary>
		/// <param name="context">The context.</param>
		void IIRVisitor.MulSigned(Context context)
		{
			if (AreAny64Bit(context))
			{
				ExpandMul(context);
			}
		}

		/// <summary>
		/// Visitation function for MulUInstruction.
		/// </summary>
		/// <param name="context">The context.</param>
		void IIRVisitor.MulUnsigned(Context context)
		{
			if (AreAny64Bit(context))
			{
				ExpandMul(context);
			}
		}

		/// <summary>
		/// Visitation function for SubSInstruction.
		/// </summary>
		/// <param name="context">The context.</param>
		void IIRVisitor.SubSigned(Context context)
		{
			if (AreAny64Bit(context))
			{
				ExpandSub(context);
			}
		}

		/// <summary>
		/// Visitation function for SubUInstruction.
		/// </summary>
		/// <param name="context">The context.</param>
		void IIRVisitor.SubUnsigned(Context context)
		{
			if (AreAny64Bit(context))
			{
				ExpandSub(context);
			}
		}

		/// <summary>
		/// Visitation function for RemSigned.
		/// </summary>
		/// <param name="context">The context.</param>
		void IIRVisitor.RemSigned(Context context)
		{
			if (AreAny64Bit(context))
			{
				ExpandRem(context);
			}
		}

		/// <summary>
		/// Visitation function for RemUInstruction.
		/// </summary>
		/// <param name="context">The context.</param>
		void IIRVisitor.RemUnsigned(Context context)
		{
			if (AreAny64Bit(context))
			{
				ExpandURem(context);
			}
		}

		/// <summary>
		/// Zeroes the extended move instruction.
		/// </summary>
		/// <param name="context">The context.</param>
		void IIRVisitor.ZeroExtendedMove(Context context)
		{
			if (AreAny64Bit(context))
			{
				ExpandUnsignedMove(context);
			}
		}

		/// <summary>
		/// Visitation function for AddSigned.
		/// </summary>
		/// <param name="context">The context.</param>
		void IIRVisitor.AddSigned(Context context)
		{
			if (AreAny64Bit(context))
			{
				ExpandAdd(context);
			}
		}

		/// <summary>
		/// Visitation function for AddUnsigned.
		/// </summary>
		/// <param name="context">The context.</param>
		void IIRVisitor.AddUnsigned(Context context)
		{
			if (AreAny64Bit(context))
			{
				ExpandAdd(context);
			}
		}

		/// <summary>
		/// Visitation function for CallInstruction.
		/// </summary>
		/// <param name="context">The context.</param>
		void IIRVisitor.Call(Context context)
		{
			Operand op0L, op0H;

			if (context.Result != null && context.Result.Is64BitInteger)
			{
				SplitLongOperand(context.Result, out op0L, out op0H);
			}

			foreach (var operand in context.Operands)
			{
				if (operand.Is64BitInteger)
				{
					SplitLongOperand(operand, out op0L, out op0H);
				}
			}
		}

		/// <summary>
		/// Visitation function for ReturnInstruction.
		/// </summary>
		/// <param name="context">The context.</param>
		void IIRVisitor.Return(Context context)
		{
			Operand op0L, op0H;

			if (context.Result != null && context.Result.Is64BitInteger)
			{
				SplitLongOperand(context.Result, out op0L, out op0H);
			}

			foreach (var operand in context.Operands)
			{
				if (operand.Is64BitInteger)
				{
					SplitLongOperand(operand, out op0L, out op0H);
				}
			}
		}

		#endregion IIRVisitor

		#region IIRVisitor - Unused

		/// <summary>
		/// Visitation function for InternalCall.
		/// </summary>
		/// <param name="context">The context.</param>
		void IIRVisitor.InternalCall(Context context)
		{
		}

		/// <summary>
		/// Visitation function for InternalReturn.
		/// </summary>
		/// <param name="context">The context.</param>
		void IIRVisitor.InternalReturn(Context context)
		{
		}

		/// <summary>
		/// Visitation function for RemFloatInstruction.
		/// </summary>
		/// <param name="context">The context.</param>
		void IIRVisitor.RemFloat(Context context)
		{
		}

		/// <summary>
		/// Visitation function for SubFloatInstruction.
		/// </summary>
		/// <param name="context">The context.</param>
		void IIRVisitor.SubFloat(Context context)
		{
		}

		/// <summary>
		/// Visitation function for SwitchInstruction.
		/// </summary>
		/// <param name="context">The context.</param>
		void IIRVisitor.Switch(Context context)
		{
		}

		/// <summary>
		/// Visitation function for AddFloat.
		/// </summary>
		/// <param name="context">The context.</param>
		void IIRVisitor.AddFloat(Context context)
		{
		}

		/// <summary>
		/// Visitation function for DivFloat.
		/// </summary>
		/// <param name="context">The context.</param>
		void IIRVisitor.DivFloat(Context context)
		{
		}

		/// <summary>
		/// Visitation function for BreakInstruction.
		/// </summary>
		/// <param name="context">The context.</param>
		void IIRVisitor.Break(Context context)
		{
		}

		/// <summary>
		/// Visitation function for AddressOfInstruction.
		/// </summary>
		/// <param name="context">The context.</param>
		void IIRVisitor.AddressOf(Context context)
		{
		}

		/// <summary>
		/// Visitation function for intrinsic the method call.
		/// </summary>
		/// <param name="context">The context.</param>
		void IIRVisitor.IntrinsicMethodCall(Context context)
		{
		}

		/// <summary>
		/// Visitation function for EpilogueInstruction.
		/// </summary>
		/// <param name="context">The context.</param>
		void IIRVisitor.Epilogue(Context context)
		{
		}

		/// <summary>
		/// Visitation function for FloatingPointCompareInstruction.
		/// </summary>
		/// <param name="context">The context.</param>
		void IIRVisitor.FloatCompare(Context context)
		{
		}

		/// <summary>
		/// Visitation function for FloatingPointToIntegerConversionInstruction.
		/// </summary>
		/// <param name="context">The context.</param>
		void IIRVisitor.FloatToIntegerConversion(Context context)
		{
		}

		/// <summary>
		/// Visitation function for IntegerToFloatingPointConversionInstruction.
		/// </summary>
		/// <param name="context">The context.</param>
		void IIRVisitor.IntegerToFloatConversion(Context context)
		{
		}

		/// <summary>
		/// Visitation function for JmpInstruction instruction.
		/// </summary>
		/// <param name="context">The context.</param>
		void IIRVisitor.Jmp(Context context)
		{
		}

		/// <summary>
		/// Visitation function for PhiInstruction.
		/// </summary>
		/// <param name="context">The context.</param>
		void IIRVisitor.Phi(Context context)
		{
		}

		/// <summary>
		/// Visitation function for PrologueInstruction.
		/// </summary>
		/// <param name="context">The context.</param>
		void IIRVisitor.Prologue(Context context)
		{
		}

		/// <summary>
		/// Visitation function for NopInstruction.
		/// </summary>
		/// <param name="context">The context.</param>
		void IIRVisitor.Nop(Context context)
		{
		}

		/// <summary>
		/// Visitation function for MulFloatInstruction.
		/// </summary>
		/// <param name="context">The context.</param>
		void IIRVisitor.MulFloat(Context context)
		{
		}

		/// <summary>
		/// Visitation function for ThrowInstruction.
		/// </summary>
		/// <param name="context">The context.</param>
		void IIRVisitor.Throw(Context context)
		{
		}

		/// <summary>
		/// Visitation function for StartTry.
		/// </summary>
		/// <param name="context">The context.</param>
		void IIRVisitor.TryStart(Context context)
		{
		}

		/// <summary>
		/// Visitation function for StartException.
		/// </summary>
		/// <param name="context">The context.</param>
		void IIRVisitor.ExceptionStart(Context context)
		{
		}

		/// <summary>
		/// Visitation function for StartFinally.
		/// </summary>
		/// <param name="context">The context.</param>
		void IIRVisitor.FinallyStart(Context context)
		{
		}

		/// <summary>
		/// Visitation function for EndTry.
		/// </summary>
		/// <param name="context">The context.</param>
		void IIRVisitor.TryEnd(Context context)
		{
		}

		/// <summary>
		/// Visitation function for EndException.
		/// </summary>
		/// <param name="context">The context.</param>
		void IIRVisitor.ExceptionEnd(Context context)
		{
		}

		/// <summary>
		/// Visitation function for EndFinally.
		/// </summary>
		/// <param name="context">The context.</param>
		void IIRVisitor.FinallyEnd(Context context)
		{
		}

		/// <summary>
		/// Visitation function for CallFinally.
		/// </summary>
		/// <param name="context">The context.</param>
		void IIRVisitor.CallFinally(Context context)
		{
		}

		#endregion IIRVisitor - Unused
	}
}