{caret}static const half2x2 foo = {
	0.3, 0.3,
	0.3, 0.3
};

float foo1 : POSITION : SV_POSITION : packoffset(2) : register(3);
float foo2 : packoffset(2) : register(3) : POSITION : SV_POSITION;