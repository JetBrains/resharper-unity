{caret}void foo()
{
    switch(a){
        case 1:
        break;
        case 2:
            foo();
            return;
        case 3:
            foo();
            break;
        case 4:
        default:
            foo();
            break;
    }
}