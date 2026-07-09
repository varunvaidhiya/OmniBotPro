#pragma once

float CalculateUVwithProgress( float raw_index , float size )
{
    float index = floor(raw_index);

    float u = index % size;
    float v = size -1 - floor(index / size);

    return floor(v)*size + u;
}
