﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    partial class BoundQueryClause
    {
        /// <summary>
        /// The bound expression that invokes the operation of the query clause.
        /// </summary>
        public BoundExpression Operation { get; private set; }

        /// <summary>
        /// The bound expression that is the invocation of a "Cast" method specified by the query translation.
        /// </summary>
        public BoundExpression Cast { get; private set; }

        /// <summary>
        /// The bound expression that is the query expression in "unoptimized" form.  Specifically, a final ".Select"
        /// invocation that is omitted by the specification is included here.
        /// </summary>
        public BoundExpression UnoptimizedForm { get; private set; }

        public BoundQueryClause(
            CSharpSyntaxNode syntax,
            BoundExpression value,
            RangeVariableSymbol definedSymbol,
            BoundExpression queryInvocation,
            BoundExpression castInvocation,
            Binder binder,
            BoundExpression unoptimizedForm,
            TypeSymbol type,
            bool hasErrors = false)
            : this(syntax, value, definedSymbol, binder, type, hasErrors)
        {
            this.Operation = queryInvocation;
            this.Cast = castInvocation;
            this.UnoptimizedForm = unoptimizedForm;
        }

        public BoundQueryClause Update(
            BoundExpression value,
            RangeVariableSymbol definedSymbol,
            BoundExpression queryInvocation,
            BoundExpression castInvocation,
            Binder binder,
            BoundExpression unoptimizedForm,
            TypeSymbol type)
        {
            if (value != this.Value || definedSymbol != this.DefinedSymbol || queryInvocation != this.Operation || castInvocation != this.Cast || binder != this.Binder || unoptimizedForm != this.UnoptimizedForm || type != this.Type)
            {
                var result = new BoundQueryClause(this.Syntax, value, definedSymbol, queryInvocation, castInvocation, binder, unoptimizedForm, type, this.HasErrors);
                result.WasCompilerGenerated = this.WasCompilerGenerated;
                return result;
            }
            return this;
        }
    }
}
