using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Rubberduck.Parsing;
using Rubberduck.Parsing.Symbols;
using Rubberduck.Parsing.VBA;

namespace Rubberduck.Inspections
{
    public sealed class UnassignedVariableUsageInspection : InspectionBase
    {
        public UnassignedVariableUsageInspection(RubberduckParserState state)
            : base(state, CodeInspectionSeverity.Error)
        {
        }

        public override string Meta { get { return InspectionsUI.UnassignedVariableUsageInspectionMeta; } }
        public override CodeInspectionType InspectionType { get { return CodeInspectionType.CodeQualityIssues; } }
        public override string Description { get { return InspectionsUI.UnassignedVariableUsageInspectionName; } }

        public override IEnumerable<InspectionResultBase> GetInspectionResults()
        {
            var declarations = UserDeclarations.Where(declaration => 
                declaration.DeclarationType == DeclarationType.Variable
                && !UserDeclarations.Any(d => d.DeclarationType == DeclarationType.UserDefinedType
                    && d.IdentifierName == declaration.AsTypeName)
                && !declaration.IsSelfAssigned
                && !declaration.References.Any(reference => reference.IsAssignment));

            var lenFunction = BuiltInDeclarations.SingleOrDefault(s => s.Scope == "VBE7.DLL;VBA.Strings.Len");
            var lenbFunction = BuiltInDeclarations.SingleOrDefault(s => s.Scope == "VBE7.DLL;VBA.Strings.LenB");

            foreach (var issue in declarations)
            {
                if (DeclarationReferencesContainsReference(lenFunction, issue) ||
                    DeclarationReferencesContainsReference(lenbFunction, issue))
                {
                    continue;
                }

                yield return new UnassignedVariableUsageInspectionResult(this, issue.Context, issue.QualifiedName.QualifiedModuleName, issue);
            }
        }

        private bool DeclarationReferencesContainsReference(Declaration parentDeclaration, Declaration target)
        {
            if (parentDeclaration == null)
            {
                return false;
            }
            
            foreach (var targetReference in target.References)
            {
                foreach (var reference in parentDeclaration.References)
                {
                    var context = (ParserRuleContext) reference.Context.Parent;
                    if (context.GetSelection().Contains(targetReference.Selection))
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }
    }
}
