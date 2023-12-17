class Instruction
{
    public InstructionType instructionType = InstructionType.NOOP;
    public List<Token> args = new List<Token>();
}
enum InstructionType{
    NOOP,
    CREATE_VARIABLE,
    OPERATION,
    CALL_FUNCTION,
    CREATE_JP,
    CONDITION,
    JUMP_TO,

};