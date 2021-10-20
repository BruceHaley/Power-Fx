﻿//------------------------------------------------------------------------------
// <copyright company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using Microsoft.AppMagic.Authoring;
using Microsoft.AppMagic.Authoring.Texl;
using Microsoft.PowerFx.Core.IR;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Microsoft.PowerFx
{
    [DebuggerDisplay("{_tree}")]
    internal class LambdaFormulaValue : FormulaValue
    {
        private readonly IntermediateNode _tree;

        // Lambdas don't get a special type. 
        // Type is the type the lambda evaluates too. 
        public LambdaFormulaValue(IRContext irContext, IntermediateNode node)
            : base(irContext)
        {
            this._tree = node;
        }

        public FormulaValue Eval(EvalVisitor runner, SymbolContext context)
        {
            FormulaValue result = _tree.Accept(runner, context);
            return result;
        }

        public override object ToObject()
        {
            return "<Lambda>";
        }

        public override void Visit(IValueVisitor visitor)
        {
            throw new NotImplementedException();
        }
    }
}