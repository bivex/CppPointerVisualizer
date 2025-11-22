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
    | arrayDeclaration
    ;

// Обычная переменная: int val = 512;
variableDeclaration
    : CONST? type IDENTIFIER '=' expression
    ;

// Указатели (включая многоуровневые): int* p, int** pp, const int* pc
pointerDeclaration
    : constQualifier* type pointerSpec+ IDENTIFIER '=' expression
    ;

// Спецификатор указателя с возможным const
pointerSpec
    : '*' CONST?
    ;

// Ссылки (включая ссылки на указатели): int& r, int*& pr
referenceDeclaration
    : CONST? type pointerSpec* '&' CONST? IDENTIFIER '=' expression
    ;

// Массивы: int arr[3] = {1,2,3}
arrayDeclaration
    : CONST? type IDENTIFIER '[' NUMBER ']' '=' arrayInitializer
    ;

arrayInitializer
    : '{' expression (',' expression)* '}'
    ;

constQualifier
    : CONST
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
    | '&' IDENTIFIER '[' NUMBER ']' # ArrayAddressExpr
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
