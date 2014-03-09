using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.NRefactory.TypeSystem;
using Saltarelle.Compiler;
using Saltarelle.Compiler.ScriptSemantics;

namespace System.Runtime.CompilerServices {
	public partial class DefaultMemberReflectabilityAttribute {
		public override void ApplyTo(IAssembly assembly, IAttributeStore attributeStore, IErrorReporter errorReporter) {
			foreach (var t in assembly.GetAllTypeDefinitions()) {
				if (!attributeStore.AttributesFor(t).HasAttribute<DefaultMemberReflectabilityAttribute>()) {
					ApplyTo(t, attributeStore, errorReporter);
				}
			}
		}

		public override void ApplyTo(IEntity entity, IAttributeStore attributeStore, IErrorReporter errorReporter) {
			var type = entity as ITypeDefinition;
			if (type == null)
				return;

			foreach (var m in type.Members) {
				var attributes = attributeStore.AttributesFor(m);
				if (!attributes.HasAttribute<ReflectableAttribute>()) {
					if (IsMemberReflectable(m)) {
						attributes.Add(new ReflectableAttribute(true));
					}
				}
			}

			if (Cascades)
			{
				if (type.Kind == TypeKind.Interface) {
					// Disallow interfaces as this creates ambiguity
					errorReporter.Message(CoreLib.Plugin.Messages._7171);
				}
				else {
					// Cascade this attribute to direct children only. That way, subclasses will always inherit the attribute
					// from their most direct parent.
					var directChildren = type.GetSubTypeDefinitions().Where(subType => subType != type && subType.DirectBaseTypes.Contains(type));

					foreach (var childType in directChildren) {
						var childAttributes = attributeStore.AttributesFor(childType);
						if (!childAttributes.HasAttribute<DefaultMemberReflectabilityAttribute>()) {
							childAttributes.Add(this);
							this.ApplyTo(childType, attributeStore, errorReporter);
						}
					}
				}
			}
		}

		private bool IsMemberReflectable(IMember member) {
			switch (DefaultReflectability) {
				case MemberReflectability.None:
					return false;
				case MemberReflectability.PublicAndProtected:
					return !member.IsPrivate && !member.IsInternal;
				case MemberReflectability.NonPrivate:
					return !member.IsPrivate;
				case MemberReflectability.All:
					return true;
				default:
					throw new ArgumentException("reflectability");
			}
		}
	}
}
