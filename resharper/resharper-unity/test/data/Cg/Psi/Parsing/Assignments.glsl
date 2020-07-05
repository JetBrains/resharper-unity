{caret}
struct bar
{
    int zzz;
};

void foo()
{
    bar.a = 2;
    float b;    
    bar bar_a;

    b = 0;
    b = a;
    
    b = bar_a.zzz;    
    
    bar_a.zzz = 0;
    bar_a.zzz = bar_a.zzz;
}