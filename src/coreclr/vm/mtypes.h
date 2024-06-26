// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: mtypes.h
//

//
// Defines the mapping between MARSHAL_TYPE constants and their Marshaler
// classes. Used to generate all the enums and tables.
//


// ------------------------------------------------------------------------------------------------------------------
//                    Marshaler ID                  Marshaler class name                 Supported in WinRT scenarios
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_GENERIC_1,       CopyMarshaler1)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_GENERIC_U1,      CopyMarshalerU1)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_GENERIC_2,       CopyMarshaler2)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_GENERIC_U2,      CopyMarshalerU2)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_GENERIC_4,       CopyMarshaler4)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_GENERIC_U4,      CopyMarshalerU4)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_GENERIC_8,       CopyMarshaler8)

DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_WINBOOL,         WinBoolMarshaler)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_CBOOL,           CBoolMarshaler)
#ifdef FEATURE_COMINTEROP
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_VTBOOL,          VtBoolMarshaler)
#endif // FEATURE_COMINTEROP

DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_ANSICHAR,        AnsiCharMarshaler)

DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_FLOAT,           FloatMarshaler)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_DOUBLE,          DoubleMarshaler)

DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_CURRENCY,        CurrencyMarshaler)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_DECIMAL,         DecimalMarshaler)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_DECIMAL_PTR,     DecimalPtrMarshaler)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_GUID,            GuidMarshaler)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_GUID_PTR,        GuidPtrMarshaler)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_DATE,            DateMarshaler)

DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_LPWSTR,          WSTRMarshaler)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_LPSTR,           CSTRMarshaler)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_LPUTF8STR,       CUTF8Marshaler)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_BSTR,            BSTRMarshaler)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_ANSIBSTR,        AnsiBSTRMarshaler)

DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_LPWSTR_BUFFER,   WSTRBufferMarshaler)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_LPSTR_BUFFER,    CSTRBufferMarshaler)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_UTF8_BUFFER,     UTF8BufferMarshaler)

#if defined(FEATURE_COMINTEROP)
// CoreCLR doesn't have any support for marshalling interface pointers.
// Not even support for fake CCWs.
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_INTERFACE,       InterfaceMarshaler)
#endif // defined(FEATURE_COMINTEROP)

#ifdef FEATURE_COMINTEROP
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_SAFEARRAY,       SafeArrayMarshaler)
#endif // FEATURE_COMINTEROP
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_NATIVEARRAY,     NativeArrayMarshaler)

DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_ASANYA,          AsAnyAMarshaler)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_ASANYW,          AsAnyWMarshaler)

DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_DELEGATE,        DelegateMarshaler)

DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_BLITTABLEPTR,    BlittablePtrMarshaler)

#ifdef FEATURE_COMINTEROP
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_VBBYVALSTR,      VBByValStrMarshaler)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_VBBYVALSTRW,     VBByValStrWMarshaler)
#endif // FEATURE_COMINTEROP

DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_LAYOUTCLASSPTR,  LayoutClassPtrMarshaler)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_ARRAYWITHOFFSET, ArrayWithOffsetMarshaler)

DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_BLITTABLEVALUECLASS,             BlittableValueClassMarshaler)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_VALUECLASS,                      ValueClassMarshaler)

DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_REFERENCECUSTOMMARSHALER,        ReferenceCustomMarshaler)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_ARGITERATOR,                     ArgIteratorMarshaler)

#if defined(TARGET_WINDOWS)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_BLITTABLEVALUECLASSWITHCOPYCTOR, BlittableValueClassWithCopyCtorMarshaler)
#endif // defined(TARGET_WINDOWS)

#ifdef FEATURE_COMINTEROP
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_OBJECT,                          ObjectMarshaler)
#endif // FEATURE_COMINTEROP

DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_HANDLEREF,                       HandleRefMarshaler)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_SAFEHANDLE,                      SafeHandleMarshaler)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_CRITICALHANDLE,                  CriticalHandleMarshaler)

#ifdef FEATURE_COMINTEROP
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_OLECOLOR,                        OleColorMarshaler)
#endif // FEATURE_COMINTEROP

DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_RUNTIMETYPEHANDLE,               RuntimeTypeHandleMarshaler)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_RUNTIMEMETHODHANDLE,             RuntimeMethodHandleMarshaler)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_RUNTIMEFIELDHANDLE,              RuntimeFieldHandleMarshaler)

DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_FIXED_ARRAY,                     FixedArrayMarshaler)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_FIXED_WSTR,                      FixedWSTRMarshaler)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_FIXED_CSTR,                      FixedCSTRMarshaler)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_BLITTABLE_LAYOUTCLASS,           BlittableLayoutClassMarshaler)
DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_LAYOUTCLASS,                     LayoutClassMarshaler)

DEFINE_MARSHALER_TYPE(MARSHAL_TYPE_POINTER,                         PointerMarshaler)

#undef DEFINE_MARSHALER_TYPE
