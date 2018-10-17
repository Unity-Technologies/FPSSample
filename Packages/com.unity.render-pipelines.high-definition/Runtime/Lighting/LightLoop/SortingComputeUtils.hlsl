#ifndef __SORTINGCOMPUTEUTILS_H__
#define __SORTINGCOMPUTEUTILS_H__

unsigned int LimitPow2AndClamp(unsigned int value_in, unsigned int maxValue)
{
#if 0
    unsigned int value = 1;

    while(value<value_in && (value<<1)<=maxValue)
        value<<=1;

    return value_in==0 ? 0 : value;
#else
    uint valpw2 = value_in==0 ? 0 : (1<<firstbithigh(value_in));        // firstbithigh(0) returns -1
    valpw2 = max(valpw2, valpw2<<(valpw2!=value_in ? 1 : 0));   // max() just in case of overflow
    return min(valpw2, maxValue);
#endif
}

// have to make this sort routine a macro unfortunately because hlsl doesn't take
// groupshared memory of unspecified length as an input parameter to a function.
// maxcapacity_in must be a power of two.
// all data from length_in and up to closest power of two will be filled with UINT_MAX
#define SORTLIST(data, length_in, maxcapacity_in, localThreadID_in, nrthreads_in)   \
{   \
    int length=(int) length_in, maxcapacity=(int) maxcapacity_in, localThreadID=(int) localThreadID_in, nrthreads=(int) nrthreads_in;   \
                                                                                            \
    const int N = (const int) LimitPow2AndClamp((unsigned int) length, (uint) maxcapacity); \
    for(int t=length+localThreadID; t<N; t+=nrthreads) { data[t]=UINT_MAX; }              \
    GroupMemoryBarrierWithGroupSync();                                                      \
                                                                                            \
    for(int k=2; k<=N; k=2*k)                                                               \
    {                                                                                       \
        for(int j=k>>1; j>0; j=j>>1)                                                        \
        {                                                                                   \
            for(int i=localThreadID; i<N; i+=nrthreads)                                     \
            {                                                                               \
                int ixj=i^j;                                                                \
                if((ixj)>i)                                                                 \
                {                                                                           \
                    const unsigned int Avalue = data[i];                                    \
                    const unsigned int Bvalue = data[ixj];                                  \
                                                                                            \
                    const bool mustSwap = ((i&k)!=0^(Avalue>Bvalue)) && Avalue!=Bvalue;     \
                    if(mustSwap)                        \
                    {                                   \
                        data[i]=Bvalue;                 \
                        data[ixj]=Avalue;               \
                    }                   \
                }                       \
            }                           \
                                        \
            GroupMemoryBarrierWithGroupSync();      \
        }       \
    }       \
}

// have to make this sort routine a macro unfortunately because hlsl doesn't take
// groupshared memory of unspecified length as an input parameter to a function.
// merge-sort is not in-place so two buffers are required: data and tmpdata.
// These must both have a capacity of at least length_in entries and initial
// input is assumed to be in data and results will be delivered in data.
#define MERGESORTLIST(data, tmpdata, length_in, localThreadID_in, nrthreads_in) \
{           \
    int length=(int) length_in, localThreadID=(int) localThreadID_in, nrthreads=(int) nrthreads_in; \
                                                                                                        \
    for(int curr_size=1; curr_size<=length-1; curr_size = 2*curr_size)                                                  \
    {                                                                                                               \
        for(int left_start=localThreadID*(2*curr_size); left_start<(length-1); left_start+=nrthreads*(2*curr_size))     \
        {                                                                                                           \
            int mid = left_start + curr_size - 1;                                                                   \
            int right_end = min(left_start + 2*curr_size - 1, length-1);                                            \
            {                                                                                                       \
                int l=left_start, m=mid, r=right_end;                                                               \
                                                                                                                    \
                int i, j, k;                                                                                        \
                                                                                                                    \
                int ol = l;                                                                                         \
                int or = m+1;                                                                                       \
                int sl = m - l + 1;                                                                                 \
                int sr =  r - m;                                                                                    \
                                                                                                                    \
                for(int i=l; i<=r; i++) tmpdata[i] = data[i];                                                       \
                                                                                                                    \
                i = 0; j = 0; k = l;                                                                                \
                while (i < sl && j < sr)                                                                            \
                {                                                                                                   \
                    const uint lVal = tmpdata[ol+i];                                                                \
                    const uint rVal = tmpdata[or+j];                                                                \
                    bool pickLeft = lVal <= rVal;                                                                   \
                    i = pickLeft ? (i+1) : i;                                                                       \
                    j = pickLeft ? j : (j+1);                                                                       \
                    data[k] = pickLeft ? lVal : rVal;                                                               \
                    k++;                                                                                            \
                }                                                                                                   \
                                                                                                                    \
                while (i < sl)                                                                                      \
                {                                                                                                   \
                    data[k] = tmpdata[ol+i];                                                                        \
                    i++; k++;                                                                                       \
                }                                                                                                   \
                                                                                                                    \
                while (j < sr)                                                                                      \
                {                                                                                                   \
                    data[k] = tmpdata[or+j];                                                                        \
                    j++; k++;                                                                                       \
                }                                                                                                   \
            }                                                                                                       \
        }                                                                                                           \
                                                                                                                    \
       GroupMemoryBarrierWithGroupSync();                                                                           \
   }                                                                                                                \
}



#endif
