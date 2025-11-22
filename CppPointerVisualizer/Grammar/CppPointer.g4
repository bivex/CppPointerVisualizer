grammar CppPointer;

// Parser rules
program
    : statement* EOF
    ;

statement
    : declaration ';'
    | COMMENT
    | NEWLINE
    ;

declaration
    : variableDeclaration
    | pointerDeclaration
    | referenceDeclaration
    ;

variableDeclaration
    : CONST? type IDENTIFIER '=' expression
    ;

pointerDeclaration
    : CONST? type pointerOperator+ CONST? IDENTIFIER '=' expression
    ;

referenceDeclaration
    : CONST? type pointerOperator* CONST? '&' IDENTIFIER '=' expression
    ;

pointerOperator
    : '*'
    ;

type
    : IDENTIFIER
    ;

expression
    : IDENTIFIER                    # IdentifierExpr
    | NUMBER                        # NumberExpr
    | FLOAT_NUMBER                  # FloatExpr
    | STRING                        # StringExpr
    | '&' IDENTIFIER                # AddressOfExpr
    | '*' IDENTIFIER                # DereferenceExpr
    | NULLPTR                       # NullptrExpr
    | NULL                          # NullExpr
    | '0'                           # ZeroExpr
    ;

// Lexer rules
CONST       : 'const';
NULLPTR     : 'nullptr';
NULL        : 'NULL';

IDENTIFIER  : [a-zA-Z_][a-zA-Z0-9_]*;
NUMBER      : [0-9]+;
FLOAT_NUMBER: [0-9]+ '.' [0-9]+;
STRING      : '"' (~["\r\n])* '"';

COMMENT     : '//' ~[\r\n]* -> skip;
NEWLINE     : [\r\n]+ -> skip;
WS          : [ \t]+ -> skip;
