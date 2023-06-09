﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Main.Core.Entities.Composite;

namespace WB.Core.SharedKernels.QuestionnaireEntities
{
    [DebuggerDisplay("Variable {PublicKey}")]
    public class Variable : IVariable
    {
        //used for deserialization, change carefuly
        public Variable(Guid publicKey, VariableData? variableData, List<IComposite>? children = null)
        {
            this.PublicKey = publicKey;
            Expression = String.Empty;
            Label = String.Empty;
            Name = String.Empty;
            
            if (variableData != null)
            {
                this.Type = variableData.Type;
                this.Name = variableData.Name;
                this.Expression = variableData.Expression;
                this.Label = variableData.Label;
                this.DoNotExport = variableData.DoNotExport;
            }
        }

        public string Label { get; set; }
        public Guid PublicKey { get; set; }
        public VariableType Type { get; set; }
        public string Name { get; set; }
        public string Expression { get; set; }
        public bool DoNotExport { get; set; }

        private ReadOnlyCollection<IComposite> children = new ReadOnlyCollection<IComposite>(new List<IComposite>(0));

        public ReadOnlyCollection<IComposite> Children
        {
            get
            {
                return children;
            }
            set
            {
                // do nothing
            }
        }

        private IComposite? parent;

        public IComposite? GetParent()
        {
            return this.parent;
        }

        public void SetParent(IComposite? parent)
        {
            this.parent = parent;
        }

        public T? Find<T>(Guid publicKey) where T : class, IComposite
        {
            return null;
        }

        public IEnumerable<T> Find<T>(Func<T, bool> condition) where T : class
        {
            return new T[0];
        }

        public T? FirstOrDefault<T>(Func<T, bool> condition) where T : class
        {
            return null;
        }

        public void ConnectChildrenWithParent()
        {
        }

        public IComposite Clone()
        {
            var variable = (IVariable)this.MemberwiseClone();
            variable.SetParent(null);
            return variable;
        }

        public void Insert(int index, IComposite itemToInsert, Guid? parent)
        {
        }

        public void RemoveChild(Guid child)
        {
        }

        public string VariableName => this.Name;
    }
}
