{caret}void foo()
{
    float f;

    // literals
    f = 1;
    f = -1;
    f = +1;
    
    f = +f;
    f = -f;

    f = +(f);
    f = -(f);

    f = -getFloat();    

    bool b;

    b = 0;

    b = !0;
    b = !1;
    b = !b;
    b = ~b;
}