## .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3 (Job: Job-BZNPUE(IterationCount=5, IterationTime=200ms, LaunchCount=1, WarmupCount=3))

```assembly
; AuroraBenchmark.ScriptDatumBenchmarks.CreateInt32()
       sub       rsp,28
M00_L00:
       xor       eax,eax
       jmp       short M00_L02
M00_L01:
       cmp       eax,[rdx+8]
       jae       near ptr M00_L06
       shl       r10,4
       lea       rdx,[rdx+r10+10]
       xor       r10d,r10d
       mov       [rdx],r10
       mov       [rdx+8],r8
       inc       eax
       cmp       eax,400
       jge       short M00_L03
M00_L02:
       mov       rdx,[rcx+20]
       mov       r8,[rcx+8]
       cmp       eax,[r8+8]
       jae       near ptr M00_L06
       mov       r10d,eax
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2sd xmm0,xmm0,dword ptr [r8+r10*4+10]
       vucomisd  xmm0,xmm0
       jp        short M00_L04
       jne       short M00_L04
       vmovq     r8,xmm0
       cmp       r8,2
       ja        short M00_L01
       test      r8d,r8d
       jne       short M00_L05
       mov       r8,7FF8000000000001
       jmp       short M00_L01
M00_L03:
       add       rsp,28
       ret
M00_L04:
       mov       r8,7FF8000000000004
       jmp       short M00_L01
M00_L05:
       cmp       r8d,2
       ja        short M00_L01
       mov       r8d,r8d
       lea       r9,[7FF9778BA9A8]
       mov       r9d,[r9+r8*4]
       lea       r11,[M00_L00]
       add       r9,r11
       jmp       r9
       mov       r8,7FF8000000000002
       jmp       near ptr M00_L01
       mov       r8,7FF8000000000003
       jmp       near ptr M00_L01
M00_L06:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 199
```

## .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3 (Job: Job-BZNPUE(IterationCount=5, IterationTime=200ms, LaunchCount=1, WarmupCount=3))

```assembly
; AuroraBenchmark.ScriptDatumBenchmarks.CreateUInt32()
       sub       rsp,28
M00_L00:
       xor       eax,eax
       jmp       short M00_L02
M00_L01:
       cmp       eax,[rdx+8]
       jae       near ptr M00_L06
       shl       r10,4
       lea       rdx,[rdx+r10+10]
       xor       r10d,r10d
       mov       [rdx],r10
       mov       [rdx+8],r8
       inc       eax
       cmp       eax,400
       jge       short M00_L03
M00_L02:
       mov       rdx,[rcx+20]
       mov       r8,[rcx+8]
       cmp       eax,[r8+8]
       jae       near ptr M00_L06
       mov       r10d,eax
       mov       r8d,[r8+r10*4+10]
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2sd xmm0,xmm0,r8
       vucomisd  xmm0,xmm0
       jp        short M00_L04
       jne       short M00_L04
       vmovq     r8,xmm0
       cmp       r8,2
       ja        short M00_L01
       test      r8d,r8d
       jne       short M00_L05
       mov       r8,7FF8000000000001
       jmp       short M00_L01
M00_L03:
       add       rsp,28
       ret
M00_L04:
       mov       r8,7FF8000000000004
       jmp       short M00_L01
M00_L05:
       cmp       r8d,2
       ja        near ptr M00_L01
       mov       r8d,r8d
       lea       r9,[7FF97789A9F0]
       mov       r9d,[r9+r8*4]
       lea       r11,[M00_L00]
       add       r9,r11
       jmp       r9
       mov       r8,7FF8000000000002
       jmp       near ptr M00_L01
       mov       r8,7FF8000000000003
       jmp       near ptr M00_L01
M00_L06:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 206
```

## .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3 (Job: Job-BZNPUE(IterationCount=5, IterationTime=200ms, LaunchCount=1, WarmupCount=3))

```assembly
; AuroraBenchmark.ScriptDatumBenchmarks.CreateNumberInt64()
       sub       rsp,28
M00_L00:
       xor       eax,eax
       jmp       short M00_L02
M00_L01:
       cmp       eax,[rdx+8]
       jae       near ptr M00_L06
       shl       r10,4
       lea       rdx,[rdx+r10+10]
       xor       r10d,r10d
       mov       [rdx],r10
       mov       [rdx+8],r8
       inc       eax
       cmp       eax,400
       jge       short M00_L03
M00_L02:
       mov       rdx,[rcx+20]
       mov       r8,[rcx+8]
       cmp       eax,[r8+8]
       jae       near ptr M00_L06
       mov       r10d,eax
       movsxd    r8,dword ptr [r8+r10*4+10]
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2sd xmm0,xmm0,r8
       vucomisd  xmm0,xmm0
       jp        short M00_L04
       jne       short M00_L04
       vmovq     r8,xmm0
       cmp       r8,2
       ja        short M00_L01
       test      r8d,r8d
       jne       short M00_L05
       mov       r8,7FF8000000000001
       jmp       short M00_L01
M00_L03:
       add       rsp,28
       ret
M00_L04:
       mov       r8,7FF8000000000004
       jmp       short M00_L01
M00_L05:
       cmp       r8d,2
       ja        near ptr M00_L01
       mov       r8d,r8d
       lea       r9,[7FF9778AA9F0]
       mov       r9d,[r9+r8*4]
       lea       r11,[M00_L00]
       add       r9,r11
       jmp       r9
       mov       r8,7FF8000000000002
       jmp       near ptr M00_L01
       mov       r8,7FF8000000000003
       jmp       near ptr M00_L01
M00_L06:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 206
```

## .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3 (Job: Job-BZNPUE(IterationCount=5, IterationTime=200ms, LaunchCount=1, WarmupCount=3))

```assembly
; AuroraBenchmark.ScriptDatumBenchmarks.CreateDouble()
       sub       rsp,28
M00_L00:
       xor       eax,eax
       jmp       short M00_L02
M00_L01:
       cmp       eax,[rdx+8]
       jae       near ptr M00_L06
       shl       r10,4
       lea       rdx,[rdx+r10+10]
       xor       r10d,r10d
       mov       [rdx],r10
       mov       [rdx+8],r8
       inc       eax
       cmp       eax,400
       jge       near ptr M00_L05
M00_L02:
       mov       rdx,[rcx+20]
       mov       r8,[rcx+10]
       cmp       eax,[r8+8]
       jae       near ptr M00_L06
       mov       r10d,eax
       vmovsd    xmm0,qword ptr [r8+r10*8+10]
       vucomisd  xmm0,xmm0
       jp        short M00_L04
       jne       short M00_L04
       vmovq     r8,xmm0
       cmp       r8,2
       ja        short M00_L01
       test      r8d,r8d
       jne       short M00_L03
       mov       r8,7FF8000000000001
       jmp       short M00_L01
M00_L03:
       cmp       r8d,2
       ja        short M00_L01
       mov       r8d,r8d
       lea       r9,[7FF9778AA810]
       mov       r9d,[r9+r8*4]
       lea       r11,[M00_L00]
       add       r9,r11
       jmp       r9
       mov       r8,7FF8000000000002
       jmp       near ptr M00_L01
M00_L04:
       mov       r8,7FF8000000000004
       jmp       near ptr M00_L01
M00_L05:
       add       rsp,28
       ret
       mov       r8,7FF8000000000003
       jmp       near ptr M00_L01
M00_L06:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 202
```

## .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3 (Job: Job-BZNPUE(IterationCount=5, IterationTime=200ms, LaunchCount=1, WarmupCount=3))

```assembly
; AuroraBenchmark.ScriptDatumBenchmarks.ReadNumber()
       sub       rsp,28
M00_L00:
       xor       eax,eax
       mov       rcx,[rcx+18]
       xor       edx,edx
       jmp       short M00_L02
       xchg      ax,ax
M00_L01:
       vmovq     xmm0,r8
       vmovq     r8,xmm0
       xor       rax,r8
       inc       edx
       cmp       edx,400
       jge       short M00_L03
M00_L02:
       mov       r8,rcx
       cmp       edx,[r8+8]
       jae       short M00_L04
       mov       r10,rdx
       shl       r10,4
       mov       r8,[r8+r10+18]
       mov       r10,8007FFFFFFFFFFFF
       add       r10,r8
       cmp       r10,3
       ja        short M00_L01
       cmp       r10d,3
       ja        short M00_L01
       mov       r8d,r10d
       lea       r10,[7FF97789A820]
       mov       r10d,[r10+r8*4]
       lea       r9,[M00_L00]
       add       r10,r9
       jmp       r10
       xor       r8d,r8d
       jmp       short M00_L01
M00_L03:
       add       rsp,28
       ret
       mov       r8,0FFF8000000000000
       jmp       short M00_L01
       mov       r8d,1
       jmp       short M00_L01
       mov       r8d,2
       jmp       near ptr M00_L01
M00_L04:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 159
```

## .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3 (Job: Job-BZNPUE(IterationCount=5, IterationTime=200ms, LaunchCount=1, WarmupCount=3))

```assembly
; AuroraBenchmark.ScriptDatumBenchmarks.Truthiness()
       push      r14
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,30
       xor       eax,eax
       mov       [rsp+20],rax
       mov       [rsp+28],rax
       mov       rbx,rcx
M00_L00:
       xor       esi,esi
       xor       edi,edi
       jmp       short M00_L03
M00_L01:
       inc       esi
M00_L02:
       inc       edi
       cmp       edi,400
       jge       near ptr M00_L08
M00_L03:
       mov       rdx,[rbx+18]
       cmp       edi,[rdx+8]
       jae       near ptr M00_L21
       mov       rcx,rdi
       shl       rcx,4
       vmovdqu   xmm0,xmmword ptr [rdx+rcx+10]
       vmovdqu   xmmword ptr [rsp+20],xmm0
       cmp       qword ptr [rsp+20],0
       jne       near ptr M00_L10
       mov       rdx,[rsp+28]
       test      rdx,rdx
       je        near ptr M00_L13
       dec       rdx
       cmp       rdx,1
       jbe       near ptr M00_L14
       mov       ebp,2
M00_L04:
       cmp       ebp,2
       jne       near ptr M00_L15
       mov       rcx,[rsp+28]
       mov       rax,8007FFFFFFFFFFFF
       add       rax,rcx
       cmp       rax,3
       jbe       short M00_L09
M00_L05:
       vmovq     xmm0,rcx
       vxorps    xmm1,xmm1,xmm1
       vucomisd  xmm0,xmm1
       jp        short M00_L06
       je        near ptr M00_L02
M00_L06:
       vucomisd  xmm0,xmm0
       setnp     r14b
       jp        short M00_L07
       sete      r14b
M00_L07:
       movzx     r14d,r14b
       test      r14d,r14d
       je        near ptr M00_L02
       jmp       near ptr M00_L01
M00_L08:
       mov       eax,esi
       add       rsp,30
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       ret
M00_L09:
       cmp       eax,3
       ja        short M00_L05
       mov       ecx,eax
       lea       rax,[7FF9778BB700]
       mov       eax,[rax+rcx*4]
       lea       rdx,[M00_L00]
       add       rax,rdx
       jmp       rax
       xor       ecx,ecx
       jmp       short M00_L05
       mov       rcx,0FFF8000000000000
       jmp       short M00_L05
       mov       ecx,1
       jmp       short M00_L05
M00_L10:
       mov       rdx,[rsp+20]
       mov       rcx,8001400A00
       cmp       rdx,[rcx]
       jne       short M00_L11
       mov       ebp,1000
       jmp       near ptr M00_L04
M00_L11:
       mov       rdx,[rsp+20]
       mov       rcx,8001400A08
       cmp       rdx,[rcx]
       jne       short M00_L12
       mov       ebp,2000
       jmp       near ptr M00_L04
M00_L12:
       movsx     rbp,word ptr [rsp+28]
       jmp       near ptr M00_L04
M00_L13:
       xor       ebp,ebp
       jmp       near ptr M00_L04
M00_L14:
       mov       ebp,1
       jmp       near ptr M00_L04
M00_L15:
       cmp       ebp,4
       ja        short M00_L16
       mov       edx,ebp
       lea       rcx,[7FF9778BB710]
       mov       ecx,[rcx+rdx*4]
       lea       rax,[M00_L00]
       add       rcx,rax
       jmp       rcx
M00_L16:
       cmp       ebp,1000
       je        short M00_L17
       cmp       ebp,2000
       je        short M00_L17
       lea       rcx,[rsp+20]
       call      qword ptr [7FF977C54060]
       mov       r14,rax
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       call      qword ptr [7FF977845728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rax,8001401350
       cmp       r14,[rax]
       setne     r14b
       movzx     r14d,r14b
       jmp       short M00_L20
       cmp       qword ptr [rsp+28],2
       sete      r14b
       movzx     r14d,r14b
       jmp       short M00_L20
       mov       ecx,2
       jmp       near ptr M00_L05
M00_L17:
       cmp       qword ptr [rsp+28],0
       setne     r14b
       movzx     r14d,r14b
       jmp       short M00_L20
       mov       rdx,[rsp+20]
       mov       rcx,offset MT_System.String
       call      qword ptr [7FF977846850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rax,rax
       je        short M00_L18
       cmp       dword ptr [rax+8],0
       sete      cl
       movzx     ecx,cl
       jmp       short M00_L19
M00_L18:
       mov       ecx,1
M00_L19:
       test      ecx,ecx
       sete      r14b
       movzx     r14d,r14b
M00_L20:
       test      r14d,r14d
       je        near ptr M00_L02
       jmp       near ptr M00_L01
M00_L21:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 575
```
```assembly
; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rax,[rcx+20]
       mov       rax,[rax-18]
       mov       rdx,rax
       test      dl,1
       jne       short M01_L00
       ret
M01_L00:
       jmp       qword ptr [7FF977845C38]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBaseSlow(System.Runtime.CompilerServices.MethodTable*)
; Total bytes of code 23
```
```assembly
; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rdx,rdx
       je        short M02_L02
       mov       rax,[rdx]
       cmp       rax,rcx
       je        short M02_L02
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M02_L02
M02_L00:
       test      rax,rax
       je        short M02_L01
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M02_L02
       test      rax,rax
       je        short M02_L01
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M02_L02
       test      rax,rax
       jne       short M02_L03
M02_L01:
       xor       edx,edx
M02_L02:
       mov       rax,rdx
       ret
M02_L03:
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M02_L02
       test      rax,rax
       je        short M02_L01
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M02_L02
       jmp       short M02_L00
; Total bytes of code 86
```
```assembly
; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBaseSlow(System.Runtime.CompilerServices.MethodTable*)
       push      rbx
       sub       rsp,30
       xor       eax,eax
       mov       [rsp+28],rax
       mov       rbx,rcx
       mov       rcx,rbx
       call      qword ptr [7FF96E854D70]; Precode of System.Runtime.CompilerServices.InitHelpers.InitClassSlow(System.Runtime.CompilerServices.MethodTable*)
       mov       rax,[rbx+20]
       mov       rax,[rax-18]
       mov       [rsp+28],rax
       mov       rax,[rsp+28]
       and       rax,0FFFFFFFFFFFFFFFE
       xor       ecx,ecx
       mov       [rsp+28],rcx
       add       rsp,30
       pop       rbx
       ret
; Total bytes of code 59
```

## .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3 (Job: Job-BZNPUE(IterationCount=5, IterationTime=200ms, LaunchCount=1, WarmupCount=3))

```assembly
; AuroraBenchmark.ScriptDatumBenchmarks.ConvertNumber()
       push      r14
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,40
       vxorps    xmm4,xmm4,xmm4
       vmovdqu   xmmword ptr [rsp+28],xmm4
       xor       eax,eax
       mov       [rsp+38],rax
       mov       rbx,rcx
M00_L00:
       xor       esi,esi
       xor       edi,edi
       jmp       short M00_L04
M00_L01:
       vmovq     xmm0,rax
       vmovsd    qword ptr [rsp+38],xmm0
M00_L02:
       mov       rax,[rsp+38]
       xor       rsi,rax
M00_L03:
       inc       edi
       cmp       edi,400
       jge       near ptr M00_L06
M00_L04:
       mov       rdx,[rbx+18]
       cmp       edi,[rdx+8]
       jae       near ptr M00_L21
       mov       rcx,rdi
       shl       rcx,4
       lea       rbp,[rdx+rcx+10]
       cmp       qword ptr [rbp],0
       jne       near ptr M00_L07
       mov       rdx,[rbp+8]
       test      rdx,rdx
       je        near ptr M00_L10
       dec       rdx
       cmp       rdx,1
       jbe       near ptr M00_L11
       mov       r14d,2
M00_L05:
       lea       edx,[r14-1]
       cmp       edx,1
       jne       near ptr M00_L12
       mov       rax,[rbp+8]
       mov       rcx,8007FFFFFFFFFFFF
       add       rcx,rax
       cmp       rcx,3
       ja        near ptr M00_L01
       cmp       ecx,3
       ja        near ptr M00_L01
       mov       eax,ecx
       lea       rcx,[7FF9778BB828]
       mov       ecx,[rcx+rax*4]
       lea       rdx,[M00_L00]
       add       rcx,rdx
       jmp       rcx
       xor       eax,eax
       jmp       near ptr M00_L01
M00_L06:
       mov       rax,rsi
       add       rsp,40
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       ret
       mov       rax,0FFF8000000000000
       jmp       near ptr M00_L01
       mov       eax,1
       jmp       near ptr M00_L01
M00_L07:
       mov       rdx,[rbp]
       mov       rcx,8001400A00
       cmp       rdx,[rcx]
       jne       short M00_L08
       mov       r14d,1000
       jmp       near ptr M00_L05
M00_L08:
       mov       rdx,[rbp]
       mov       rcx,8001400A08
       cmp       rdx,[rcx]
       jne       short M00_L09
       mov       r14d,2000
       jmp       near ptr M00_L05
M00_L09:
       movsx     r14,word ptr [rbp+8]
       jmp       near ptr M00_L05
M00_L10:
       xor       r14d,r14d
       jmp       near ptr M00_L05
M00_L11:
       mov       r14d,1
       jmp       near ptr M00_L05
M00_L12:
       cmp       edx,3
       ja        short M00_L13
       mov       edx,edx
       lea       rcx,[7FF9778BB838]
       mov       ecx,[rcx+rdx*4]
       lea       rax,[M00_L00]
       add       rcx,rax
       jmp       rcx
M00_L13:
       cmp       r14d,1000
       je        short M00_L15
       cmp       r14d,2000
       jne       near ptr M00_L20
       mov       rax,[rbp+8]
       vxorps    xmm0,xmm0,xmm0
       mov       rdx,rax
       shr       rdx,1
       mov       ecx,eax
       and       ecx,1
       or        rcx,rdx
       test      rax,rax
       cmovns    rcx,rax
       vcvtsi2sd xmm0,xmm0,rcx
       jns       short M00_L14
       vaddsd    xmm0,xmm0,xmm0
M00_L14:
       vmovsd    qword ptr [rsp+38],xmm0
       jmp       near ptr M00_L02
       mov       eax,2
       jmp       near ptr M00_L01
M00_L15:
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2sd xmm0,xmm0,qword ptr [rbp+8]
       vmovsd    qword ptr [rsp+38],xmm0
       jmp       near ptr M00_L02
       cmp       qword ptr [rbp+8],2
       je        short M00_L16
       vxorps    xmm0,xmm0,xmm0
       jmp       short M00_L17
M00_L16:
       vmovsd    xmm0,qword ptr [7FF9778BB848]
M00_L17:
       vmovsd    qword ptr [rsp+38],xmm0
       jmp       near ptr M00_L02
       mov       rdx,[rbp]
       mov       rcx,offset MT_System.String
       call      qword ptr [7FF977846850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rax,rax
       jne       short M00_L18
       xor       eax,eax
       mov       [rsp+38],rax
       jmp       short M00_L19
M00_L18:
       lea       rbp,[rax+0C]
       mov       r14d,[rax+8]
       mov       rcx,8001400180
       mov       rcx,[rcx]
       call      qword ptr [7FF977BD5DB8]; System.Globalization.NumberFormatInfo.<GetInstance>g__GetProviderNonNull|58_0(System.IFormatProvider)
       mov       r8,rax
       mov       [rsp+28],rbp
       mov       [rsp+30],r14d
       lea       rcx,[rsp+28]
       lea       r9,[rsp+38]
       mov       edx,24
       call      qword ptr [7FF977C541C8]
M00_L19:
       test      eax,eax
       je        near ptr M00_L03
       jmp       near ptr M00_L02
M00_L20:
       mov       rax,0FFF8000000000000
       mov       [rsp+38],rax
       jmp       near ptr M00_L03
M00_L21:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 644
```
```assembly
; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rdx,rdx
       je        short M01_L02
       mov       rax,[rdx]
       cmp       rax,rcx
       je        short M01_L02
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M01_L02
M01_L00:
       test      rax,rax
       je        short M01_L01
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M01_L02
       test      rax,rax
       je        short M01_L01
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M01_L02
       test      rax,rax
       jne       short M01_L03
M01_L01:
       xor       edx,edx
M01_L02:
       mov       rax,rdx
       ret
M01_L03:
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M01_L02
       test      rax,rax
       je        short M01_L01
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M01_L02
       jmp       short M01_L00
; Total bytes of code 86
```
```assembly
; System.Globalization.NumberFormatInfo.<GetInstance>g__GetProviderNonNull|58_0(System.IFormatProvider)
       push      rsi
       push      rbx
       sub       rsp,28
       mov       rbx,rcx
       mov       rcx,offset MT_System.Globalization.CultureInfo
       cmp       [rbx],rcx
       jne       short M02_L00
       mov       rcx,rbx
       mov       rdx,7FF977CB7C80
       call      CORINFO_HELP_CLASSPROFILE32
       mov       rsi,[rbx+18]
       test      rsi,rsi
       jne       near ptr M02_L05
       mov       rcx,7FF977CB7D8C
       call      CORINFO_HELP_COUNTPROFILE32
M02_L00:
       mov       rcx,offset MT_System.Globalization.NumberFormatInfo
       xor       edx,edx
       cmp       [rbx],rcx
       mov       rsi,rdx
       cmove     rsi,rbx
       test      rsi,rsi
       jne       short M02_L03
       mov       rcx,rbx
       mov       rdx,7FF977CB7D90
       call      CORINFO_HELP_CLASSPROFILE32
       mov       rcx,rbx
       mov       r11,7FF977790568
       mov       rdx,0A033EC1518
       call      qword ptr [r11]
       test      rax,rax
       je        short M02_L01
       mov       rcx,offset MT_System.Globalization.NumberFormatInfo
       cmp       [rax],rcx
       jne       short M02_L01
       mov       rsi,rax
       jmp       short M02_L02
M02_L01:
       xor       esi,esi
M02_L02:
       test      rsi,rsi
       je        short M02_L04
       mov       rcx,7FF977CB7EA0
       call      CORINFO_HELP_COUNTPROFILE32
M02_L03:
       mov       rcx,7FF977CB7E98
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rax,rsi
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
M02_L04:
       mov       rcx,7FF977CB7E9C
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rcx,7FF977CB7E98
       call      CORINFO_HELP_COUNTPROFILE32
       nop
       add       rsp,28
       pop       rbx
       pop       rsi
       jmp       qword ptr [7FF97784D2A8]; System.Globalization.NumberFormatInfo.get_CurrentInfo()
M02_L05:
       mov       rcx,7FF977CB7D88
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rax,rsi
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
; Total bytes of code 281
```
```assembly
; System.Globalization.NumberFormatInfo.get_CurrentInfo()
       push      rbx
       sub       rsp,20
       call      qword ptr [7FF96E850950]; Precode of System.Globalization.CultureInfo.get_CurrentCulture()
       mov       rbx,rax
       cmp       byte ptr [rbx+61],0
       jne       short M03_L00
       mov       rax,[rbx+18]
       test      rax,rax
       je        short M03_L00
       add       rsp,20
       pop       rbx
       ret
M03_L00:
       mov       rcx,[System.Reflection.CustomAttributeExtensions.GetCustomAttribute[[System.__Canon, System.Private.CoreLib]](System.Reflection.Assembly)]
       call      qword ptr [7FF96E839058]; Precode of System.RuntimeTypeHandle.GetRuntimeTypeFromHandle(IntPtr)
       mov       rdx,rax
       mov       rcx,rbx
       lea       r11,[System.Reflection.CustomAttributeExtensions.GetCustomAttribute[[System.__Canon, System.Private.CoreLib]](System.Reflection.Assembly)]
       call      qword ptr [r11]
       mov       rcx,rax
       call      qword ptr [7FF96E848458]
       nop
       add       rsp,20
       pop       rbx
       ret
; Total bytes of code 80
```
```assembly
; System.Reflection.CustomAttributeExtensions.GetCustomAttribute[[System.__Canon, System.Private.CoreLib]](System.Reflection.Assembly)
       push      rsi
       push      rbx
       sub       rsp,28
       mov       [rsp+20],rcx
       mov       rbx,rdx
       call      qword ptr [7FF96E83EC68]
       mov       rsi,rax
       mov       rcx,rax
       call      qword ptr [7FF96E839058]; Precode of System.RuntimeTypeHandle.GetRuntimeTypeFromHandle(IntPtr)
       mov       rdx,rax
       mov       rcx,rbx
       mov       r8d,1
       call      qword ptr [7FF96E84A5E8]
       mov       r8,rax
       test      r8,r8
       je        short M04_L00
       mov       rcx,rsi
       cmp       [r8],rcx
       je        short M04_L00
       mov       rdx,rax
       call      qword ptr [7FF96E839090]; Precode of System.Runtime.CompilerServices.CastHelpers.ChkCastAny(Void*, System.Object)
       mov       r8,rax
M04_L00:
       mov       rax,r8
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
; Total bytes of code 88
```

## .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3 (Job: Job-BZNPUE(IterationCount=5, IterationTime=200ms, LaunchCount=1, WarmupCount=3))

```assembly
; AuroraBenchmark.ScriptDatumBenchmarks.ExactIntegerEquality()
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,30
       xor       eax,eax
       mov       [rsp+20],rax
       mov       [rsp+28],rax
       mov       rbx,rcx
       xor       esi,esi
       xor       edi,edi
       jmp       short M00_L02
M00_L00:
       inc       esi
M00_L01:
       inc       edi
       cmp       edi,400
       jge       short M00_L03
M00_L02:
       mov       rcx,[rbx+28]
       cmp       edi,[rcx+8]
       jae       short M00_L04
       mov       rdx,rdi
       shl       rdx,4
       lea       rcx,[rcx+rdx+10]
       mov       rax,[rbx+30]
       cmp       edi,[rax+8]
       jae       short M00_L04
       vmovdqu   xmm0,xmmword ptr [rax+rdx+10]
       vmovdqu   xmmword ptr [rsp+20],xmm0
       lea       rdx,[rsp+20]
       call      qword ptr [7FF977BCFF90]; AuroraScript.Runtime.ScriptDatum.Equals(AuroraScript.Runtime.ScriptDatum)
       test      eax,eax
       je        short M00_L01
       jmp       short M00_L00
M00_L03:
       mov       eax,esi
       add       rsp,30
       pop       rbx
       pop       rsi
       pop       rdi
       ret
M00_L04:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 115
```
```assembly
; AuroraScript.Runtime.ScriptDatum.Equals(AuroraScript.Runtime.ScriptDatum)
       push      r14
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,30
       xor       eax,eax
       mov       [rsp+28],rax
       mov       [rsp+20],rax
M01_L00:
       mov       rbx,[rdx]
       mov       rsi,[rdx+8]
       mov       rdi,[rcx]
       mov       rbp,[rcx+8]
       test      rbx,rbx
       je        near ptr M01_L26
       mov       rdx,8482400A00
       cmp       rbx,[rdx]
       je        near ptr M01_L12
       mov       rdx,8482400A08
       movsx     rcx,si
       mov       eax,2000
       cmp       rbx,[rdx]
       cmove     ecx,eax
M01_L01:
       test      rdi,rdi
       je        near ptr M01_L29
       mov       rdx,8482400A00
       cmp       rdi,[rdx]
       je        near ptr M01_L13
       mov       rdx,8482400A08
       movsx     rax,bp
       mov       r8d,2000
       cmp       rdi,[rdx]
       cmove     eax,r8d
M01_L02:
       cmp       ecx,eax
       je        near ptr M01_L32
       test      rbx,rbx
       je        near ptr M01_L48
       mov       rdx,8482400A00
       cmp       rbx,[rdx]
       je        near ptr M01_L14
       mov       rdx,8482400A08
       movsx     rcx,si
       mov       eax,2000
       cmp       rbx,[rdx]
       cmove     ecx,eax
M01_L03:
       lea       edx,[rcx-1]
       cmp       edx,3
       jbe       near ptr M01_L47
       cmp       ecx,1000
       jne       near ptr M01_L15
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2sd xmm0,xmm0,rsi
       vmovsd    qword ptr [rsp+28],xmm0
M01_L04:
       test      rdi,rdi
       je        near ptr M01_L57
       mov       rdx,8482400A00
       cmp       rdi,[rdx]
       je        near ptr M01_L17
       mov       rdx,8482400A08
       movsx     rcx,bp
       mov       eax,2000
       cmp       rdi,[rdx]
       cmove     ecx,eax
M01_L05:
       lea       edx,[rcx-1]
       cmp       edx,3
       jbe       near ptr M01_L56
       cmp       ecx,1000
       jne       near ptr M01_L18
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2sd xmm0,xmm0,rbp
       vmovsd    qword ptr [rsp+20],xmm0
M01_L06:
       test      rbx,rbx
       je        near ptr M01_L66
       mov       rax,8482400A00
       cmp       rbx,[rax]
       je        near ptr M01_L20
       mov       rax,8482400A08
       cmp       rbx,[rax]
       jne       near ptr M01_L65
M01_L07:
       mov       rax,8482400A00
       cmp       rbx,[rax]
       je        short M01_L09
       mov       rax,8482400A08
       cmp       rbx,[rax]
       jne       near ptr M01_L75
M01_L08:
       test      rdi,rdi
       je        near ptr M01_L81
       mov       rax,8482400A00
       cmp       rdi,[rax]
       je        near ptr M01_L24
       mov       rax,8482400A08
       cmp       rdi,[rax]
       jne       near ptr M01_L80
M01_L09:
       vmovsd    xmm0,qword ptr [rsp+28]
       vucomisd  xmm0,qword ptr [rsp+20]
       setnp     r14b
       jp        short M01_L10
       sete      r14b
M01_L10:
       movzx     r14d,r14b
M01_L11:
       movzx     eax,r14b
       add       rsp,30
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       ret
M01_L12:
       mov       ecx,1000
       jmp       near ptr M01_L01
M01_L13:
       mov       eax,1000
       jmp       near ptr M01_L02
M01_L14:
       mov       ecx,1000
       jmp       near ptr M01_L03
M01_L15:
       cmp       ecx,2000
       jne       near ptr M01_L55
       vxorps    xmm0,xmm0,xmm0
       mov       rdx,rsi
       shr       rdx,1
       mov       ecx,esi
       and       ecx,1
       or        rcx,rdx
       test      rsi,rsi
       cmovns    rcx,rsi
       vcvtsi2sd xmm0,xmm0,rcx
       jns       short M01_L16
       vaddsd    xmm0,xmm0,xmm0
M01_L16:
       vmovsd    qword ptr [rsp+28],xmm0
       jmp       near ptr M01_L04
M01_L17:
       mov       ecx,1000
       jmp       near ptr M01_L05
M01_L18:
       cmp       ecx,2000
       jne       near ptr M01_L64
       vxorps    xmm0,xmm0,xmm0
       mov       rdx,rbp
       shr       rdx,1
       mov       ecx,ebp
       and       ecx,1
       or        rcx,rdx
       test      rbp,rbp
       cmovns    rcx,rbp
       vcvtsi2sd xmm0,xmm0,rcx
       jns       short M01_L19
       vaddsd    xmm0,xmm0,xmm0
M01_L19:
       vmovsd    qword ptr [rsp+20],xmm0
       jmp       near ptr M01_L06
M01_L20:
       test      rdi,rdi
       je        near ptr M01_L71
       mov       rax,8482400A00
       cmp       rdi,[rax]
       je        short M01_L23
       mov       rax,8482400A08
       cmp       rdi,[rax]
       jne       near ptr M01_L70
M01_L21:
       test      rsi,rsi
       jge       short M01_L25
M01_L22:
       xor       eax,eax
       add       rsp,30
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       ret
M01_L23:
       test      rbx,rbx
       je        near ptr M01_L76
       jmp       near ptr M01_L07
M01_L24:
       test      rbp,rbp
       jl        short M01_L22
M01_L25:
       cmp       rsi,rbp
       sete      r14b
       movzx     r14d,r14b
       jmp       near ptr M01_L11
M01_L26:
       test      rsi,rsi
       je        short M01_L28
       lea       rcx,[rsi-1]
       mov       edx,1
       mov       eax,2
       cmp       rcx,1
       cmova     edx,eax
       mov       ecx,edx
M01_L27:
       jmp       near ptr M01_L01
M01_L28:
       xor       ecx,ecx
       jmp       short M01_L27
M01_L29:
       test      rbp,rbp
       je        short M01_L31
       lea       rax,[rbp-1]
       mov       edx,1
       mov       r8d,2
       cmp       rax,1
       cmova     edx,r8d
       mov       eax,edx
M01_L30:
       jmp       near ptr M01_L02
M01_L31:
       xor       eax,eax
       jmp       short M01_L30
M01_L32:
       test      rbx,rbx
       je        short M01_L35
       mov       rdx,8482400A00
       cmp       rbx,[rdx]
       jne       short M01_L33
       mov       eax,1000
       jmp       short M01_L34
M01_L33:
       mov       rdx,8482400A08
       movsx     rax,si
       mov       ecx,2000
       cmp       rbx,[rdx]
       cmove     eax,ecx
M01_L34:
       cmp       eax,4
       ja        short M01_L38
       mov       edx,eax
       lea       rcx,[7FF9778ABAA8]
       mov       ecx,[rcx+rdx*4]
       lea       rax,[M01_L00]
       add       rcx,rax
       jmp       rcx
M01_L35:
       test      rsi,rsi
       je        short M01_L37
       lea       rax,[rsi-1]
       mov       edx,1
       mov       ecx,2
       cmp       rax,1
       cmova     edx,ecx
       mov       eax,edx
M01_L36:
       jmp       short M01_L34
M01_L37:
       xor       eax,eax
       jmp       short M01_L36
M01_L38:
       cmp       eax,1000
       je        short M01_L39
       cmp       eax,2000
       jne       near ptr M01_L45
M01_L39:
       cmp       rsi,rbp
       sete      r14b
       movzx     r14d,r14b
       jmp       near ptr M01_L46
       mov       r14d,1
       jmp       near ptr M01_L46
       cmp       rsi,2
       sete      al
       movzx     eax,al
       cmp       rbp,2
       sete      cl
       movzx     ecx,cl
       cmp       eax,ecx
       sete      r14b
       movzx     r14d,r14b
       jmp       near ptr M01_L46
       mov       rax,8007FFFFFFFFFFFF
       add       rax,rsi
       cmp       rax,3
       ja        short M01_L40
       cmp       eax,3
       ja        short M01_L40
       mov       eax,eax
       lea       rcx,[7FF9778ABABC]
       mov       ecx,[rcx+rax*4]
       lea       rdx,[M01_L00]
       add       rcx,rdx
       jmp       rcx
M01_L40:
       mov       rax,rsi
       jmp       short M01_L41
       xor       eax,eax
       jmp       short M01_L41
       mov       eax,1
       jmp       short M01_L41
       mov       eax,2
       jmp       short M01_L41
       mov       rax,0FFF8000000000000
M01_L41:
       vmovq     xmm0,rax
       mov       rax,8007FFFFFFFFFFFF
       add       rax,rbp
       cmp       rax,3
       ja        short M01_L42
       cmp       eax,3
       ja        short M01_L42
       mov       eax,eax
       lea       rcx,[7FF9778ABACC]
       mov       ecx,[rcx+rax*4]
       lea       rdx,[M01_L00]
       add       rcx,rdx
       jmp       rcx
M01_L42:
       mov       rax,rbp
       jmp       short M01_L43
       xor       eax,eax
       jmp       short M01_L43
       mov       eax,1
       jmp       short M01_L43
       mov       eax,2
       jmp       short M01_L43
       mov       rax,0FFF8000000000000
M01_L43:
       vmovq     xmm1,rax
       vucomisd  xmm0,xmm1
       setnp     r14b
       jp        short M01_L44
       sete      r14b
M01_L44:
       movzx     r14d,r14b
       jmp       short M01_L46
       mov       rdx,rbx
       mov       rcx,offset MT_System.String
       call      qword ptr [7FF977836850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rsi,rax
       mov       rdx,rdi
       mov       rcx,offset MT_System.String
       call      qword ptr [7FF977836850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdx,rax
       mov       rcx,rsi
       call      qword ptr [7FF977836E08]; System.String.Equals(System.String, System.String)
       mov       r14d,eax
       jmp       short M01_L46
M01_L45:
       cmp       rbx,rdi
       sete      r14b
       movzx     r14d,r14b
M01_L46:
       jmp       near ptr M01_L11
M01_L47:
       mov       edx,edx
       lea       rcx,[7FF9778ABADC]
       mov       ecx,[rcx+rdx*4]
       lea       rax,[M01_L00]
       add       rcx,rax
       jmp       rcx
M01_L48:
       test      rsi,rsi
       je        short M01_L50
       lea       rcx,[rsi-1]
       mov       edx,1
       mov       eax,2
       cmp       rcx,1
       cmova     edx,eax
       mov       ecx,edx
M01_L49:
       jmp       near ptr M01_L03
M01_L50:
       xor       ecx,ecx
       jmp       short M01_L49
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rsi
       cmp       rdx,3
       ja        short M01_L51
       cmp       edx,3
       ja        short M01_L51
       mov       edx,edx
       lea       rcx,[7FF9778ABAEC]
       mov       ecx,[rcx+rdx*4]
       lea       rax,[M01_L00]
       add       rcx,rax
       jmp       rcx
M01_L51:
       mov       rdx,rsi
       jmp       short M01_L52
       xor       edx,edx
       jmp       short M01_L52
       mov       edx,1
       jmp       short M01_L52
       mov       edx,2
       jmp       short M01_L52
       mov       rdx,0FFF8000000000000
M01_L52:
       vmovq     xmm0,rdx
       vmovsd    qword ptr [rsp+28],xmm0
       jmp       near ptr M01_L04
       cmp       rsi,2
       je        short M01_L53
       vxorps    xmm0,xmm0,xmm0
       jmp       short M01_L54
M01_L53:
       vmovsd    xmm0,qword ptr [7FF9778ABB00]
M01_L54:
       vmovsd    qword ptr [rsp+28],xmm0
       jmp       near ptr M01_L04
       mov       rdx,rbx
       mov       rcx,offset MT_System.String
       call      qword ptr [7FF977836850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rcx,rax
       mov       r8,8482400180
       mov       r8,[r8]
       lea       r9,[rsp+28]
       mov       edx,24
       call      qword ptr [7FF977C44060]
       test      eax,eax
       je        near ptr M01_L22
       jmp       near ptr M01_L04
M01_L55:
       mov       rdx,0FFF8000000000000
       mov       [rsp+28],rdx
       jmp       near ptr M01_L22
M01_L56:
       mov       edx,edx
       lea       rcx,[7FF9778ABB08]
       mov       ecx,[rcx+rdx*4]
       lea       rax,[M01_L00]
       add       rcx,rax
       jmp       rcx
M01_L57:
       test      rbp,rbp
       je        short M01_L59
       lea       rcx,[rbp-1]
       mov       edx,1
       mov       eax,2
       cmp       rcx,1
       cmova     edx,eax
       mov       ecx,edx
M01_L58:
       jmp       near ptr M01_L05
M01_L59:
       xor       ecx,ecx
       jmp       short M01_L58
       mov       rax,8007FFFFFFFFFFFF
       add       rax,rbp
       cmp       rax,3
       ja        short M01_L60
       cmp       eax,3
       ja        short M01_L60
       mov       eax,eax
       lea       rdx,[7FF9778ABB18]
       mov       edx,[rdx+rax*4]
       lea       rcx,[M01_L00]
       add       rdx,rcx
       jmp       rdx
M01_L60:
       mov       rax,rbp
       jmp       short M01_L61
       xor       eax,eax
       jmp       short M01_L61
       mov       eax,1
       jmp       short M01_L61
       mov       eax,2
       jmp       short M01_L61
       mov       rax,0FFF8000000000000
M01_L61:
       vmovq     xmm0,rax
       vmovsd    qword ptr [rsp+20],xmm0
       jmp       near ptr M01_L06
       cmp       rbp,2
       je        short M01_L62
       vxorps    xmm0,xmm0,xmm0
       jmp       short M01_L63
M01_L62:
       vmovsd    xmm0,qword ptr [7FF9778ABB00]
M01_L63:
       vmovsd    qword ptr [rsp+20],xmm0
       jmp       near ptr M01_L06
       mov       rdx,rdi
       mov       rcx,offset MT_System.String
       call      qword ptr [7FF977836850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rcx,rax
       mov       r8,8482400180
       mov       r8,[r8]
       lea       r9,[rsp+20]
       mov       edx,24
       call      qword ptr [7FF977C44060]
       test      eax,eax
       je        near ptr M01_L22
       jmp       near ptr M01_L06
M01_L64:
       mov       rax,0FFF8000000000000
       mov       [rsp+20],rax
       jmp       near ptr M01_L22
M01_L65:
       movsx     rax,si
       jmp       short M01_L69
M01_L66:
       test      rsi,rsi
       je        short M01_L68
       lea       rax,[rsi-1]
       mov       edx,1
       mov       ecx,2
       cmp       rax,1
       cmova     edx,ecx
       mov       eax,edx
M01_L67:
       jmp       short M01_L69
M01_L68:
       xor       eax,eax
       jmp       short M01_L67
M01_L69:
       cmp       eax,1000
       jne       near ptr M01_L23
       jmp       near ptr M01_L20
M01_L70:
       movsx     rax,bp
       jmp       short M01_L74
M01_L71:
       test      rbp,rbp
       je        short M01_L73
       lea       rax,[rbp-1]
       mov       edx,1
       mov       ecx,2
       cmp       rax,1
       cmova     edx,ecx
       mov       eax,edx
M01_L72:
       jmp       short M01_L74
M01_L73:
       xor       eax,eax
       jmp       short M01_L72
M01_L74:
       cmp       eax,2000
       jne       near ptr M01_L23
       jmp       near ptr M01_L21
M01_L75:
       movsx     rax,si
       jmp       short M01_L79
M01_L76:
       test      rsi,rsi
       je        short M01_L78
       lea       rax,[rsi-1]
       mov       edx,1
       mov       ecx,2
       cmp       rax,1
       cmova     edx,ecx
       mov       eax,edx
M01_L77:
       jmp       short M01_L79
M01_L78:
       xor       eax,eax
       jmp       short M01_L77
M01_L79:
       cmp       eax,2000
       jne       near ptr M01_L09
       jmp       near ptr M01_L08
M01_L80:
       movsx     rax,bp
       jmp       short M01_L84
M01_L81:
       test      rbp,rbp
       je        short M01_L83
       lea       rax,[rbp-1]
       mov       edx,1
       mov       ecx,2
       cmp       rax,1
       cmova     edx,ecx
       mov       eax,edx
M01_L82:
       jmp       short M01_L84
M01_L83:
       xor       eax,eax
       jmp       short M01_L82
M01_L84:
       cmp       eax,1000
       jne       near ptr M01_L09
       jmp       near ptr M01_L24
; Total bytes of code 2066
```
```assembly
; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rdx,rdx
       je        short M02_L02
       mov       rax,[rdx]
       cmp       rax,rcx
       je        short M02_L02
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M02_L02
M02_L00:
       test      rax,rax
       je        short M02_L01
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M02_L02
       test      rax,rax
       je        short M02_L01
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M02_L02
       test      rax,rax
       jne       short M02_L03
M02_L01:
       xor       edx,edx
M02_L02:
       mov       rax,rdx
       ret
M02_L03:
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M02_L02
       test      rax,rax
       je        short M02_L01
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M02_L02
       jmp       short M02_L00
; Total bytes of code 86
```
```assembly
; System.String.Equals(System.String, System.String)
       sub       rsp,28
       cmp       rcx,rdx
       je        short M03_L02
       test      rcx,rcx
       je        short M03_L00
       test      rdx,rdx
       je        short M03_L00
       mov       r8d,[rcx+8]
       cmp       r8d,[rdx+8]
       je        short M03_L01
M03_L00:
       xor       eax,eax
       add       rsp,28
       ret
M03_L01:
       lea       rax,[rcx+0C]
       add       rdx,0C
       mov       r8d,[rcx+8]
       add       r8d,r8d
       mov       rcx,rax
       call      qword ptr [7FF96E84D900]; Precode of System.SpanHelpers.SequenceEqual(Byte ByRef, Byte ByRef, UIntPtr)
       nop
       add       rsp,28
       ret
M03_L02:
       mov       eax,1
       add       rsp,28
       ret
; Total bytes of code 76
```

