Shader "Test" {
  Properties {
    _MyColour ("Normal", Color) = (0.3, 0.4, 0.6, 1)
    _MyColour2 ("Check range", Color) = (    0.3, 0.4, 0.6, 1    )
    _MyColour3 ("Empty values", Color) = (0,,,1)
    _MyColour4 ("Invalid values", Color) = (0, foo, bar, oink)
    _MyColour5 ("Missing values", Color) = (0)
    _MyColour6 ("Too many values", Color) = (1, 1, 1, 1, 1)
    _MyColour7 ("Out of range values", Color) = (0, 0, 32767, 32767)
    _MyVector ("Out of range values. Also, vector", Vector) = (0, 0, 32767, 32767)
  }
}
