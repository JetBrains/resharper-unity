{caret}
void foo()
{
    noArgs();

    float f;
    oneArg(0);
    oneArg(f);

    twoArgs(0, 0);
    twoArgs(f, 0);
    twoArgs(0, f);
    twoArgs(f, f);
}

void noArgs(){}
void oneArg(float f){}
void twoArgs(float f1, float f2){}