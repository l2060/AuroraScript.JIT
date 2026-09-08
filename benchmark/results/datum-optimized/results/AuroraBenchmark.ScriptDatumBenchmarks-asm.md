## .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3 (Job: Job-BZNPUE(IterationCount=5, IterationTime=200ms, LaunchCount=1, WarmupCount=3))

```assembly
; AuroraBenchmark.ScriptDatumBenchmarks.CreateInt32()
       sub       rsp,28
       xor       eax,eax
       jmp       short M00_L02
M00_L00:
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2sd xmm0,xmm0,r8d
       vmovq     r8,xmm0
M00_L01:
       cmp       eax,[rdx+8]
       jae       short M00_L04
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
       jae       short M00_L04
       mov       r10d,eax
       mov       r8d,[r8+r10*4+10]
       test      r8d,r8d
       jne       short M00_L00
       mov       r8,7FF8000000000001
       jmp       short M00_L01
M00_L03:
       add       rsp,28
       ret
M00_L04:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 105
```

## .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3 (Job: Job-BZNPUE(IterationCount=5, IterationTime=200ms, LaunchCount=1, WarmupCount=3))

```assembly
; AuroraBenchmark.ScriptDatumBenchmarks.CreateUInt32()
       sub       rsp,28
       xor       eax,eax
       jmp       short M00_L02
M00_L00:
       mov       r8d,r8d
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2sd xmm0,xmm0,r8
       vmovq     r8,xmm0
M00_L01:
       cmp       eax,[rdx+8]
       jae       short M00_L04
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
       jae       short M00_L04
       mov       r10d,eax
       mov       r8d,[r8+r10*4+10]
       test      r8d,r8d
       jne       short M00_L00
       mov       r8,7FF8000000000001
       jmp       short M00_L01
M00_L03:
       add       rsp,28
       ret
M00_L04:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 108
```

## .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3 (Job: Job-BZNPUE(IterationCount=5, IterationTime=200ms, LaunchCount=1, WarmupCount=3))

```assembly
; AuroraBenchmark.ScriptDatumBenchmarks.CreateNumberInt64()
       sub       rsp,28
       xor       eax,eax
       jmp       short M00_L02
M00_L00:
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2sd xmm0,xmm0,r8
       vmovq     r8,xmm0
M00_L01:
       cmp       eax,[rdx+8]
       jae       short M00_L04
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
       jae       short M00_L04
       mov       r10d,eax
       movsxd    r8,dword ptr [r8+r10*4+10]
       test      r8,r8
       jne       short M00_L00
       mov       r8,7FF8000000000001
       jmp       short M00_L01
M00_L03:
       add       rsp,28
       ret
M00_L04:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 105
```

## .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3 (Job: Job-BZNPUE(IterationCount=5, IterationTime=200ms, LaunchCount=1, WarmupCount=3))

```assembly
; AuroraBenchmark.ScriptDatumBenchmarks.CreateDouble()
       sub       rsp,28
       xor       eax,eax
       jmp       short M00_L01
M00_L00:
       cmp       eax,[rdx+8]
       jae       short M00_L04
       shl       r10,4
       lea       rdx,[rdx+r10+10]
       xor       r10d,r10d
       mov       [rdx],r10
       mov       [rdx+8],r8
       inc       eax
       cmp       eax,400
       jge       short M00_L03
M00_L01:
       mov       rdx,[rcx+20]
       mov       r8,[rcx+10]
       cmp       eax,[r8+8]
       jae       short M00_L04
       mov       r10d,eax
       vmovsd    xmm0,qword ptr [r8+r10*8+10]
       vucomisd  xmm0,xmm0
       jp        short M00_L02
       jne       short M00_L02
       vmovq     r8,xmm0
       cmp       r8,2
       ja        short M00_L00
       mov       r9,7FF8000000000001
       add       r8,r9
       jmp       short M00_L00
M00_L02:
       mov       r8,7FF8000000000004
       jmp       short M00_L00
M00_L03:
       add       rsp,28
       ret
M00_L04:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 122
```

## .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3 (Job: Job-BZNPUE(IterationCount=5, IterationTime=200ms, LaunchCount=1, WarmupCount=3))

```assembly
; AuroraBenchmark.ScriptDatumBenchmarks.ReadNumber()
       sub       rsp,28
       xor       eax,eax
       mov       rcx,[rcx+18]
       xor       edx,edx
       jmp       short M00_L02
       xchg      ax,ax
M00_L00:
       vmovq     xmm0,r8
M00_L01:
       vmovq     r8,xmm0
       xor       rax,r8
       inc       edx
       cmp       edx,400
       jge       short M00_L04
M00_L02:
       mov       r8,rcx
       cmp       edx,[r8+8]
       jae       short M00_L05
       mov       r10,rdx
       shl       r10,4
       mov       r8,[r8+r10+18]
       mov       r10,8007FFFFFFFFFFFF
       add       r10,r8
       cmp       r10,3
       ja        short M00_L00
       cmp       r10,2
       ja        short M00_L03
       vmovq     xmm0,r10
       jmp       short M00_L01
M00_L03:
       vmovsd    xmm0,qword ptr [7FF9778AA598]
       jmp       short M00_L01
M00_L04:
       add       rsp,28
       ret
M00_L05:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 113
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
       jge       near ptr M00_L05
M00_L03:
       mov       rdx,[rbx+18]
       cmp       edi,[rdx+8]
       jae       near ptr M00_L17
       mov       rcx,rdi
       shl       rcx,4
       vmovdqu   xmm0,xmmword ptr [rdx+rcx+10]
       vmovdqu   xmmword ptr [rsp+20],xmm0
       cmp       qword ptr [rsp+20],0
       jne       short M00_L06
       mov       rdx,[rsp+28]
       test      rdx,rdx
       je        near ptr M00_L09
       dec       rdx
       cmp       rdx,1
       jbe       near ptr M00_L10
       mov       ebp,2
M00_L04:
       cmp       ebp,2
       jne       near ptr M00_L11
       mov       rcx,7FF8000000000001
       cmp       [rsp+28],rcx
       je        short M00_L02
       mov       rcx,8000000000000000
       cmp       [rsp+28],rcx
       je        near ptr M00_L02
       mov       rcx,7FF8000000000004
       cmp       [rsp+28],rcx
       je        near ptr M00_L02
       jmp       near ptr M00_L01
M00_L05:
       mov       eax,esi
       add       rsp,30
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       ret
M00_L06:
       mov       rdx,[rsp+20]
       mov       rcx,8001400A00
       cmp       rdx,[rcx]
       jne       short M00_L07
       mov       ebp,1000
       jmp       short M00_L04
M00_L07:
       mov       rdx,[rsp+20]
       mov       rcx,8001400A08
       cmp       rdx,[rcx]
       jne       short M00_L08
       mov       ebp,2000
       jmp       near ptr M00_L04
M00_L08:
       movsx     rbp,word ptr [rsp+28]
       jmp       near ptr M00_L04
M00_L09:
       xor       ebp,ebp
       jmp       near ptr M00_L04
M00_L10:
       mov       ebp,1
       jmp       near ptr M00_L04
M00_L11:
       cmp       ebp,4
       ja        short M00_L12
       mov       edx,ebp
       lea       rcx,[7FF97789B150]
       mov       ecx,[rcx+rdx*4]
       lea       rax,[M00_L00]
       add       rcx,rax
       jmp       rcx
M00_L12:
       cmp       ebp,1000
       je        short M00_L13
       cmp       ebp,2000
       je        short M00_L13
       lea       rcx,[rsp+20]
       call      qword ptr [7FF977C34048]
       mov       r14,rax
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rax,8001401350
       cmp       r14,[rax]
       setne     r14b
       movzx     r14d,r14b
       jmp       short M00_L16
       cmp       qword ptr [rsp+28],2
       sete      r14b
       movzx     r14d,r14b
       jmp       short M00_L16
M00_L13:
       cmp       qword ptr [rsp+28],0
       setne     r14b
       movzx     r14d,r14b
       jmp       short M00_L16
       mov       rdx,[rsp+20]
       mov       rcx,offset MT_System.String
       call      qword ptr [7FF977826850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rax,rax
       je        short M00_L14
       cmp       dword ptr [rax+8],0
       sete      cl
       movzx     ecx,cl
       jmp       short M00_L15
M00_L14:
       mov       ecx,1
M00_L15:
       test      ecx,ecx
       sete      r14b
       movzx     r14d,r14b
M00_L16:
       test      r14d,r14d
       je        near ptr M00_L02
       jmp       near ptr M00_L01
M00_L17:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 493
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
       jmp       qword ptr [7FF977825C38]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBaseSlow(System.Runtime.CompilerServices.MethodTable*)
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
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,30
       xor       eax,eax
       mov       [rsp+28],rax
       mov       rbx,rcx
       xor       esi,esi
       xor       edi,edi
       jmp       short M00_L04
M00_L00:
       vmovq     xmm0,rdx
M00_L01:
       vmovsd    qword ptr [rsp+28],xmm0
M00_L02:
       mov       rax,[rsp+28]
       xor       rsi,rax
M00_L03:
       inc       edi
       cmp       edi,400
       jge       short M00_L06
M00_L04:
       mov       rdx,[rbx+18]
       cmp       edi,[rdx+8]
       jae       short M00_L08
       mov       rcx,rdi
       shl       rcx,4
       lea       rcx,[rdx+rcx+10]
       cmp       qword ptr [rcx],0
       jne       short M00_L07
       mov       rdx,[rcx+8]
       cmp       rdx,2
       jbe       short M00_L07
       mov       rcx,8007FFFFFFFFFFFF
       add       rcx,rdx
       cmp       rcx,3
       ja        short M00_L00
       cmp       rcx,2
       ja        short M00_L05
       vmovq     xmm0,rcx
       jmp       short M00_L01
M00_L05:
       vmovsd    xmm0,qword ptr [7FF9778AAAC8]
       jmp       short M00_L01
M00_L06:
       mov       rax,rsi
       add       rsp,30
       pop       rbx
       pop       rsi
       pop       rdi
       ret
M00_L07:
       lea       rdx,[rsp+28]
       call      qword ptr [7FF977BCFFA8]
       test      eax,eax
       je        short M00_L03
       jmp       short M00_L02
M00_L08:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 165
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
       call      qword ptr [7FF977BCFF78]; AuroraScript.Runtime.ScriptDatum.Equals(AuroraScript.Runtime.ScriptDatum)
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
       push      rbx
       sub       rsp,50
       vxorps    xmm4,xmm4,xmm4
       vmovdqu   ymmword ptr [rsp+20],ymm4
       vmovdqa   xmmword ptr [rsp+40],xmm4
M01_L00:
       vmovdqu   xmm0,xmmword ptr [rdx]
       vmovdqu   xmmword ptr [rsp+40],xmm0
       vmovdqu   xmm0,xmmword ptr [rcx]
       vmovdqu   xmmword ptr [rsp+30],xmm0
       cmp       qword ptr [rsp+40],0
       je        near ptr M01_L07
       mov       rcx,[rsp+40]
       mov       rdx,0A6B6800A00
       cmp       rcx,[rdx]
       je        near ptr M01_L05
       mov       rcx,[rsp+40]
       mov       rdx,0A6B6800A08
       movsx     rax,word ptr [rsp+48]
       mov       r8d,2000
       cmp       rcx,[rdx]
       cmove     eax,r8d
M01_L01:
       cmp       qword ptr [rsp+30],0
       je        near ptr M01_L10
       mov       rcx,[rsp+30]
       mov       rdx,0A6B6800A00
       cmp       rcx,[rdx]
       je        near ptr M01_L06
       mov       rcx,[rsp+30]
       mov       rdx,0A6B6800A08
       movsx     r8,word ptr [rsp+38]
       mov       r10d,2000
       cmp       rcx,[rdx]
       cmove     r8d,r10d
M01_L02:
       cmp       eax,r8d
       je        near ptr M01_L13
       cmp       eax,1000
       je        near ptr M01_L25
M01_L03:
       cmp       eax,2000
       jne       near ptr M01_L26
       cmp       r8d,1000
       jne       near ptr M01_L26
       cmp       qword ptr [rsp+38],0
       jl        near ptr M01_L38
       mov       rcx,[rsp+48]
       cmp       rcx,[rsp+38]
       sete      bl
       movzx     ebx,bl
M01_L04:
       movzx     eax,bl
       add       rsp,50
       pop       rbx
       ret
M01_L05:
       mov       eax,1000
       jmp       near ptr M01_L01
M01_L06:
       mov       r8d,1000
       jmp       short M01_L02
M01_L07:
       mov       rax,[rsp+48]
       test      rax,rax
       je        short M01_L09
       dec       rax
       mov       ecx,1
       mov       edx,2
       cmp       rax,1
       cmova     ecx,edx
       mov       eax,ecx
M01_L08:
       jmp       near ptr M01_L01
M01_L09:
       xor       eax,eax
       jmp       short M01_L08
M01_L10:
       mov       r8,[rsp+38]
       test      r8,r8
       je        short M01_L12
       dec       r8
       mov       ecx,1
       mov       edx,2
       cmp       r8,1
       cmova     ecx,edx
       mov       r8d,ecx
M01_L11:
       jmp       near ptr M01_L02
M01_L12:
       xor       r8d,r8d
       jmp       short M01_L11
M01_L13:
       cmp       r8d,4
       ja        short M01_L14
       mov       edx,r8d
       lea       rcx,[7FF9778AAF90]
       mov       ecx,[rcx+rdx*4]
       lea       rax,[M01_L00]
       add       rcx,rax
       jmp       rcx
M01_L14:
       cmp       r8d,1000
       je        near ptr M01_L22
       cmp       r8d,2000
       jne       near ptr M01_L23
       mov       rax,[rsp+48]
       cmp       rax,[rsp+38]
       sete      bl
       movzx     ebx,bl
       jmp       near ptr M01_L24
       mov       ebx,1
       jmp       near ptr M01_L24
       cmp       qword ptr [rsp+48],2
       sete      al
       movzx     eax,al
       cmp       qword ptr [rsp+38],2
       sete      cl
       movzx     ecx,cl
       cmp       eax,ecx
       sete      bl
       movzx     ebx,bl
       jmp       near ptr M01_L24
       mov       rax,[rsp+48]
       mov       rcx,8007FFFFFFFFFFFF
       add       rcx,rax
       cmp       rcx,3
       jbe       short M01_L15
       vmovq     xmm0,rax
       jmp       short M01_L17
M01_L15:
       cmp       rcx,2
       jbe       short M01_L16
       vmovsd    xmm0,qword ptr [7FF9778AAFA8]
       jmp       short M01_L17
M01_L16:
       vmovq     xmm0,rcx
M01_L17:
       mov       rax,[rsp+38]
       mov       rcx,8007FFFFFFFFFFFF
       add       rcx,rax
       cmp       rcx,3
       jbe       short M01_L18
       vmovq     xmm1,rax
       jmp       short M01_L20
M01_L18:
       cmp       rcx,2
       jbe       short M01_L19
       vmovsd    xmm1,qword ptr [7FF9778AAFA8]
       jmp       short M01_L20
M01_L19:
       vmovq     xmm1,rcx
M01_L20:
       vucomisd  xmm0,xmm1
       setnp     bl
       jp        short M01_L21
       sete      bl
M01_L21:
       movzx     ebx,bl
       jmp       short M01_L24
M01_L22:
       mov       rax,[rsp+48]
       cmp       rax,[rsp+38]
       sete      bl
       movzx     ebx,bl
       jmp       short M01_L24
       mov       rdx,[rsp+40]
       mov       rcx,offset MT_System.String
       call      qword ptr [7FF977836850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbx,rax
       mov       rdx,[rsp+30]
       mov       rcx,offset MT_System.String
       call      qword ptr [7FF977836850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdx,rax
       mov       rcx,rbx
       call      qword ptr [7FF977836E08]; System.String.Equals(System.String, System.String)
       mov       ebx,eax
       jmp       short M01_L24
M01_L23:
       mov       rax,[rsp+40]
       cmp       rax,[rsp+30]
       sete      bl
       movzx     ebx,bl
M01_L24:
       jmp       near ptr M01_L04
M01_L25:
       cmp       r8d,2000
       jne       near ptr M01_L03
       cmp       qword ptr [rsp+48],0
       jl        near ptr M01_L38
       mov       rax,[rsp+48]
       cmp       rax,[rsp+38]
       sete      bl
       movzx     ebx,bl
       jmp       near ptr M01_L04
M01_L26:
       cmp       qword ptr [rsp+40],0
       jne       short M01_L30
       cmp       qword ptr [rsp+48],2
       jbe       short M01_L30
       mov       rcx,[rsp+48]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M01_L27
       vmovq     xmm0,rcx
       jmp       short M01_L29
M01_L27:
       cmp       rdx,2
       jbe       short M01_L28
       vmovsd    xmm0,qword ptr [7FF9778AAFA8]
       jmp       short M01_L29
M01_L28:
       vmovq     xmm0,rdx
M01_L29:
       vmovsd    qword ptr [rsp+28],xmm0
       jmp       short M01_L31
M01_L30:
       lea       rcx,[rsp+40]
       lea       rdx,[rsp+28]
       call      qword ptr [7FF977C45DB8]
       test      eax,eax
       je        near ptr M01_L38
M01_L31:
       cmp       qword ptr [rsp+30],0
       jne       short M01_L35
       cmp       qword ptr [rsp+38],2
       jbe       short M01_L35
       mov       rcx,[rsp+38]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M01_L32
       vmovq     xmm0,rcx
       jmp       short M01_L34
M01_L32:
       cmp       rdx,2
       jbe       short M01_L33
       vmovsd    xmm0,qword ptr [7FF9778AAFA8]
       jmp       short M01_L34
M01_L33:
       vmovq     xmm0,rdx
M01_L34:
       vmovsd    qword ptr [rsp+20],xmm0
       jmp       short M01_L36
M01_L35:
       lea       rcx,[rsp+30]
       lea       rdx,[rsp+20]
       call      qword ptr [7FF977C45DB8]
       test      eax,eax
       je        short M01_L38
M01_L36:
       vmovsd    xmm0,qword ptr [rsp+28]
       vucomisd  xmm0,qword ptr [rsp+20]
       setnp     bl
       jp        short M01_L37
       sete      bl
M01_L37:
       movzx     ebx,bl
       jmp       near ptr M01_L04
M01_L38:
       xor       eax,eax
       add       rsp,50
       pop       rbx
       ret
; Total bytes of code 988
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

