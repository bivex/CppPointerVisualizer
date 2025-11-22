using System;
using System.Globalization;
using Antlr4.Runtime;
using CppPointerVisualizer.Models;
using CppPointerVisualizer.Grammar;

namespace CppPointerVisualizer.Parser
{
    public class CppPointerAntlrParser
    {
        public MemoryState Parse(string code)
        {
            var inputStream = new AntlrInputStream(code);
            var lexer = new CppPointerLexer(inputStream);
            var tokenStream = new CommonTokenStream(lexer);
            var parser = new CppPointerParser(tokenStream);

            var tree = parser.program();
            var visitor = new MemoryStateVisitor();

            return visitor.Visit(tree);
        }
    }

    internal class MemoryStateVisitor : CppPointerBaseVisitor<MemoryState>
    {
        private readonly MemoryState _state;
        private int _addressCounter = 0x1000;

        public MemoryStateVisitor()
        {
            _state = new MemoryState();
        }

        public override MemoryState VisitProgram(CppPointerParser.ProgramContext context)
        {
            foreach (var statement in context.statement())
            {
                Visit(statement);
            }
            return _state;
        }

        public override MemoryState VisitStatement(CppPointerParser.StatementContext context)
        {
            if (context.declaration() != null)
            {
                Visit(context.declaration());
            }
            return _state;
        }

        public override MemoryState VisitVariableDeclaration(CppPointerParser.VariableDeclarationContext context)
        {
            bool isConst = context.CONST() != null;
            string type = context.type().GetText();
            string name = context.IDENTIFIER().GetText();
            object value = EvaluateExpression(context.expression());

            var obj = new MemoryObject
            {
                Name = name,
                Type = type,
                Value = value,
                ObjectType = MemoryObjectType.Variable,
                Address = GenerateAddress(),
                IsConst = isConst
            };

            _state.Objects.Add(obj);
            return _state;
        }

        public override MemoryState VisitPointerDeclaration(CppPointerParser.PointerDeclarationContext context)
        {
            var constTokens = context.CONST();
            bool isConst = false;
            bool isPointerConst = false;

            if (constTokens != null && constTokens.Length > 0)
            {
                // Check position of first CONST token relative to type
                if (constTokens[0].Symbol.TokenIndex < context.type().Start.TokenIndex)
                {
                    isConst = true;
                }

                // Check if there's a CONST after the pointer operator(s)
                if (constTokens.Length > 1 ||
                    (constTokens.Length == 1 && constTokens[0].Symbol.TokenIndex > context.type().Stop.TokenIndex))
                {
                    isPointerConst = true;
                    if (constTokens.Length > 1) isConst = true;
                }
            }

            string type = context.type().GetText();
            string name = context.IDENTIFIER().GetText();
            int pointerLevel = context.pointerOperator().Length;

            string? pointsTo = null;
            if (context.expression() is CppPointerParser.AddressOfExprContext addressOf)
            {
                string targetName = addressOf.IDENTIFIER().GetText();
                var target = _state.GetObjectByName(targetName);
                if (target != null)
                {
                    pointsTo = target.Address;
                }
            }
            else if (context.expression() is CppPointerParser.NullptrExprContext ||
                     context.expression() is CppPointerParser.NullExprContext ||
                     context.expression() is CppPointerParser.ZeroExprContext)
            {
                pointsTo = "nullptr";
            }

            var obj = new MemoryObject
            {
                Name = name,
                Type = type,
                Value = pointsTo,
                ObjectType = MemoryObjectType.Pointer,
                Address = GenerateAddress(),
                PointsTo = pointsTo,
                IsConst = isConst,
                IsPointerConst = isPointerConst,
                PointerLevel = pointerLevel
            };

            _state.Objects.Add(obj);
            return _state;
        }

        public override MemoryState VisitReferenceDeclaration(CppPointerParser.ReferenceDeclarationContext context)
        {
            var constTokens = context.CONST();
            bool isConst = constTokens != null && constTokens.Length > 0 &&
                          constTokens[0].Symbol.TokenIndex < context.type().Start.TokenIndex;

            string type = context.type().GetText();
            string name = context.IDENTIFIER().GetText();

            string? pointsTo = null;
            object? value = null;

            if (context.expression() is CppPointerParser.IdentifierExprContext identExpr)
            {
                string targetName = identExpr.IDENTIFIER().GetText();
                var target = _state.GetObjectByName(targetName);
                if (target != null)
                {
                    pointsTo = target.Address;
                    value = target.Value;
                }
            }
            else if (context.expression() is CppPointerParser.DereferenceExprContext derefExpr)
            {
                string ptrName = derefExpr.IDENTIFIER().GetText();
                var ptrTarget = _state.GetObjectByName(ptrName);
                if (ptrTarget != null && ptrTarget.PointsTo != null)
                {
                    // Reference points to what the pointer points to
                    pointsTo = ptrTarget.PointsTo;
                    var finalTarget = _state.GetObjectByAddress(ptrTarget.PointsTo);
                    if (finalTarget != null)
                    {
                        value = finalTarget.Value;
                    }
                }
            }

            var obj = new MemoryObject
            {
                Name = name,
                Type = type,
                Value = value,
                ObjectType = MemoryObjectType.Reference,
                Address = GenerateAddress(),
                PointsTo = pointsTo,
                IsConst = isConst
            };

            _state.Objects.Add(obj);
            return _state;
        }

        private object EvaluateExpression(CppPointerParser.ExpressionContext context)
        {
            if (context is CppPointerParser.NumberExprContext numberExpr)
            {
                return int.Parse(numberExpr.NUMBER().GetText());
            }
            else if (context is CppPointerParser.FloatExprContext floatExpr)
            {
                return double.Parse(floatExpr.FLOAT_NUMBER().GetText(), CultureInfo.InvariantCulture);
            }
            else if (context is CppPointerParser.StringExprContext stringExpr)
            {
                string text = stringExpr.STRING().GetText();
                return text.Substring(1, text.Length - 2); // Remove quotes
            }
            else if (context is CppPointerParser.IdentifierExprContext identExpr)
            {
                return identExpr.IDENTIFIER().GetText();
            }

            return context.GetText();
        }

        private string GenerateAddress()
        {
            string addr = $"0x{_addressCounter:X4}";
            _addressCounter += 4;
            return addr;
        }

        protected override MemoryState DefaultResult => _state;

        protected override MemoryState AggregateResult(MemoryState aggregate, MemoryState nextResult)
        {
            return aggregate ?? nextResult;
        }
    }
}
