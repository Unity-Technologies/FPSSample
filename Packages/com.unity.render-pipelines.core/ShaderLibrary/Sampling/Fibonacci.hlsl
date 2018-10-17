#ifndef UNITY_FIBONACCI_INCLUDED
#define UNITY_FIBONACCI_INCLUDED

// Computes a point using the Fibonacci sequence of length N.
// Input: Fib[N - 1], Fib[N - 2], and the index 'i' of the point.
// Ref: Efficient Quadrature Rules for Illumination Integrals
real2 Fibonacci2dSeq(real fibN1, real fibN2, uint i)
{
    // 3 cycles on GCN if 'fibN1' and 'fibN2' are known at compile time.
    // N.b.: According to Swinbank and Pusser [SP06], the uniformity of the distribution
    // can be slightly improved by introducing an offset of 1/N to the Z (or R) coordinates.
    return real2(i / fibN1 + (0.5 / fibN1), frac(i * (fibN2 / fibN1)));
}

#define GOLDEN_RATIO 1.6180339887498948482

// Replaces the Fibonacci sequence in Fibonacci2dSeq() with the Golden ratio.
real2 Golden2dSeq(uint i, real n)
{
    return real2(i / n + (0.5 / n), frac(i * rcp(GOLDEN_RATIO)));
}

static const uint k_FibonacciSeq[] = {
    0, 1, 1, 2, 3, 5, 8, 13, 21, 34, 55, 89, 144, 233, 377, 610, 987, 1597, 2584, 4181
};

static const real2 k_Fibonacci2dSeq21[] = {
    real2(0.02380952, 0.00000000),
    real2(0.07142857, 0.61904764),
    real2(0.11904762, 0.23809528),
    real2(0.16666667, 0.85714293),
    real2(0.21428572, 0.47619057),
    real2(0.26190478, 0.09523821),
    real2(0.30952382, 0.71428585),
    real2(0.35714287, 0.33333349),
    real2(0.40476191, 0.95238113),
    real2(0.45238096, 0.57142878),
    real2(0.50000000, 0.19047642),
    real2(0.54761904, 0.80952406),
    real2(0.59523809, 0.42857170),
    real2(0.64285713, 0.04761887),
    real2(0.69047618, 0.66666698),
    real2(0.73809522, 0.28571510),
    real2(0.78571427, 0.90476227),
    real2(0.83333331, 0.52380943),
    real2(0.88095236, 0.14285755),
    real2(0.92857140, 0.76190567),
    real2(0.97619045, 0.38095284)
};

static const real2 k_Fibonacci2dSeq34[] = {
    real2(0.01470588, 0.00000000),
    real2(0.04411765, 0.61764705),
    real2(0.07352941, 0.23529410),
    real2(0.10294118, 0.85294116),
    real2(0.13235295, 0.47058821),
    real2(0.16176471, 0.08823538),
    real2(0.19117647, 0.70588231),
    real2(0.22058824, 0.32352924),
    real2(0.25000000, 0.94117641),
    real2(0.27941176, 0.55882359),
    real2(0.30882353, 0.17647076),
    real2(0.33823529, 0.79411745),
    real2(0.36764705, 0.41176462),
    real2(0.39705881, 0.02941132),
    real2(0.42647058, 0.64705849),
    real2(0.45588234, 0.26470566),
    real2(0.48529410, 0.88235283),
    real2(0.51470590, 0.50000000),
    real2(0.54411763, 0.11764717),
    real2(0.57352942, 0.73529434),
    real2(0.60294116, 0.35294151),
    real2(0.63235295, 0.97058773),
    real2(0.66176468, 0.58823490),
    real2(0.69117647, 0.20588207),
    real2(0.72058821, 0.82352924),
    real2(0.75000000, 0.44117641),
    real2(0.77941179, 0.05882263),
    real2(0.80882353, 0.67646980),
    real2(0.83823532, 0.29411697),
    real2(0.86764705, 0.91176414),
    real2(0.89705884, 0.52941132),
    real2(0.92647058, 0.14705849),
    real2(0.95588237, 0.76470566),
    real2(0.98529410, 0.38235283)
};

static const real2 k_Fibonacci2dSeq55[] = {
    real2(0.00909091, 0.00000000),
    real2(0.02727273, 0.61818182),
    real2(0.04545455, 0.23636365),
    real2(0.06363636, 0.85454547),
    real2(0.08181818, 0.47272730),
    real2(0.10000000, 0.09090900),
    real2(0.11818182, 0.70909095),
    real2(0.13636364, 0.32727289),
    real2(0.15454546, 0.94545460),
    real2(0.17272727, 0.56363630),
    real2(0.19090909, 0.18181801),
    real2(0.20909090, 0.80000019),
    real2(0.22727273, 0.41818190),
    real2(0.24545455, 0.03636360),
    real2(0.26363635, 0.65454578),
    real2(0.28181818, 0.27272701),
    real2(0.30000001, 0.89090919),
    real2(0.31818181, 0.50909138),
    real2(0.33636364, 0.12727261),
    real2(0.35454544, 0.74545479),
    real2(0.37272727, 0.36363602),
    real2(0.39090911, 0.98181820),
    real2(0.40909091, 0.60000038),
    real2(0.42727274, 0.21818161),
    real2(0.44545454, 0.83636379),
    real2(0.46363637, 0.45454597),
    real2(0.48181817, 0.07272720),
    real2(0.50000000, 0.69090843),
    real2(0.51818180, 0.30909157),
    real2(0.53636366, 0.92727280),
    real2(0.55454546, 0.54545403),
    real2(0.57272726, 0.16363716),
    real2(0.59090906, 0.78181839),
    real2(0.60909092, 0.39999962),
    real2(0.62727273, 0.01818275),
    real2(0.64545453, 0.63636398),
    real2(0.66363639, 0.25454521),
    real2(0.68181819, 0.87272835),
    real2(0.69999999, 0.49090958),
    real2(0.71818179, 0.10909081),
    real2(0.73636365, 0.72727203),
    real2(0.75454545, 0.34545517),
    real2(0.77272725, 0.96363640),
    real2(0.79090911, 0.58181763),
    real2(0.80909091, 0.20000076),
    real2(0.82727271, 0.81818199),
    real2(0.84545457, 0.43636322),
    real2(0.86363637, 0.05454636),
    real2(0.88181818, 0.67272758),
    real2(0.89999998, 0.29090881),
    real2(0.91818184, 0.90909195),
    real2(0.93636364, 0.52727318),
    real2(0.95454544, 0.14545441),
    real2(0.97272730, 0.76363754),
    real2(0.99090910, 0.38181686)
};

static const real2 k_Fibonacci2dSeq89[] = {
    real2(0.00561798, 0.00000000),
    real2(0.01685393, 0.61797750),
    real2(0.02808989, 0.23595500),
    real2(0.03932584, 0.85393250),
    real2(0.05056180, 0.47191000),
    real2(0.06179775, 0.08988762),
    real2(0.07303371, 0.70786500),
    real2(0.08426967, 0.32584238),
    real2(0.09550562, 0.94382000),
    real2(0.10674157, 0.56179762),
    real2(0.11797753, 0.17977524),
    real2(0.12921348, 0.79775238),
    real2(0.14044943, 0.41573000),
    real2(0.15168539, 0.03370762),
    real2(0.16292135, 0.65168476),
    real2(0.17415731, 0.26966286),
    real2(0.18539326, 0.88764000),
    real2(0.19662921, 0.50561714),
    real2(0.20786516, 0.12359524),
    real2(0.21910113, 0.74157238),
    real2(0.23033708, 0.35955048),
    real2(0.24157304, 0.97752762),
    real2(0.25280899, 0.59550476),
    real2(0.26404494, 0.21348286),
    real2(0.27528089, 0.83146000),
    real2(0.28651685, 0.44943714),
    real2(0.29775280, 0.06741524),
    real2(0.30898875, 0.68539238),
    real2(0.32022473, 0.30336952),
    real2(0.33146068, 0.92134666),
    real2(0.34269664, 0.53932571),
    real2(0.35393259, 0.15730286),
    real2(0.36516854, 0.77528000),
    real2(0.37640449, 0.39325714),
    real2(0.38764045, 0.01123428),
    real2(0.39887640, 0.62921333),
    real2(0.41011235, 0.24719048),
    real2(0.42134830, 0.86516762),
    real2(0.43258426, 0.48314476),
    real2(0.44382024, 0.10112190),
    real2(0.45505619, 0.71910095),
    real2(0.46629214, 0.33707809),
    real2(0.47752810, 0.95505524),
    real2(0.48876405, 0.57303238),
    real2(0.50000000, 0.19100952),
    real2(0.51123595, 0.80898666),
    real2(0.52247190, 0.42696571),
    real2(0.53370786, 0.04494286),
    real2(0.54494381, 0.66292000),
    real2(0.55617976, 0.28089714),
    real2(0.56741571, 0.89887428),
    real2(0.57865167, 0.51685333),
    real2(0.58988762, 0.13483047),
    real2(0.60112357, 0.75280762),
    real2(0.61235952, 0.37078476),
    real2(0.62359548, 0.98876190),
    real2(0.63483149, 0.60673904),
    real2(0.64606744, 0.22471619),
    real2(0.65730339, 0.84269333),
    real2(0.66853935, 0.46067429),
    real2(0.67977530, 0.07865143),
    real2(0.69101125, 0.69662857),
    real2(0.70224720, 0.31460571),
    real2(0.71348315, 0.93258286),
    real2(0.72471911, 0.55056000),
    real2(0.73595506, 0.16853714),
    real2(0.74719101, 0.78651428),
    real2(0.75842696, 0.40449142),
    real2(0.76966292, 0.02246857),
    real2(0.78089887, 0.64044571),
    real2(0.79213482, 0.25842667),
    real2(0.80337077, 0.87640381),
    real2(0.81460673, 0.49438095),
    real2(0.82584268, 0.11235809),
    real2(0.83707863, 0.73033524),
    real2(0.84831458, 0.34831238),
    real2(0.85955054, 0.96628952),
    real2(0.87078649, 0.58426666),
    real2(0.88202250, 0.20224380),
    real2(0.89325845, 0.82022095),
    real2(0.90449440, 0.43820190),
    real2(0.91573036, 0.05617905),
    real2(0.92696631, 0.67415619),
    real2(0.93820226, 0.29213333),
    real2(0.94943821, 0.91011047),
    real2(0.96067417, 0.52808762),
    real2(0.97191012, 0.14606476),
    real2(0.98314607, 0.76404190),
    real2(0.99438202, 0.38201904)
};

// Loads elements from one of the precomputed tables for sample counts of 21, 34, 55, and 89.
// Computes sample positions at runtime otherwise.
// Sample count must be a Fibonacci number (see 'k_FibonacciSeq').
real2 Fibonacci2d(uint i, uint sampleCount)
{
    switch (sampleCount)
    {
        case 21: return k_Fibonacci2dSeq21[i];
        case 34: return k_Fibonacci2dSeq34[i];
        case 55: return k_Fibonacci2dSeq55[i];
        case 89: return k_Fibonacci2dSeq89[i];
        default:
        {
            uint fibN1 = sampleCount;
            uint fibN2 = sampleCount;

            // These are all constants, so this loop will be optimized away.
            for (uint j = 1; j < 20; j++)
            {
                if (k_FibonacciSeq[j] == fibN1)
                {
                    fibN2 = k_FibonacciSeq[j - 1];
                }
            }

            return Fibonacci2dSeq(fibN1, fibN2, i);
        }
    }
}

// Returns the radius as the X coordinate, and the angle as the Y coordinate.
real2 SampleDiskFibonacci(uint i, uint sampleCount)
{
    real2 f = Fibonacci2d(i, sampleCount);
    return real2(f.x, TWO_PI * f.y);
}

// Returns the zenith as the X coordinate, and the azimuthal angle as the Y coordinate.
real2 SampleHemisphereFibonacci(uint i, uint sampleCount)
{
    real2 f = Fibonacci2d(i, sampleCount);
    return real2(1 - f.x, TWO_PI * f.y);
}

// Returns the zenith as the X coordinate, and the azimuthal angle as the Y coordinate.
real2 SampleSphereFibonacci(uint i, uint sampleCount)
{
    real2 f = Fibonacci2d(i, sampleCount);
    return real2(1 - 2 * f.x, TWO_PI * f.y);
}

#endif // UNITY_FIBONACCI_INCLUDED
