﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Shared.Collections
{
    internal class SimpleIntervalTree<T> : IntervalTree<T>
    {
        private readonly IIntervalIntrospector<T> introspector;

        public SimpleIntervalTree(IIntervalIntrospector<T> introspector) : this(introspector, root: null)
        {
        }

        protected SimpleIntervalTree(IIntervalIntrospector<T> introspector, Node root) : base(root)
        {
            if (introspector == null)
            {
                throw new ArgumentNullException("introspector");
            }

            this.introspector = introspector;
        }

        public SimpleIntervalTree(IIntervalIntrospector<T> introspector, IEnumerable<T> values)
            : this(introspector, root: null)
        {
            if (values != null)
            {
                foreach (var value in values)
                {
                    root = Insert(root, new Node(introspector, value), introspector, inPlace: true);
                }
            }
        }

        protected IIntervalIntrospector<T> Introspector
        {
            get { return introspector; }
        }

        public IEnumerable<T> GetOverlappingIntervals(int start, int length)
        {
            return GetOverlappingIntervals(start, length, this.introspector);
        }

        public IEnumerable<T> GetIntersectingIntervals(int start, int length)
        {
            return GetIntersectingIntervals(start, length, this.introspector);
        }

        public IEnumerable<T> GetContainingIntervals(int start, int length)
        {
            return GetContainingIntervals(start, length, this.introspector);
        }

        public bool IntersectsWith(int position)
        {
            return GetIntersectingIntervals(position, 0).Any();
        }

        public SimpleIntervalTree<T> AddInterval(T value)
        {
            var newNode = new Node(introspector, value);
            return new SimpleIntervalTree<T>(introspector, Insert(root, newNode, introspector, inPlace: false));
        }

        protected int MaxEndValue(Node node)
        {
            return GetEnd(node.MaxEndNode.Value, introspector);
        }
    }
}
