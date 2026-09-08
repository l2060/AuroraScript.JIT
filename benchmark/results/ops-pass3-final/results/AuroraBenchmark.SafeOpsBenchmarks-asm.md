## .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3 (Job: Job-BZNPUE(IterationCount=5, IterationTime=200ms, LaunchCount=1, WarmupCount=3))

```assembly
; AuroraBenchmark.SafeOpsBenchmarks.ArrayMiss()
       push      r14
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,80
       xor       eax,eax
       mov       [rsp+28],rax
       vxorps    xmm4,xmm4,xmm4
       vmovdqu   ymmword ptr [rsp+30],ymm4
       vmovdqu   ymmword ptr [rsp+50],ymm4
       vmovdqa   xmmword ptr [rsp+70],xmm4
M00_L00:
       mov       rdx,[rcx+10]
       mov       rbx,[rcx+18]
       mov       rsi,[rcx+70]
       mov       rdi,[rcx+78]
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rsp+50],xmm0
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rsp+60],xmm0
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rsp+70],xmm0
       test      rdx,rdx
       jne       near ptr M00_L08
       test      rbx,rbx
       je        near ptr M00_L17
       lea       rcx,[rbx-1]
       mov       eax,1
       mov       r8d,2
       cmp       rcx,1
       cmova     eax,r8d
M00_L01:
       cmp       eax,4
       je        near ptr M00_L18
M00_L02:
       mov       [rsp+40],rdx
       mov       [rsp+48],rbx
       cmp       qword ptr [rsp+40],0
       jne       near ptr M00_L09
       mov       rcx,[rsp+48]
       test      rcx,rcx
       je        near ptr M00_L31
       dec       rcx
       mov       eax,1
       mov       edx,2
       cmp       rcx,1
       cmova     eax,edx
M00_L03:
       cmp       eax,4
       jbe       near ptr M00_L30
       cmp       eax,1000
       je        near ptr M00_L52
       cmp       eax,2000
       je        near ptr M00_L53
       mov       rdx,[rsp+40]
       mov       rbx,rdx
       test      rbx,rbx
       je        short M00_L04
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptArray
       cmp       [rbx],rcx
       jne       near ptr M00_L32
M00_L04:
       test      rbx,rbx
       je        near ptr M00_L54
M00_L05:
       test      rbx,rbx
       jne       near ptr M00_L10
M00_L06:
       mov       rdx,rbx
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptPackedArray
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       je        near ptr M00_L14
       mov       rcx,rbp
       mov       rax,[rbp]
       mov       rax,[rax+58]
       call      qword ptr [rax+8]
       mov       ebx,eax
       xor       r14d,r14d
       test      ebx,ebx
       jle       near ptr M00_L12
       cmp       [rbp],ebp
       mov       rdx,[rbp]
M00_L07:
       lea       rdx,[rsp+60]
       mov       rcx,rbp
       mov       r8d,r14d
       mov       rax,[rbp]
       mov       rax,[rax+58]
       call      qword ptr [rax+10]
       mov       [rsp+28],rsi
       mov       [rsp+30],rdi
       lea       rdx,[rsp+28]
       lea       rcx,[rsp+60]
       call      qword ptr [7FF977C35C38]; AuroraScript.Runtime.ScriptDatum.Equals(AuroraScript.Runtime.ScriptDatum)
       test      eax,eax
       jne       near ptr M00_L16
       inc       r14d
       cmp       r14d,ebx
       jl        short M00_L07
       jmp       near ptr M00_L12
M00_L08:
       mov       rax,8001400A00
       cmp       rdx,[rax]
       je        near ptr M00_L02
       mov       rcx,8001400A08
       cmp       rdx,[rcx]
       je        near ptr M00_L02
       movsx     rax,bx
       jmp       near ptr M00_L01
M00_L09:
       mov       rax,[rsp+40]
       mov       rcx,8001400A00
       cmp       rax,[rcx]
       je        near ptr M00_L29
       mov       rcx,[rsp+40]
       mov       rax,8001400A08
       movsx     rdx,word ptr [rsp+48]
       mov       r8d,2000
       cmp       rcx,[rax]
       mov       eax,r8d
       cmovne    eax,edx
       jmp       near ptr M00_L03
M00_L10:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptArray
       cmp       [rbx],rcx
       jne       near ptr M00_L06
       mov       ebp,[rbx+34]
       xor       r14d,r14d
       test      ebp,ebp
       jle       short M00_L12
M00_L11:
       mov       rcx,[rbx+38]
       cmp       r14d,[rcx+8]
       jae       near ptr M00_L58
       mov       rdx,r14
       shl       rdx,4
       lea       rcx,[rcx+rdx+10]
       mov       [rsp+28],rsi
       mov       [rsp+30],rdi
       lea       rdx,[rsp+28]
       call      qword ptr [7FF977C35C38]; AuroraScript.Runtime.ScriptDatum.Equals(AuroraScript.Runtime.ScriptDatum)
       test      eax,eax
       jne       short M00_L16
       inc       r14d
       cmp       r14d,ebp
       jl        short M00_L11
M00_L12:
       xor       eax,eax
M00_L13:
       add       rsp,80
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       ret
M00_L14:
       mov       rcx,rbx
       mov       rax,[rbx]
       mov       rax,[rax+58]
       call      qword ptr [rax]
       mov       rbx,rax
       lea       rdx,[rsp+70]
       mov       rcx,rbx
       cmp       [rcx],ecx
       call      qword ptr [7FF977C35CB0]
       test      eax,eax
       je        short M00_L12
M00_L15:
       mov       [rsp+28],rsi
       mov       [rsp+30],rdi
       lea       rdx,[rsp+28]
       lea       rcx,[rsp+70]
       call      qword ptr [7FF977C35C38]; AuroraScript.Runtime.ScriptDatum.Equals(AuroraScript.Runtime.ScriptDatum)
       test      eax,eax
       jne       short M00_L16
       lea       rdx,[rsp+70]
       mov       rcx,rbx
       cmp       [rcx],ecx
       call      qword ptr [7FF977C35CB0]
       test      eax,eax
       je        short M00_L12
       jmp       short M00_L15
M00_L16:
       mov       eax,1
       jmp       short M00_L13
M00_L17:
       xor       eax,eax
       jmp       near ptr M00_L01
M00_L18:
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbx,rax
       mov       rdx,0A033FB0008
       test      rbx,rbx
       cmove     rbx,rdx
       mov       rbp,rbx
       test      rsi,rsi
       je        short M00_L19
       mov       rcx,8001400A00
       cmp       rsi,[rcx]
       je        near ptr M00_L23
       mov       rdx,8001400A08
       cmp       rsi,[rdx]
       je        near ptr M00_L23
       movsx     rcx,di
       jmp       short M00_L22
M00_L19:
       test      rdi,rdi
       je        short M00_L21
       lea       rdx,[rdi-1]
       mov       ecx,1
       mov       eax,2
       cmp       rdx,1
       cmova     ecx,eax
M00_L20:
       jmp       short M00_L22
M00_L21:
       xor       ecx,ecx
       jmp       short M00_L20
M00_L22:
       cmp       ecx,4
       jne       short M00_L23
       mov       rdx,rsi
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdx,rax
       mov       r9,0A033FB0008
       test      rdx,rdx
       cmove     rdx,r9
       cmp       dword ptr [rdx+8],0
       je        near ptr M00_L12
       mov       dword ptr [rsp+20],4
       mov       r9d,[rbx+8]
       mov       rcx,rbx
       xor       r8d,r8d
       call      qword ptr [7FF977C37CA8]
       not       eax
       shr       eax,1F
       jmp       near ptr M00_L13
M00_L23:
       xor       ebx,ebx
       jmp       near ptr M00_L28
M00_L24:
       movzx     r14d,word ptr [rbp+rbx*2+0C]
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      qword ptr [7FF977835728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001401498
       mov       rcx,[rcx]
       cmp       [rcx+8],r14d
       jg        short M00_L25
       mov       ecx,1
       call      qword ptr [7FF977836670]; System.String.FastAllocateString(IntPtr)
       mov       [rax+0C],r14w
       jmp       short M00_L26
M00_L25:
       mov       ecx,r14d
       call      qword ptr [7FF977C37CC0]
       mov       rax,[rax+38]
M00_L26:
       test      rax,rax
       jne       short M00_L27
       mov       rax,0A033FB0008
M00_L27:
       mov       [rsp+50],rax
       mov       qword ptr [rsp+58],4
       mov       [rsp+28],rsi
       mov       [rsp+30],rdi
       lea       rdx,[rsp+28]
       lea       rcx,[rsp+50]
       call      qword ptr [7FF977C35C38]; AuroraScript.Runtime.ScriptDatum.Equals(AuroraScript.Runtime.ScriptDatum)
       test      eax,eax
       jne       near ptr M00_L16
       inc       ebx
M00_L28:
       cmp       [rbp+8],ebx
       jg        near ptr M00_L24
       jmp       near ptr M00_L12
M00_L29:
       mov       eax,1000
       jmp       near ptr M00_L03
M00_L30:
       mov       ecx,eax
       lea       rax,[7FF9778B1BD8]
       mov       eax,[rax+rcx*4]
       lea       rdx,[M00_L00]
       add       rax,rdx
       jmp       rax
M00_L31:
       xor       eax,eax
       jmp       near ptr M00_L03
M00_L32:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbx,rax
       jmp       near ptr M00_L04
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       call      qword ptr [7FF977835728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001400A50
       mov       rbx,[rcx]
       jmp       near ptr M00_L05
       cmp       qword ptr [rsp+48],2
       je        short M00_L33
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BooleanValue
       call      qword ptr [7FF977835740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001401520
       mov       rbx,[rcx]
       jmp       short M00_L34
M00_L33:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BooleanValue
       call      qword ptr [7FF977835740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001401518
       mov       rbx,[rcx]
M00_L34:
       jmp       near ptr M00_L05
       mov       rcx,[rsp+48]
       mov       rax,8007FFFFFFFFFFFF
       add       rax,rcx
       cmp       rax,3
       jbe       short M00_L35
       vmovq     xmm1,rcx
       jmp       short M00_L37
M00_L35:
       cmp       rax,2
       jbe       short M00_L36
       vmovsd    xmm1,qword ptr [7FF9778B1BF0]
       jmp       short M00_L37
M00_L36:
       vmovq     xmm1,rax
M00_L37:
       vucomisd  xmm1,xmm1
       jp        short M00_L38
       je        short M00_L39
M00_L38:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977835740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001401508
       mov       rbx,[rcx]
       jmp       near ptr M00_L51
M00_L39:
       vxorps    xmm0,xmm0,xmm0
       vucomisd  xmm1,xmm0
       jp        short M00_L40
       jne       short M00_L40
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977835740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80014014B8
       mov       rbx,[rcx]
       jmp       near ptr M00_L51
M00_L40:
       vucomisd  xmm1,qword ptr [7FF9778B1BF8]
       jp        short M00_L41
       jne       short M00_L41
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977835740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80014014C0
       mov       rbx,[rcx]
       jmp       near ptr M00_L51
M00_L41:
       vucomisd  xmm1,qword ptr [7FF9778B1C00]
       jp        short M00_L42
       jne       short M00_L42
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977835740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80014014C8
       mov       rbx,[rcx]
       jmp       near ptr M00_L51
M00_L42:
       vucomisd  xmm1,qword ptr [7FF9778B1C08]
       jp        short M00_L43
       jne       short M00_L43
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977835740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80014014D0
       mov       rbx,[rcx]
       jmp       near ptr M00_L51
M00_L43:
       vucomisd  xmm1,qword ptr [7FF9778B1C10]
       jp        short M00_L44
       jne       short M00_L44
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977835740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80014014D8
       mov       rbx,[rcx]
       jmp       near ptr M00_L51
M00_L44:
       vucomisd  xmm1,qword ptr [7FF9778B1C18]
       jp        short M00_L45
       jne       short M00_L45
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977835740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80014014E0
       mov       rbx,[rcx]
       jmp       near ptr M00_L51
M00_L45:
       vucomisd  xmm1,qword ptr [7FF9778B1C20]
       jp        short M00_L46
       jne       short M00_L46
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977835740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80014014E8
       mov       rbx,[rcx]
       jmp       near ptr M00_L51
M00_L46:
       vucomisd  xmm1,qword ptr [7FF9778B1C28]
       jp        short M00_L47
       jne       short M00_L47
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977835740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80014014F0
       mov       rbx,[rcx]
       jmp       near ptr M00_L51
M00_L47:
       vucomisd  xmm1,qword ptr [7FF9778B1C30]
       jp        short M00_L48
       jne       short M00_L48
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977835740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80014014F8
       mov       rbx,[rcx]
       jmp       near ptr M00_L51
M00_L48:
       vucomisd  xmm1,qword ptr [7FF9778B1C38]
       jp        short M00_L49
       jne       short M00_L49
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977835740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001401500
       mov       rbx,[rcx]
       jmp       short M00_L51
M00_L49:
       vmovsd    qword ptr [rsp+38],xmm1
       vucomisd  xmm1,qword ptr [7FF9778B1C40]
       jp        short M00_L50
       jne       short M00_L50
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977835740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80014014B0
       mov       rbx,[rcx]
       jmp       short M00_L51
M00_L50:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       rcx,rbx
       vmovsd    xmm1,qword ptr [rsp+38]
       call      qword ptr [7FF977C37CD8]
M00_L51:
       jmp       near ptr M00_L05
M00_L52:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.Int64Value
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       rcx,rbx
       mov       rdx,[rsp+48]
       call      qword ptr [7FF977C35D40]
       jmp       near ptr M00_L05
M00_L53:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.UInt64Value
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       rcx,rbx
       mov       rdx,[rsp+48]
       call      qword ptr [7FF977C35D70]
       jmp       near ptr M00_L05
       lea       rcx,[rsp+40]
       call      qword ptr [7FF977C35D88]
       mov       rbx,rax
       jmp       near ptr M00_L05
M00_L54:
       mov       rdx,[rsp+40]
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       short M00_L55
       xor       ebx,ebx
       jmp       near ptr M00_L05
M00_L55:
       cmp       dword ptr [rbp+8],0
       jne       short M00_L56
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      qword ptr [7FF977835728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80014014A0
       mov       rbx,[rcx]
       jmp       short M00_L57
M00_L56:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       rcx,rbx
       mov       rdx,rbp
       call      qword ptr [7FF977C37CF0]
M00_L57:
       jmp       near ptr M00_L05
M00_L58:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 2104
```
```assembly
; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rdx,rdx
       je        short M01_L01
       mov       rax,[rdx]
       cmp       rax,rcx
       je        short M01_L01
       mov       rax,[rax+10]
M01_L00:
       cmp       rax,rcx
       jne       short M01_L02
M01_L01:
       mov       rax,rdx
       ret
M01_L02:
       test      rax,rax
       je        short M01_L03
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M01_L01
       test      rax,rax
       je        short M01_L03
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M01_L01
       test      rax,rax
       je        short M01_L03
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M01_L01
       test      rax,rax
       je        short M01_L03
       mov       rax,[rax+10]
       jmp       short M01_L00
M01_L03:
       xor       edx,edx
       jmp       short M01_L01
; Total bytes of code 83
```
```assembly
; AuroraScript.Runtime.ScriptDatum.Equals(AuroraScript.Runtime.ScriptDatum)
       push      rbx
       sub       rsp,50
       vxorps    xmm4,xmm4,xmm4
       vmovdqu   ymmword ptr [rsp+20],ymm4
       vmovdqa   xmmword ptr [rsp+40],xmm4
M02_L00:
       vmovdqu   xmm0,xmmword ptr [rdx]
       vmovdqu   xmmword ptr [rsp+40],xmm0
       vmovdqu   xmm0,xmmword ptr [rcx]
       vmovdqu   xmmword ptr [rsp+30],xmm0
       cmp       qword ptr [rsp+40],0
       jne       near ptr M02_L07
       mov       rdx,[rsp+48]
       test      rdx,rdx
       je        near ptr M02_L12
       dec       rdx
       mov       eax,1
       mov       ecx,2
       cmp       rdx,1
       cmova     eax,ecx
M02_L01:
       cmp       qword ptr [rsp+30],0
       jne       near ptr M02_L08
       mov       rdx,[rsp+38]
       test      rdx,rdx
       je        near ptr M02_L14
       dec       rdx
       mov       r8d,1
       mov       ecx,2
       cmp       rdx,1
       cmova     r8d,ecx
M02_L02:
       cmp       eax,r8d
       jne       near ptr M02_L21
       cmp       r8d,2
       jne       near ptr M02_L15
       mov       rax,[rsp+48]
       mov       rcx,8007FFFFFFFFFFFF
       add       rcx,rax
       cmp       rcx,3
       jbe       near ptr M02_L09
       vmovq     xmm0,rax
M02_L03:
       mov       rcx,[rsp+38]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       near ptr M02_L10
       vmovq     xmm1,rcx
M02_L04:
       vucomisd  xmm0,xmm1
       setnp     al
       jp        short M02_L05
       sete      al
M02_L05:
       movzx     eax,al
M02_L06:
       movzx     eax,al
       add       rsp,50
       pop       rbx
       ret
M02_L07:
       mov       rax,[rsp+40]
       mov       rdx,8001400A00
       cmp       rax,[rdx]
       je        near ptr M02_L11
       mov       rdx,[rsp+40]
       mov       rcx,8001400A08
       movsx     rax,word ptr [rsp+48]
       mov       r8d,2000
       cmp       rdx,[rcx]
       cmove     eax,r8d
       jmp       near ptr M02_L01
M02_L08:
       mov       r8,[rsp+30]
       mov       rdx,8001400A00
       cmp       r8,[rdx]
       je        short M02_L13
       mov       rdx,[rsp+30]
       mov       rcx,8001400A08
       movsx     r8,word ptr [rsp+38]
       mov       r10d,2000
       cmp       rdx,[rcx]
       cmove     r8d,r10d
       jmp       near ptr M02_L02
M02_L09:
       cmp       rcx,2
       ja        near ptr M02_L17
       vmovq     xmm0,rcx
       jmp       near ptr M02_L03
M02_L10:
       cmp       rdx,2
       ja        near ptr M02_L18
       vmovq     xmm1,rdx
       jmp       near ptr M02_L04
M02_L11:
       mov       eax,1000
       jmp       near ptr M02_L01
M02_L12:
       xor       eax,eax
       jmp       near ptr M02_L01
M02_L13:
       mov       r8d,1000
       jmp       near ptr M02_L02
M02_L14:
       xor       r8d,r8d
       jmp       near ptr M02_L02
M02_L15:
       cmp       r8d,4
       ja        short M02_L16
       mov       edx,r8d
       lea       rcx,[7FF9778B1340]
       mov       ecx,[rcx+rdx*4]
       lea       rax,[M02_L00]
       add       rcx,rax
       jmp       rcx
M02_L16:
       cmp       r8d,1000
       je        short M02_L19
       cmp       r8d,2000
       jne       near ptr M02_L20
       mov       rax,[rsp+48]
       cmp       rax,[rsp+38]
       sete      al
       movzx     eax,al
       jmp       near ptr M02_L06
       mov       eax,1
       jmp       near ptr M02_L06
       cmp       qword ptr [rsp+48],2
       sete      al
       movzx     eax,al
       cmp       qword ptr [rsp+38],2
       sete      cl
       movzx     ecx,cl
       cmp       eax,ecx
       sete      al
       movzx     eax,al
       jmp       near ptr M02_L06
M02_L17:
       vmovsd    xmm0,qword ptr [7FF9778B1358]
       jmp       near ptr M02_L03
M02_L18:
       vmovsd    xmm1,qword ptr [7FF9778B1358]
       jmp       near ptr M02_L04
M02_L19:
       mov       rax,[rsp+48]
       cmp       rax,[rsp+38]
       sete      al
       movzx     eax,al
       jmp       near ptr M02_L06
       mov       rdx,[rsp+40]
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbx,rax
       mov       rdx,[rsp+30]
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdx,rax
       mov       rcx,rbx
       call      qword ptr [7FF977836E08]; System.String.Equals(System.String, System.String)
       jmp       near ptr M02_L06
M02_L20:
       mov       rax,[rsp+40]
       cmp       rax,[rsp+30]
       sete      al
       movzx     eax,al
       jmp       near ptr M02_L06
M02_L21:
       cmp       eax,1000
       jne       short M02_L22
       cmp       r8d,2000
       jne       short M02_L22
       cmp       qword ptr [rsp+48],0
       jl        near ptr M02_L35
       mov       rax,[rsp+48]
       cmp       rax,[rsp+38]
       sete      al
       movzx     eax,al
       jmp       near ptr M02_L06
M02_L22:
       cmp       eax,2000
       jne       short M02_L23
       cmp       r8d,1000
       jne       short M02_L23
       cmp       qword ptr [rsp+38],0
       jl        near ptr M02_L35
       mov       rcx,[rsp+48]
       cmp       rcx,[rsp+38]
       sete      al
       movzx     eax,al
       jmp       near ptr M02_L06
M02_L23:
       cmp       qword ptr [rsp+40],0
       jne       short M02_L27
       cmp       qword ptr [rsp+48],2
       jbe       short M02_L27
       mov       rcx,[rsp+48]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M02_L24
       vmovq     xmm0,rcx
       jmp       short M02_L26
M02_L24:
       cmp       rdx,2
       jbe       short M02_L25
       vmovsd    xmm0,qword ptr [7FF9778B1358]
       jmp       short M02_L26
M02_L25:
       vmovq     xmm0,rdx
M02_L26:
       vmovsd    qword ptr [rsp+28],xmm0
       jmp       short M02_L28
M02_L27:
       lea       rcx,[rsp+40]
       lea       rdx,[rsp+28]
       call      qword ptr [7FF977C37C60]
       test      eax,eax
       je        near ptr M02_L35
M02_L28:
       cmp       qword ptr [rsp+30],0
       jne       short M02_L32
       cmp       qword ptr [rsp+38],2
       jbe       short M02_L32
       mov       rcx,[rsp+38]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M02_L29
       vmovq     xmm0,rcx
       jmp       short M02_L31
M02_L29:
       cmp       rdx,2
       jbe       short M02_L30
       vmovsd    xmm0,qword ptr [7FF9778B1358]
       jmp       short M02_L31
M02_L30:
       vmovq     xmm0,rdx
M02_L31:
       vmovsd    qword ptr [rsp+20],xmm0
       jmp       short M02_L33
M02_L32:
       lea       rcx,[rsp+30]
       lea       rdx,[rsp+20]
       call      qword ptr [7FF977C37C60]
       test      eax,eax
       je        short M02_L35
M02_L33:
       vmovsd    xmm0,qword ptr [rsp+28]
       vucomisd  xmm0,qword ptr [rsp+20]
       setnp     al
       jp        short M02_L34
       sete      al
M02_L34:
       movzx     eax,al
       jmp       near ptr M02_L06
M02_L35:
       xor       eax,eax
       add       rsp,50
       pop       rbx
       ret
; Total bytes of code 1021
```
```assembly
; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rax,[rcx+20]
       mov       rax,[rax-18]
       mov       rdx,rax
       test      dl,1
       jne       short M03_L00
       ret
M03_L00:
       jmp       qword ptr [7FF977835C38]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBaseSlow(System.Runtime.CompilerServices.MethodTable*)
; Total bytes of code 23
```
```assembly
; System.String.FastAllocateString(IntPtr)
       mov       rdx,rcx
       mov       rcx,offset MT_System.String
       jmp       near ptr 00007FF9D74C4E80
; Total bytes of code 18
```
```assembly
; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       push      rsi
       push      rbx
       sub       rsp,28
       mov       rbx,rcx
       mov       rcx,[rbx+20]
       mov       rsi,[rcx-10]
       mov       rcx,rsi
       test      cl,1
       jne       short M05_L00
       mov       rcx,7FF977CA629C
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rax,rsi
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
M05_L00:
       mov       rcx,7FF977CA6298
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rcx,rbx
       add       rsp,28
       pop       rbx
       pop       rsi
       jmp       qword ptr [7FF977A0E838]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBaseSlow(System.Runtime.CompilerServices.MethodTable*)
; Total bytes of code 80
```
```assembly
; System.String.Equals(System.String, System.String)
       sub       rsp,28
       cmp       rcx,rdx
       je        short M06_L02
       test      rcx,rcx
       je        short M06_L00
       test      rdx,rdx
       je        short M06_L00
       mov       r8d,[rcx+8]
       cmp       r8d,[rdx+8]
       je        short M06_L01
M06_L00:
       xor       eax,eax
       add       rsp,28
       ret
M06_L01:
       lea       rax,[rcx+0C]
       add       rdx,0C
       mov       r8d,[rcx+8]
       add       r8d,r8d
       mov       rcx,rax
       call      qword ptr [7FF96E84D900]; Precode of System.SpanHelpers.SequenceEqual(Byte ByRef, Byte ByRef, UIntPtr)
       nop
       add       rsp,28
       ret
M06_L02:
       mov       eax,1
       add       rsp,28
       ret
; Total bytes of code 76
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
```assembly
; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBaseSlow(System.Runtime.CompilerServices.MethodTable*)
       push      rbx
       sub       rsp,30
       xor       eax,eax
       mov       [rsp+28],rax
       mov       rbx,rcx
       mov       rcx,rbx
       call      qword ptr [7FF96E854D70]; Precode of System.Runtime.CompilerServices.InitHelpers.InitClassSlow(System.Runtime.CompilerServices.MethodTable*)
       mov       rax,[rbx+20]
       mov       rax,[rax-10]
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
; AuroraBenchmark.SafeOpsBenchmarks.PackedMiss()
       push      r15
       push      r14
       push      r13
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,80
       xor       eax,eax
       mov       [rsp+28],rax
       vxorps    xmm4,xmm4,xmm4
       vmovdqu   ymmword ptr [rsp+30],ymm4
       vmovdqu   ymmword ptr [rsp+50],ymm4
       vmovdqa   xmmword ptr [rsp+70],xmm4
M00_L00:
       mov       rdx,[rcx+20]
       mov       rbx,[rcx+28]
       mov       rsi,[rcx+70]
       mov       rdi,[rcx+78]
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rsp+50],xmm0
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rsp+60],xmm0
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rsp+70],xmm0
       test      rdx,rdx
       jne       near ptr M00_L13
       test      rbx,rbx
       je        near ptr M00_L22
       lea       rcx,[rbx-1]
       mov       eax,1
       mov       r8d,2
       cmp       rcx,1
       cmova     eax,r8d
M00_L01:
       cmp       eax,4
       je        near ptr M00_L23
M00_L02:
       mov       [rsp+40],rdx
       mov       [rsp+48],rbx
       cmp       qword ptr [rsp+40],0
       jne       near ptr M00_L14
       mov       rcx,[rsp+48]
       test      rcx,rcx
       je        near ptr M00_L36
       dec       rcx
       mov       eax,1
       mov       edx,2
       cmp       rcx,1
       cmova     eax,edx
M00_L03:
       cmp       eax,4
       jbe       near ptr M00_L35
       cmp       eax,1000
       je        near ptr M00_L57
       cmp       eax,2000
       je        near ptr M00_L58
       mov       rdx,[rsp+40]
       mov       rbx,rdx
       test      rbx,rbx
       je        short M00_L04
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt32Array
       cmp       [rbx],rcx
       jne       near ptr M00_L37
M00_L04:
       test      rbx,rbx
       je        near ptr M00_L59
M00_L05:
       test      rbx,rbx
       jne       near ptr M00_L15
M00_L06:
       mov       rbp,rbx
       test      rbp,rbp
       je        short M00_L07
       mov       rdx,offset MT_AuroraScript.Runtime.Types.ScriptInt32Array
       cmp       [rbp],rdx
       jne       near ptr M00_L63
M00_L07:
       test      rbp,rbp
       je        near ptr M00_L67
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt32Array
       cmp       [rbp],rcx
       jne       near ptr M00_L64
       mov       rcx,[rbp+38]
       mov       ebx,[rcx+8]
M00_L08:
       xor       r14d,r14d
       test      ebx,ebx
       jle       short M00_L11
       mov       rdx,offset MT_AuroraScript.Runtime.Types.ScriptInt32Array
       cmp       [rbp],rdx
       jne       near ptr M00_L20
M00_L09:
       mov       r15,[rbp+38]
       cmp       r14d,[r15+8]
       jae       near ptr M00_L69
       mov       edx,r14d
       mov       r13d,[r15+rdx*4+10]
       test      r13d,r13d
       je        near ptr M00_L17
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2sd xmm0,xmm0,r13d
       vmovq     rax,xmm0
M00_L10:
       xor       edx,edx
       mov       [rsp+60],rdx
       mov       [rsp+68],rax
       mov       [rsp+28],rsi
       mov       [rsp+30],rdi
       lea       rdx,[rsp+28]
       lea       rcx,[rsp+60]
       call      qword ptr [7FF977C45C38]; AuroraScript.Runtime.ScriptDatum.Equals(AuroraScript.Runtime.ScriptDatum)
       test      eax,eax
       jne       near ptr M00_L21
       inc       r14d
       cmp       r14d,ebx
       jl        short M00_L09
M00_L11:
       xor       eax,eax
M00_L12:
       add       rsp,80
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r13
       pop       r14
       pop       r15
       ret
M00_L13:
       mov       rax,8001400A00
       cmp       rdx,[rax]
       je        near ptr M00_L02
       mov       rcx,8001400A08
       cmp       rdx,[rcx]
       je        near ptr M00_L02
       movsx     rax,bx
       jmp       near ptr M00_L01
M00_L14:
       mov       rax,[rsp+40]
       mov       rcx,8001400A00
       cmp       rax,[rcx]
       je        near ptr M00_L34
       mov       rcx,[rsp+40]
       mov       rax,8001400A08
       movsx     rdx,word ptr [rsp+48]
       mov       r8d,2000
       cmp       rcx,[rax]
       mov       eax,r8d
       cmovne    eax,edx
       jmp       near ptr M00_L03
M00_L15:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptArray
       cmp       [rbx],rcx
       jne       near ptr M00_L06
       mov       ebp,[rbx+34]
       xor       r14d,r14d
       cmp       r14d,ebp
       jge       near ptr M00_L11
M00_L16:
       mov       rcx,[rbx+38]
       cmp       r14d,[rcx+8]
       jae       near ptr M00_L69
       mov       rdx,r14
       shl       rdx,4
       lea       rcx,[rcx+rdx+10]
       mov       [rsp+28],rsi
       mov       [rsp+30],rdi
       lea       rdx,[rsp+28]
       call      qword ptr [7FF977C45C38]; AuroraScript.Runtime.ScriptDatum.Equals(AuroraScript.Runtime.ScriptDatum)
       test      eax,eax
       jne       near ptr M00_L21
       inc       r14d
       cmp       r14d,ebp
       jl        short M00_L16
       jmp       near ptr M00_L11
M00_L17:
       mov       rax,7FF8000000000001
       jmp       near ptr M00_L10
M00_L18:
       xor       edx,edx
       mov       [rsp+60],rdx
       mov       [rsp+68],rax
M00_L19:
       mov       [rsp+28],rsi
       mov       [rsp+30],rdi
       lea       rdx,[rsp+28]
       lea       rcx,[rsp+60]
       call      qword ptr [7FF977C45C38]; AuroraScript.Runtime.ScriptDatum.Equals(AuroraScript.Runtime.ScriptDatum)
       test      eax,eax
       jne       short M00_L21
       inc       r14d
       cmp       r14d,ebx
       jge       near ptr M00_L11
M00_L20:
       mov       rdx,offset MT_AuroraScript.Runtime.Types.ScriptInt32Array
       cmp       [rbp],rdx
       jne       near ptr M00_L65
       mov       r15,[rbp+38]
       cmp       r14d,[r15+8]
       jae       near ptr M00_L69
       mov       edx,r14d
       mov       r13d,[r15+rdx*4+10]
       test      r13d,r13d
       je        near ptr M00_L66
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2sd xmm0,xmm0,r13d
       vmovq     rax,xmm0
       jmp       short M00_L18
M00_L21:
       mov       eax,1
       jmp       near ptr M00_L12
M00_L22:
       xor       eax,eax
       jmp       near ptr M00_L01
M00_L23:
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbx,rax
       mov       rdx,0A033CE0008
       test      rbx,rbx
       cmove     rbx,rdx
       mov       rbp,rbx
       test      rsi,rsi
       je        short M00_L24
       mov       rcx,8001400A00
       cmp       rsi,[rcx]
       je        near ptr M00_L28
       mov       rdx,8001400A08
       cmp       rsi,[rdx]
       je        near ptr M00_L28
       movsx     rcx,di
       jmp       short M00_L27
M00_L24:
       test      rdi,rdi
       je        short M00_L26
       lea       rdx,[rdi-1]
       mov       ecx,1
       mov       eax,2
       cmp       rdx,1
       cmova     ecx,eax
M00_L25:
       jmp       short M00_L27
M00_L26:
       xor       ecx,ecx
       jmp       short M00_L25
M00_L27:
       cmp       ecx,4
       jne       short M00_L28
       mov       rdx,rsi
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdx,rax
       mov       r9,0A033CE0008
       test      rdx,rdx
       cmove     rdx,r9
       cmp       dword ptr [rdx+8],0
       je        near ptr M00_L11
       mov       dword ptr [rsp+20],4
       mov       r9d,[rbx+8]
       mov       rcx,rbx
       xor       r8d,r8d
       call      qword ptr [7FF977C47CD8]
       not       eax
       shr       eax,1F
       jmp       near ptr M00_L12
M00_L28:
       xor       ebx,ebx
       jmp       near ptr M00_L33
M00_L29:
       movzx     r14d,word ptr [rbp+rbx*2+0C]
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      qword ptr [7FF977845728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001401498
       mov       rcx,[rcx]
       cmp       [rcx+8],r14d
       jg        short M00_L30
       mov       ecx,1
       call      qword ptr [7FF977846670]; System.String.FastAllocateString(IntPtr)
       mov       [rax+0C],r14w
       jmp       short M00_L31
M00_L30:
       mov       ecx,r14d
       call      qword ptr [7FF977C47CF0]
       mov       rax,[rax+38]
M00_L31:
       test      rax,rax
       jne       short M00_L32
       mov       rax,0A033CE0008
M00_L32:
       mov       [rsp+50],rax
       mov       qword ptr [rsp+58],4
       mov       [rsp+28],rsi
       mov       [rsp+30],rdi
       lea       rdx,[rsp+28]
       lea       rcx,[rsp+50]
       call      qword ptr [7FF977C45C38]; AuroraScript.Runtime.ScriptDatum.Equals(AuroraScript.Runtime.ScriptDatum)
       test      eax,eax
       jne       near ptr M00_L21
       inc       ebx
M00_L33:
       cmp       [rbp+8],ebx
       jg        near ptr M00_L29
       jmp       near ptr M00_L11
M00_L34:
       mov       eax,1000
       jmp       near ptr M00_L03
M00_L35:
       mov       ecx,eax
       lea       rax,[7FF9778C2060]
       mov       eax,[rax+rcx*4]
       lea       rdx,[M00_L00]
       add       rax,rdx
       jmp       rax
M00_L36:
       xor       eax,eax
       jmp       near ptr M00_L03
M00_L37:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbx,rax
       jmp       near ptr M00_L04
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       call      qword ptr [7FF977845728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001400A50
       mov       rbx,[rcx]
       jmp       near ptr M00_L05
       cmp       qword ptr [rsp+48],2
       je        short M00_L38
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BooleanValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001401520
       mov       rbx,[rcx]
       jmp       short M00_L39
M00_L38:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BooleanValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001401518
       mov       rbx,[rcx]
M00_L39:
       jmp       near ptr M00_L05
       mov       rcx,[rsp+48]
       mov       rax,8007FFFFFFFFFFFF
       add       rax,rcx
       cmp       rax,3
       jbe       short M00_L40
       vmovq     xmm1,rcx
       jmp       short M00_L42
M00_L40:
       cmp       rax,2
       jbe       short M00_L41
       vmovsd    xmm1,qword ptr [7FF9778C2078]
       jmp       short M00_L42
M00_L41:
       vmovq     xmm1,rax
M00_L42:
       vucomisd  xmm1,xmm1
       jp        short M00_L43
       je        short M00_L44
M00_L43:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001401508
       mov       rbx,[rcx]
       jmp       near ptr M00_L56
M00_L44:
       vxorps    xmm0,xmm0,xmm0
       vucomisd  xmm1,xmm0
       jp        short M00_L45
       jne       short M00_L45
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80014014B8
       mov       rbx,[rcx]
       jmp       near ptr M00_L56
M00_L45:
       vucomisd  xmm1,qword ptr [7FF9778C2080]
       jp        short M00_L46
       jne       short M00_L46
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80014014C0
       mov       rbx,[rcx]
       jmp       near ptr M00_L56
M00_L46:
       vucomisd  xmm1,qword ptr [7FF9778C2088]
       jp        short M00_L47
       jne       short M00_L47
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80014014C8
       mov       rbx,[rcx]
       jmp       near ptr M00_L56
M00_L47:
       vucomisd  xmm1,qword ptr [7FF9778C2090]
       jp        short M00_L48
       jne       short M00_L48
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80014014D0
       mov       rbx,[rcx]
       jmp       near ptr M00_L56
M00_L48:
       vucomisd  xmm1,qword ptr [7FF9778C2098]
       jp        short M00_L49
       jne       short M00_L49
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80014014D8
       mov       rbx,[rcx]
       jmp       near ptr M00_L56
M00_L49:
       vucomisd  xmm1,qword ptr [7FF9778C20A0]
       jp        short M00_L50
       jne       short M00_L50
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80014014E0
       mov       rbx,[rcx]
       jmp       near ptr M00_L56
M00_L50:
       vucomisd  xmm1,qword ptr [7FF9778C20A8]
       jp        short M00_L51
       jne       short M00_L51
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80014014E8
       mov       rbx,[rcx]
       jmp       near ptr M00_L56
M00_L51:
       vucomisd  xmm1,qword ptr [7FF9778C20B0]
       jp        short M00_L52
       jne       short M00_L52
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80014014F0
       mov       rbx,[rcx]
       jmp       near ptr M00_L56
M00_L52:
       vucomisd  xmm1,qword ptr [7FF9778C20B8]
       jp        short M00_L53
       jne       short M00_L53
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80014014F8
       mov       rbx,[rcx]
       jmp       near ptr M00_L56
M00_L53:
       vucomisd  xmm1,qword ptr [7FF9778C20C0]
       jp        short M00_L54
       jne       short M00_L54
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001401500
       mov       rbx,[rcx]
       jmp       short M00_L56
M00_L54:
       vmovsd    qword ptr [rsp+38],xmm1
       vucomisd  xmm1,qword ptr [7FF9778C20C8]
       jp        short M00_L55
       jne       short M00_L55
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80014014B0
       mov       rbx,[rcx]
       jmp       short M00_L56
M00_L55:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       rcx,rbx
       vmovsd    xmm1,qword ptr [rsp+38]
       call      qword ptr [7FF977C47D08]
M00_L56:
       jmp       near ptr M00_L05
M00_L57:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.Int64Value
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       rcx,rbx
       mov       rdx,[rsp+48]
       call      qword ptr [7FF977C45D40]
       jmp       near ptr M00_L05
M00_L58:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.UInt64Value
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       rcx,rbx
       mov       rdx,[rsp+48]
       call      qword ptr [7FF977C45D70]
       jmp       near ptr M00_L05
       lea       rcx,[rsp+40]
       call      qword ptr [7FF977C45D88]
       mov       rbx,rax
       jmp       near ptr M00_L05
M00_L59:
       mov       rdx,[rsp+40]
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       short M00_L60
       xor       ebx,ebx
       jmp       near ptr M00_L05
M00_L60:
       cmp       dword ptr [rbp+8],0
       jne       short M00_L61
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      qword ptr [7FF977845728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80014014A0
       mov       rbx,[rcx]
       jmp       short M00_L62
M00_L61:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       rcx,rbx
       mov       rdx,rbp
       call      qword ptr [7FF977C47D20]
M00_L62:
       jmp       near ptr M00_L05
M00_L63:
       mov       rdx,rbx
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptPackedArray
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       jmp       near ptr M00_L07
M00_L64:
       mov       rcx,rbp
       mov       rax,[rbp]
       mov       rax,[rax+58]
       call      qword ptr [rax+8]
       mov       ebx,eax
       jmp       near ptr M00_L08
M00_L65:
       lea       rdx,[rsp+60]
       mov       rcx,rbp
       mov       r8d,r14d
       mov       rax,[rbp]
       mov       rax,[rax+58]
       call      qword ptr [rax+10]
       jmp       near ptr M00_L19
M00_L66:
       mov       rax,7FF8000000000001
       jmp       near ptr M00_L18
M00_L67:
       mov       rcx,rbx
       mov       rax,[rbx]
       mov       rax,[rax+58]
       call      qword ptr [rax]
       mov       rbx,rax
M00_L68:
       lea       rdx,[rsp+70]
       mov       rcx,rbx
       cmp       [rcx],ecx
       call      qword ptr [7FF977C45CB0]
       test      eax,eax
       je        near ptr M00_L11
       mov       [rsp+28],rsi
       mov       [rsp+30],rdi
       lea       rdx,[rsp+28]
       lea       rcx,[rsp+70]
       call      qword ptr [7FF977C45C38]; AuroraScript.Runtime.ScriptDatum.Equals(AuroraScript.Runtime.ScriptDatum)
       test      eax,eax
       je        short M00_L68
       jmp       near ptr M00_L21
M00_L69:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 2398
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
       jne       near ptr M01_L07
       mov       rdx,[rsp+48]
       test      rdx,rdx
       je        near ptr M01_L12
       dec       rdx
       mov       eax,1
       mov       ecx,2
       cmp       rdx,1
       cmova     eax,ecx
M01_L01:
       cmp       qword ptr [rsp+30],0
       jne       near ptr M01_L08
       mov       rdx,[rsp+38]
       test      rdx,rdx
       je        near ptr M01_L14
       dec       rdx
       mov       r8d,1
       mov       ecx,2
       cmp       rdx,1
       cmova     r8d,ecx
M01_L02:
       cmp       eax,r8d
       jne       near ptr M01_L21
       cmp       r8d,2
       jne       near ptr M01_L15
       mov       rax,[rsp+48]
       mov       rcx,8007FFFFFFFFFFFF
       add       rcx,rax
       cmp       rcx,3
       jbe       near ptr M01_L09
       vmovq     xmm0,rax
M01_L03:
       mov       rcx,[rsp+38]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       near ptr M01_L10
       vmovq     xmm1,rcx
M01_L04:
       vucomisd  xmm0,xmm1
       setnp     al
       jp        short M01_L05
       sete      al
M01_L05:
       movzx     eax,al
M01_L06:
       movzx     eax,al
       add       rsp,50
       pop       rbx
       ret
M01_L07:
       mov       rax,[rsp+40]
       mov       rdx,8001400A00
       cmp       rax,[rdx]
       je        near ptr M01_L11
       mov       rdx,[rsp+40]
       mov       rcx,8001400A08
       movsx     rax,word ptr [rsp+48]
       mov       r8d,2000
       cmp       rdx,[rcx]
       cmove     eax,r8d
       jmp       near ptr M01_L01
M01_L08:
       mov       r8,[rsp+30]
       mov       rdx,8001400A00
       cmp       r8,[rdx]
       je        short M01_L13
       mov       rdx,[rsp+30]
       mov       rcx,8001400A08
       movsx     r8,word ptr [rsp+38]
       mov       r10d,2000
       cmp       rdx,[rcx]
       cmove     r8d,r10d
       jmp       near ptr M01_L02
M01_L09:
       cmp       rcx,2
       ja        near ptr M01_L17
       vmovq     xmm0,rcx
       jmp       near ptr M01_L03
M01_L10:
       cmp       rdx,2
       ja        near ptr M01_L18
       vmovq     xmm1,rdx
       jmp       near ptr M01_L04
M01_L11:
       mov       eax,1000
       jmp       near ptr M01_L01
M01_L12:
       xor       eax,eax
       jmp       near ptr M01_L01
M01_L13:
       mov       r8d,1000
       jmp       near ptr M01_L02
M01_L14:
       xor       r8d,r8d
       jmp       near ptr M01_L02
M01_L15:
       cmp       r8d,4
       ja        short M01_L16
       mov       edx,r8d
       lea       rcx,[7FF9778C16A0]
       mov       ecx,[rcx+rdx*4]
       lea       rax,[M01_L00]
       add       rcx,rax
       jmp       rcx
M01_L16:
       cmp       r8d,1000
       je        short M01_L19
       cmp       r8d,2000
       jne       near ptr M01_L20
       mov       rax,[rsp+48]
       cmp       rax,[rsp+38]
       sete      al
       movzx     eax,al
       jmp       near ptr M01_L06
       mov       eax,1
       jmp       near ptr M01_L06
       cmp       qword ptr [rsp+48],2
       sete      al
       movzx     eax,al
       cmp       qword ptr [rsp+38],2
       sete      cl
       movzx     ecx,cl
       cmp       eax,ecx
       sete      al
       movzx     eax,al
       jmp       near ptr M01_L06
M01_L17:
       vmovsd    xmm0,qword ptr [7FF9778C16B8]
       jmp       near ptr M01_L03
M01_L18:
       vmovsd    xmm1,qword ptr [7FF9778C16B8]
       jmp       near ptr M01_L04
M01_L19:
       mov       rax,[rsp+48]
       cmp       rax,[rsp+38]
       sete      al
       movzx     eax,al
       jmp       near ptr M01_L06
       mov       rdx,[rsp+40]
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbx,rax
       mov       rdx,[rsp+30]
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdx,rax
       mov       rcx,rbx
       call      qword ptr [7FF977846E08]; System.String.Equals(System.String, System.String)
       jmp       near ptr M01_L06
M01_L20:
       mov       rax,[rsp+40]
       cmp       rax,[rsp+30]
       sete      al
       movzx     eax,al
       jmp       near ptr M01_L06
M01_L21:
       cmp       eax,1000
       jne       short M01_L22
       cmp       r8d,2000
       jne       short M01_L22
       cmp       qword ptr [rsp+48],0
       jl        near ptr M01_L35
       mov       rax,[rsp+48]
       cmp       rax,[rsp+38]
       sete      al
       movzx     eax,al
       jmp       near ptr M01_L06
M01_L22:
       cmp       eax,2000
       jne       short M01_L23
       cmp       r8d,1000
       jne       short M01_L23
       cmp       qword ptr [rsp+38],0
       jl        near ptr M01_L35
       mov       rcx,[rsp+48]
       cmp       rcx,[rsp+38]
       sete      al
       movzx     eax,al
       jmp       near ptr M01_L06
M01_L23:
       cmp       qword ptr [rsp+40],0
       jne       short M01_L27
       cmp       qword ptr [rsp+48],2
       jbe       short M01_L27
       mov       rcx,[rsp+48]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M01_L24
       vmovq     xmm0,rcx
       jmp       short M01_L26
M01_L24:
       cmp       rdx,2
       jbe       short M01_L25
       vmovsd    xmm0,qword ptr [7FF9778C16B8]
       jmp       short M01_L26
M01_L25:
       vmovq     xmm0,rdx
M01_L26:
       vmovsd    qword ptr [rsp+28],xmm0
       jmp       short M01_L28
M01_L27:
       lea       rcx,[rsp+40]
       lea       rdx,[rsp+28]
       call      qword ptr [7FF977C47C90]
       test      eax,eax
       je        near ptr M01_L35
M01_L28:
       cmp       qword ptr [rsp+30],0
       jne       short M01_L32
       cmp       qword ptr [rsp+38],2
       jbe       short M01_L32
       mov       rcx,[rsp+38]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M01_L29
       vmovq     xmm0,rcx
       jmp       short M01_L31
M01_L29:
       cmp       rdx,2
       jbe       short M01_L30
       vmovsd    xmm0,qword ptr [7FF9778C16B8]
       jmp       short M01_L31
M01_L30:
       vmovq     xmm0,rdx
M01_L31:
       vmovsd    qword ptr [rsp+20],xmm0
       jmp       short M01_L33
M01_L32:
       lea       rcx,[rsp+30]
       lea       rdx,[rsp+20]
       call      qword ptr [7FF977C47C90]
       test      eax,eax
       je        short M01_L35
M01_L33:
       vmovsd    xmm0,qword ptr [rsp+28]
       vucomisd  xmm0,qword ptr [rsp+20]
       setnp     al
       jp        short M01_L34
       sete      al
M01_L34:
       movzx     eax,al
       jmp       near ptr M01_L06
M01_L35:
       xor       eax,eax
       add       rsp,50
       pop       rbx
       ret
; Total bytes of code 1021
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
       jmp       short M02_L00
; Total bytes of code 86
```
```assembly
; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rax,[rcx+20]
       mov       rax,[rax-18]
       mov       rdx,rax
       test      dl,1
       jne       short M03_L00
       ret
M03_L00:
       jmp       qword ptr [7FF977845C38]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBaseSlow(System.Runtime.CompilerServices.MethodTable*)
; Total bytes of code 23
```
```assembly
; System.String.FastAllocateString(IntPtr)
       mov       rdx,rcx
       mov       rcx,offset MT_System.String
       jmp       near ptr 00007FF9D74C4E80
; Total bytes of code 18
```
```assembly
; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rax,[rcx+20]
       mov       rax,[rax-10]
       mov       rdx,rax
       test      dl,1
       jne       short M05_L00
       ret
M05_L00:
       jmp       qword ptr [7FF977A1E838]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBaseSlow(System.Runtime.CompilerServices.MethodTable*)
; Total bytes of code 23
```
```assembly
; System.String.Equals(System.String, System.String)
       sub       rsp,28
       cmp       rcx,rdx
       je        short M06_L02
       test      rcx,rcx
       je        short M06_L00
       test      rdx,rdx
       je        short M06_L00
       mov       r8d,[rcx+8]
       cmp       r8d,[rdx+8]
       je        short M06_L01
M06_L00:
       xor       eax,eax
       add       rsp,28
       ret
M06_L01:
       lea       rax,[rcx+0C]
       add       rdx,0C
       mov       r8d,[rcx+8]
       add       r8d,r8d
       mov       rcx,rax
       call      qword ptr [7FF96E84D900]; Precode of System.SpanHelpers.SequenceEqual(Byte ByRef, Byte ByRef, UIntPtr)
       nop
       add       rsp,28
       ret
M06_L02:
       mov       eax,1
       add       rsp,28
       ret
; Total bytes of code 76
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
```assembly
; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBaseSlow(System.Runtime.CompilerServices.MethodTable*)
       push      rbx
       sub       rsp,30
       xor       eax,eax
       mov       [rsp+28],rax
       mov       rbx,rcx
       mov       rcx,rbx
       call      qword ptr [7FF96E854D70]; Precode of System.Runtime.CompilerServices.InitHelpers.InitClassSlow(System.Runtime.CompilerServices.MethodTable*)
       mov       rax,[rbx+20]
       mov       rax,[rax-10]
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
; AuroraBenchmark.SafeOpsBenchmarks.CharacterMiss()
       push      r14
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,80
       xor       eax,eax
       mov       [rsp+28],rax
       vxorps    xmm4,xmm4,xmm4
       vmovdqu   ymmword ptr [rsp+30],ymm4
       vmovdqu   ymmword ptr [rsp+50],ymm4
       vmovdqa   xmmword ptr [rsp+70],xmm4
M00_L00:
       mov       rbx,[rcx+30]
       mov       rsi,[rcx+38]
       mov       rdi,[rcx+40]
       mov       rbp,[rcx+48]
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rsp+50],xmm0
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rsp+60],xmm0
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rsp+70],xmm0
       test      rbx,rbx
       je        near ptr M00_L08
       mov       rcx,8001000A00
       cmp       rbx,[rcx]
       je        near ptr M00_L20
       mov       rcx,8001000A08
       cmp       rbx,[rcx]
       je        near ptr M00_L20
       movsx     rcx,si
M00_L01:
       cmp       ecx,4
       jne       near ptr M00_L20
       test      rbx,rbx
       je        short M00_L02
       mov       rcx,offset MT_System.String
       cmp       [rbx],rcx
       jne       short M00_L02
       jmp       short M00_L03
M00_L02:
       xor       ebx,ebx
M00_L03:
       mov       rcx,0A033CF0008
       test      rbx,rbx
       cmove     rbx,rcx
       mov       rsi,rbx
       test      rdi,rdi
       je        near ptr M00_L11
       mov       rcx,8001000A00
       cmp       rdi,[rcx]
       je        near ptr M00_L14
       mov       rcx,8001000A08
       cmp       rdi,[rcx]
       je        near ptr M00_L14
       movsx     rcx,bp
M00_L04:
       cmp       ecx,4
       jne       near ptr M00_L14
       test      rdi,rdi
       je        short M00_L05
       mov       rcx,offset MT_System.String
       cmp       [rdi],rcx
       jne       short M00_L05
       jmp       short M00_L06
M00_L05:
       xor       edi,edi
M00_L06:
       mov       rcx,0A033CF0008
       test      rdi,rdi
       cmove     rdi,rcx
       mov       r9d,[rdi+8]
       test      r9d,r9d
       je        near ptr M00_L54
       mov       edx,[rbx+8]
       lea       rcx,[rbx+0C]
       lea       r8,[rdi+0C]
       call      qword ptr [7FF977C45CF8]; System.SpanHelpers.IndexOf(Char ByRef, Int32, Char ByRef, Int32)
       not       eax
       shr       eax,1F
M00_L07:
       add       rsp,80
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       ret
M00_L08:
       test      rsi,rsi
       je        short M00_L10
       lea       rcx,[rsi-1]
       mov       edx,1
       mov       r8d,2
       cmp       rcx,1
       cmova     edx,r8d
       mov       ecx,edx
M00_L09:
       jmp       near ptr M00_L01
M00_L10:
       xor       ecx,ecx
       jmp       short M00_L09
M00_L11:
       test      rbp,rbp
       je        short M00_L13
       lea       rcx,[rbp-1]
       mov       edx,1
       mov       r8d,2
       cmp       rcx,1
       cmova     edx,r8d
       mov       ecx,edx
M00_L12:
       jmp       near ptr M00_L04
M00_L13:
       xor       ecx,ecx
       jmp       short M00_L12
M00_L14:
       xor       ebx,ebx
       jmp       near ptr M00_L19
M00_L15:
       movzx     r14d,word ptr [rsi+rbx*2+0C]
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      qword ptr [7FF977845728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80010014D0
       mov       rcx,[rcx]
       cmp       [rcx+8],r14d
       jg        short M00_L16
       mov       ecx,1
       call      qword ptr [7FF977846670]; System.String.FastAllocateString(IntPtr)
       mov       [rax+0C],r14w
       jmp       short M00_L17
M00_L16:
       mov       ecx,r14d
       call      qword ptr [7FF977C4C720]
       mov       rax,[rax+38]
M00_L17:
       test      rax,rax
       jne       short M00_L18
       mov       rax,0A033CF0008
M00_L18:
       mov       [rsp+50],rax
       mov       qword ptr [rsp+58],4
       mov       [rsp+28],rdi
       mov       [rsp+30],rbp
       lea       rdx,[rsp+28]
       lea       rcx,[rsp+50]
       call      qword ptr [7FF977C45C38]
       test      eax,eax
       jne       near ptr M00_L57
       inc       ebx
M00_L19:
       mov       edx,[rsi+8]
       cmp       edx,ebx
       jg        near ptr M00_L15
       jmp       near ptr M00_L54
M00_L20:
       mov       [rsp+40],rbx
       mov       [rsp+48],rsi
       cmp       qword ptr [rsp+40],0
       je        short M00_L23
       mov       rcx,[rsp+40]
       mov       rax,8001000A00
       cmp       rcx,[rax]
       jne       short M00_L21
       mov       edx,1000
       jmp       short M00_L22
M00_L21:
       mov       rcx,[rsp+40]
       mov       rax,8001000A08
       movsx     rdx,word ptr [rsp+48]
       mov       r8d,2000
       cmp       rcx,[rax]
       cmove     edx,r8d
M00_L22:
       cmp       edx,4
       ja        short M00_L26
       mov       ecx,edx
       lea       rax,[7FF9778C2680]
       mov       eax,[rax+rcx*4]
       lea       rdx,[M00_L00]
       add       rax,rdx
       jmp       rax
M00_L23:
       mov       rdx,[rsp+48]
       test      rdx,rdx
       je        short M00_L25
       dec       rdx
       mov       ecx,1
       mov       eax,2
       cmp       rdx,1
       cmova     ecx,eax
       mov       edx,ecx
M00_L24:
       jmp       short M00_L22
M00_L25:
       xor       edx,edx
       jmp       short M00_L24
M00_L26:
       cmp       edx,1000
       je        near ptr M00_L46
       cmp       edx,2000
       jne       near ptr M00_L47
       mov       rcx,offset MT_AuroraScript.Runtime.Types.UInt64Value
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       rcx,rbx
       mov       rdx,[rsp+48]
       call      qword ptr [7FF977C4C738]
       jmp       near ptr M00_L48
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       call      qword ptr [7FF977845728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001000A50
       mov       rbx,[rcx]
       jmp       near ptr M00_L48
       cmp       qword ptr [rsp+48],2
       je        short M00_L27
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BooleanValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001001558
       mov       rbx,[rcx]
       jmp       short M00_L28
M00_L27:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BooleanValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001001550
       mov       rbx,[rcx]
M00_L28:
       jmp       near ptr M00_L48
       mov       rcx,[rsp+48]
       mov       rax,8007FFFFFFFFFFFF
       add       rax,rcx
       cmp       rax,3
       jbe       short M00_L29
       vmovq     xmm1,rcx
       jmp       short M00_L31
M00_L29:
       cmp       rax,2
       jbe       short M00_L30
       vmovsd    xmm1,qword ptr [7FF9778C2698]
       jmp       short M00_L31
M00_L30:
       vmovq     xmm1,rax
M00_L31:
       vucomisd  xmm1,xmm1
       jp        short M00_L32
       je        short M00_L33
M00_L32:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001001540
       mov       rbx,[rcx]
       jmp       near ptr M00_L45
M00_L33:
       vxorps    xmm0,xmm0,xmm0
       vucomisd  xmm1,xmm0
       jp        short M00_L34
       jne       short M00_L34
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80010014F0
       mov       rbx,[rcx]
       jmp       near ptr M00_L45
M00_L34:
       vucomisd  xmm1,qword ptr [7FF9778C26A0]
       jp        short M00_L35
       jne       short M00_L35
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80010014F8
       mov       rbx,[rcx]
       jmp       near ptr M00_L45
M00_L35:
       vucomisd  xmm1,qword ptr [7FF9778C26A8]
       jp        short M00_L36
       jne       short M00_L36
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001001500
       mov       rbx,[rcx]
       jmp       near ptr M00_L45
M00_L36:
       vucomisd  xmm1,qword ptr [7FF9778C26B0]
       jp        short M00_L37
       jne       short M00_L37
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001001508
       mov       rbx,[rcx]
       jmp       near ptr M00_L45
M00_L37:
       vucomisd  xmm1,qword ptr [7FF9778C26B8]
       jp        short M00_L38
       jne       short M00_L38
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001001510
       mov       rbx,[rcx]
       jmp       near ptr M00_L45
M00_L38:
       vucomisd  xmm1,qword ptr [7FF9778C26C0]
       jp        short M00_L39
       jne       short M00_L39
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001001518
       mov       rbx,[rcx]
       jmp       near ptr M00_L45
M00_L39:
       vucomisd  xmm1,qword ptr [7FF9778C26C8]
       jp        short M00_L40
       jne       short M00_L40
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001001520
       mov       rbx,[rcx]
       jmp       near ptr M00_L45
M00_L40:
       vucomisd  xmm1,qword ptr [7FF9778C26D0]
       jp        short M00_L41
       jne       short M00_L41
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001001528
       mov       rbx,[rcx]
       jmp       near ptr M00_L45
M00_L41:
       vucomisd  xmm1,qword ptr [7FF9778C26D8]
       jp        short M00_L42
       jne       short M00_L42
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001001530
       mov       rbx,[rcx]
       jmp       near ptr M00_L45
M00_L42:
       vucomisd  xmm1,qword ptr [7FF9778C26E0]
       jp        short M00_L43
       jne       short M00_L43
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001001538
       mov       rbx,[rcx]
       jmp       short M00_L45
M00_L43:
       vmovsd    qword ptr [rsp+38],xmm1
       vucomisd  xmm1,qword ptr [7FF9778C26E8]
       jp        short M00_L44
       jne       short M00_L44
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80010014E8
       mov       rbx,[rcx]
       jmp       short M00_L45
M00_L44:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       rcx,rbx
       vmovsd    xmm1,qword ptr [rsp+38]
       call      qword ptr [7FF977C4C750]
M00_L45:
       jmp       short M00_L48
M00_L46:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.Int64Value
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       rcx,rbx
       mov       rdx,[rsp+48]
       call      qword ptr [7FF977C4C768]
       jmp       short M00_L48
       lea       rcx,[rsp+40]
       call      qword ptr [7FF977C4C780]
       mov       rbx,rax
       jmp       short M00_L48
M00_L47:
       lea       rcx,[rsp+40]
       call      qword ptr [7FF977C4C798]
       mov       rbx,rax
M00_L48:
       mov       rdx,rbx
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptArray
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rsi,rax
       test      rsi,rsi
       je        short M00_L51
       mov       ebx,[rsi+34]
       xor       r14d,r14d
       jmp       short M00_L50
M00_L49:
       mov       rcx,[rsi+38]
       cmp       r14d,[rcx+8]
       jae       near ptr M00_L58
       mov       rdx,r14
       shl       rdx,4
       lea       rcx,[rcx+rdx+10]
       mov       [rsp+28],rdi
       mov       [rsp+30],rbp
       lea       rdx,[rsp+28]
       call      qword ptr [7FF977C45C38]
       test      eax,eax
       jne       near ptr M00_L57
       inc       r14d
M00_L50:
       cmp       r14d,ebx
       jl        short M00_L49
       jmp       short M00_L54
M00_L51:
       mov       rdx,rbx
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptPackedArray
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rsi,rax
       test      rsi,rsi
       je        short M00_L55
       mov       rcx,rsi
       mov       rax,[rsi]
       mov       rax,[rax+58]
       call      qword ptr [rax+8]
       mov       ebx,eax
       xor       r14d,r14d
       jmp       short M00_L53
M00_L52:
       lea       rdx,[rsp+60]
       mov       rcx,rsi
       mov       r8d,r14d
       mov       rax,[rsi]
       mov       rax,[rax+58]
       call      qword ptr [rax+10]
       mov       [rsp+28],rdi
       mov       [rsp+30],rbp
       lea       rdx,[rsp+28]
       lea       rcx,[rsp+60]
       call      qword ptr [7FF977C45C38]
       test      eax,eax
       jne       short M00_L57
       inc       r14d
M00_L53:
       cmp       r14d,ebx
       jl        short M00_L52
M00_L54:
       xor       eax,eax
       jmp       near ptr M00_L07
M00_L55:
       mov       rcx,rbx
       mov       rax,[rbx]
       mov       rax,[rax+58]
       call      qword ptr [rax]
       mov       rbx,rax
M00_L56:
       lea       rdx,[rsp+70]
       mov       rcx,rbx
       cmp       [rcx],ecx
       call      qword ptr [7FF977C45CB0]
       test      eax,eax
       je        short M00_L54
       mov       [rsp+28],rdi
       mov       [rsp+30],rbp
       lea       rdx,[rsp+28]
       lea       rcx,[rsp+70]
       call      qword ptr [7FF977C45C38]
       test      eax,eax
       je        short M00_L56
M00_L57:
       mov       eax,1
       jmp       near ptr M00_L07
M00_L58:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 1885
```
```assembly
; System.SpanHelpers.IndexOf(Char ByRef, Int32, Char ByRef, Int32)
       push      r15
       push      r14
       push      r13
       push      r12
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,78
       vmovaps   [rsp+60],xmm6
       vmovaps   [rsp+50],xmm7
       vmovaps   [rsp+40],xmm8
       vmovaps   [rsp+30],xmm9
       mov       rsi,rcx
       mov       rdi,r8
       mov       r8d,edx
       mov       ebx,r9d
       test      ebx,ebx
       je        short M01_L00
       lea       ebp,[rbx-1]
       test      ebp,ebp
       jne       near ptr M01_L02
       movsx     rdx,word ptr [rdi]
       movzx     ecx,dx
       dec       ecx
       cmp       ecx,0FE
       jae       short M01_L01
       movsx     rdx,dx
       mov       rcx,rsi
       vzeroupper
       vmovaps   xmm6,[rsp+60]
       vmovaps   xmm7,[rsp+50]
       vmovaps   xmm8,[rsp+40]
       vmovaps   xmm9,[rsp+30]
       add       rsp,78
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       jmp       qword ptr [7FF977A1C420]; System.PackedSpanHelpers.IndexOf[[System.SpanHelpers+DontNegate`1[[System.Int16, System.Private.CoreLib]], System.Private.CoreLib],[System.PackedSpanHelpers+NopTransform, System.Private.CoreLib]](Int16 ByRef, Int16, Int32)
M01_L00:
       xor       eax,eax
       vzeroupper
       vmovaps   xmm6,[rsp+60]
       vmovaps   xmm7,[rsp+50]
       vmovaps   xmm8,[rsp+40]
       vmovaps   xmm9,[rsp+30]
       add       rsp,78
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       ret
M01_L01:
       mov       rcx,rsi
       vzeroupper
       vmovaps   xmm6,[rsp+60]
       vmovaps   xmm7,[rsp+50]
       vmovaps   xmm8,[rsp+40]
       vmovaps   xmm9,[rsp+30]
       add       rsp,78
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       jmp       qword ptr [7FF977847288]; System.SpanHelpers.NonPackedIndexOfValueType[[System.Int16, System.Private.CoreLib],[System.SpanHelpers+DontNegate`1[[System.Int16, System.Private.CoreLib]], System.Private.CoreLib]](Int16 ByRef, Int16, Int32)
M01_L02:
       xor       r14d,r14d
       movzx     r15d,word ptr [rdi]
       mov       r13d,r8d
       sub       r13d,ebp
       cmp       r13d,8
       jge       near ptr M01_L06
       add       rdi,2
       test      r13d,r13d
       jle       short M01_L05
M01_L03:
       lea       rcx,[rsi+r14*2]
       movsx     rdx,r15w
       mov       r8d,r13d
       call      qword ptr [7FF977847288]; System.SpanHelpers.NonPackedIndexOfValueType[[System.Int16, System.Private.CoreLib],[System.SpanHelpers+DontNegate`1[[System.Int16, System.Private.CoreLib]], System.Private.CoreLib]](Int16 ByRef, Int16, Int32)
       test      eax,eax
       jl        short M01_L05
       sub       r13d,eax
       mov       ecx,eax
       add       r14,rcx
       test      r13d,r13d
       jle       short M01_L05
       lea       rcx,[rsi+r14*2+2]
       mov       r8d,ebp
       add       r8,r8
       mov       rdx,rdi
       call      qword ptr [7FF97784C330]; System.SpanHelpers.SequenceEqual(Byte ByRef, Byte ByRef, UIntPtr)
       test      eax,eax
       jne       short M01_L04
       dec       r13d
       inc       r14
       test      r13d,r13d
       jg        short M01_L03
       jmp       short M01_L05
M01_L04:
       mov       eax,r14d
       jmp       near ptr M01_L26
M01_L05:
       mov       eax,0FFFFFFFF
       vzeroupper
       vmovaps   xmm6,[rsp+60]
       vmovaps   xmm7,[rsp+50]
       vmovaps   xmm8,[rsp+40]
       vmovaps   xmm9,[rsp+30]
       add       rsp,78
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       ret
M01_L06:
       lea       ecx,[r13-10]
       test      ecx,ecx
       jl        near ptr M01_L16
       movsxd    rcx,ebp
       movzx     ecx,word ptr [rdi+rcx*2]
       movsxd    rbp,ebp
       jmp       short M01_L08
M01_L07:
       dec       rbp
       movzx     ecx,word ptr [rdi+rbp*2]
M01_L08:
       cmp       ecx,r15d
       jne       short M01_L09
       cmp       rbp,1
       jg        short M01_L07
M01_L09:
       vmovd     xmm6,r15d
       vpbroadcastw ymm6,xmm6
       vmovd     xmm7,ecx
       vpbroadcastw ymm7,xmm7
       mov       r15d,r13d
       add       r15,0FFFFFFFFFFFFFFF0
M01_L10:
       vpcmpeqw  ymm0,ymm6,[rsi+r14*2]
       lea       rcx,[r14+rbp]
       vpcmpeqw  ymm1,ymm7,[rsi+rcx*2]
       vpand     ymm0,ymm1,ymm0
       vptest    ymm0,ymm0
       jne       short M01_L12
       jmp       short M01_L14
M01_L11:
       cmp       r14,r15
       jle       short M01_L10
       mov       r14,r15
       jmp       short M01_L10
M01_L12:
       vpmovmskb r12d,ymm0
M01_L13:
       xor       ecx,ecx
       tzcnt     ecx,r12d
       shr       ecx,1
       mov       eax,ecx
       mov       [rsp+28],rax
       cmp       ebx,2
       je        short M01_L15
       lea       rcx,[r14+rax]
       lea       rcx,[rsi+rcx*2]
       mov       r8d,ebx
       add       r8,r8
       mov       rdx,rdi
       vextractf128 xmm8,ymm6,1
       vextractf128 xmm9,ymm7,1
       call      qword ptr [7FF97784C330]; System.SpanHelpers.SequenceEqual(Byte ByRef, Byte ByRef, UIntPtr)
       test      eax,eax
       vinsertf128 ymm6,ymm6,xmm8,1
       vinsertf128 ymm7,ymm7,xmm9,1
       mov       rax,[rsp+28]
       jne       short M01_L15
       blsr      ecx,r12d
       blsr      r12d,ecx
       jne       short M01_L13
M01_L14:
       add       r14,10
       mov       ecx,r13d
       cmp       r14,rcx
       je        near ptr M01_L05
       jmp       short M01_L11
M01_L15:
       add       eax,r14d
       jmp       near ptr M01_L26
M01_L16:
       movsxd    rcx,ebp
       movzx     ecx,word ptr [rdi+rcx*2]
       movsxd    rbp,ebp
       jmp       short M01_L18
M01_L17:
       dec       rbp
       movzx     ecx,word ptr [rdi+rbp*2]
M01_L18:
       cmp       ecx,r15d
       jne       short M01_L19
       cmp       rbp,1
       jg        short M01_L17
M01_L19:
       vmovd     xmm6,r15d
       vpbroadcastw xmm6,xmm6
       vmovd     xmm7,ecx
       vpbroadcastw xmm7,xmm7
       mov       r15d,r13d
       add       r15,0FFFFFFFFFFFFFFF8
M01_L20:
       vpcmpeqw  xmm0,xmm6,[rsi+r14*2]
       lea       rcx,[r14+rbp]
       vpcmpeqw  xmm1,xmm7,[rsi+rcx*2]
       vpand     xmm0,xmm1,xmm0
       vptest    xmm0,xmm0
       jne       short M01_L22
       jmp       short M01_L24
M01_L21:
       cmp       r14,r15
       jle       short M01_L20
       mov       r14,r15
       jmp       short M01_L20
M01_L22:
       vpmovmskb r12d,xmm0
M01_L23:
       xor       ecx,ecx
       tzcnt     ecx,r12d
       shr       ecx,1
       mov       eax,ecx
       mov       [rsp+20],rax
       cmp       ebx,2
       je        short M01_L25
       lea       rcx,[r14+rax]
       lea       rcx,[rsi+rcx*2]
       mov       r8d,ebx
       add       r8,r8
       mov       rdx,rdi
       call      qword ptr [7FF97784C330]; System.SpanHelpers.SequenceEqual(Byte ByRef, Byte ByRef, UIntPtr)
       test      eax,eax
       mov       rax,[rsp+20]
       jne       short M01_L25
       blsr      eax,r12d
       blsr      r12d,eax
       jne       short M01_L23
M01_L24:
       add       r14,8
       mov       eax,r13d
       cmp       r14,rax
       je        near ptr M01_L05
       jmp       short M01_L21
M01_L25:
       add       eax,r14d
M01_L26:
       vzeroupper
       vmovaps   xmm6,[rsp+60]
       vmovaps   xmm7,[rsp+50]
       vmovaps   xmm8,[rsp+40]
       vmovaps   xmm9,[rsp+30]
       add       rsp,78
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       ret
; Total bytes of code 851
```
```assembly
; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rax,[rcx+20]
       mov       rax,[rax-18]
       mov       rdx,rax
       test      dl,1
       jne       short M02_L00
       ret
M02_L00:
       jmp       qword ptr [7FF977845C38]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBaseSlow(System.Runtime.CompilerServices.MethodTable*)
; Total bytes of code 23
```
```assembly
; System.String.FastAllocateString(IntPtr)
       mov       rdx,rcx
       mov       rcx,offset MT_System.String
       jmp       near ptr 00007FF9D74C4E80
; Total bytes of code 18
```
```assembly
; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rax,[rcx+20]
       mov       rax,[rax-10]
       mov       rdx,rax
       test      dl,1
       jne       short M04_L00
       ret
M04_L00:
       jmp       qword ptr [7FF977A1E838]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBaseSlow(System.Runtime.CompilerServices.MethodTable*)
; Total bytes of code 23
```
```assembly
; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rdx,rdx
       je        short M05_L00
       cmp       [rdx],rcx
       jne       short M05_L01
M05_L00:
       mov       rax,rdx
       ret
M05_L01:
       mov       rax,[rdx]
       mov       rax,[rax+10]
M05_L02:
       cmp       rax,rcx
       je        short M05_L00
       test      rax,rax
       je        short M05_L03
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M05_L00
       test      rax,rax
       je        short M05_L03
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M05_L00
       test      rax,rax
       je        short M05_L03
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M05_L00
       test      rax,rax
       je        short M05_L03
       mov       rax,[rax+10]
       jmp       short M05_L02
M05_L03:
       xor       edx,edx
       jmp       short M05_L00
; Total bytes of code 83
```
```assembly
; System.PackedSpanHelpers.IndexOf[[System.SpanHelpers+DontNegate`1[[System.Int16, System.Private.CoreLib]], System.Private.CoreLib],[System.PackedSpanHelpers+NopTransform, System.Private.CoreLib]](Int16 ByRef, Int16, Int32)
       cmp       r8d,8
       jl        near ptr M06_L09
       mov       rax,rcx
       movzx     edx,dl
       movd      xmm0,edx
       xorps     xmm1,xmm1
       pshufb    xmm0,xmm1
       cmp       r8d,10
       jle       short M06_L02
       lea       edx,[r8-10]
       movsxd    rdx,edx
       lea       rdx,[rax+rdx*2]
       movups    xmm1,[rcx]
       movups    xmm2,[rcx+10]
       packuswb  xmm1,xmm2
       pcmpeqb   xmm1,xmm0
       ptest     xmm1,xmm1
       jne       short M06_L01
M06_L00:
       add       rax,20
       cmp       rax,rdx
       jae       short M06_L02
       movups    xmm1,[rax]
       movups    xmm2,[rax+10]
       packuswb  xmm1,xmm2
       pcmpeqb   xmm1,xmm0
       ptest     xmm1,xmm1
       je        short M06_L00
M06_L01:
       pmovmskb  r8d,xmm1
       bsf       edx,r8d
       mov       r10d,20
       test      r8d,r8d
       cmove     edx,r10d
       sub       rax,rcx
       shr       rax,1
       add       eax,edx
       jmp       short M06_L04
M06_L02:
       add       r8d,0FFFFFFF8
       movsxd    rdx,r8d
       lea       rdx,[rcx+rdx*2]
       cmp       rax,rdx
       cmova     rax,rdx
       movups    xmm1,[rax]
       movups    xmm2,[rdx]
       packuswb  xmm1,xmm2
       pcmpeqb   xmm0,xmm1
       ptest     xmm0,xmm0
       jne       short M06_L05
M06_L03:
       mov       eax,0FFFFFFFF
M06_L04:
       ret
M06_L05:
       pmovmskb  r8d,xmm0
       test      r8d,r8d
       je        short M06_L06
       bsf       r8d,r8d
       cmp       r8d,8
       jl        short M06_L08
       jmp       short M06_L07
M06_L06:
       mov       r8d,20
M06_L07:
       mov       rax,rdx
       add       r8d,0FFFFFFF8
M06_L08:
       sub       rax,rcx
       shr       rax,1
       add       eax,r8d
       jmp       short M06_L04
M06_L09:
       xor       r10d,r10d
       cmp       r8d,4
       jl        short M06_L10
       add       r8d,0FFFFFFFC
       movsx     r10,word ptr [rcx]
       movsx     r9,dx
       cmp       r10d,r9d
       je        short M06_L13
       movsx     rax,word ptr [rcx+2]
       cmp       eax,r9d
       je        short M06_L14
       movsx     rax,word ptr [rcx+4]
       cmp       eax,r9d
       je        short M06_L15
       movsx     rax,word ptr [rcx+6]
       cmp       eax,r9d
       je        short M06_L16
       mov       r10d,4
M06_L10:
       test      r8d,r8d
       jle       short M06_L03
       movsx     r9,dx
M06_L11:
       dec       r8d
       movsx     rax,word ptr [rcx+r10*2]
       cmp       eax,r9d
       je        short M06_L12
       inc       r10
       test      r8d,r8d
       jg        short M06_L11
       jmp       near ptr M06_L03
M06_L12:
       mov       eax,r10d
       jmp       near ptr M06_L04
M06_L13:
       xor       eax,eax
       ret
M06_L14:
       mov       eax,1
       ret
M06_L15:
       mov       eax,2
       ret
M06_L16:
       mov       eax,3
       jmp       near ptr M06_L04
; Total bytes of code 351
```
```assembly
; System.SpanHelpers.NonPackedIndexOfValueType[[System.Int16, System.Private.CoreLib],[System.SpanHelpers+DontNegate`1[[System.Int16, System.Private.CoreLib]], System.Private.CoreLib]](Int16 ByRef, Int16, Int32)
       cmp       r8d,8
       jl        short M07_L03
       cmp       r8d,10
       jl        near ptr M07_L18
       vmovd     xmm0,edx
       vpbroadcastw ymm0,xmm0
       mov       rdx,rcx
       lea       eax,[r8-10]
       cdqe
       lea       rax,[rdx+rax*2]
       vpcmpeqw  ymm1,ymm0,[rcx]
       vptest    ymm1,ymm1
       jne       near ptr M07_L16
M07_L00:
       add       rdx,20
       cmp       rdx,rax
       ja        short M07_L01
       vpcmpeqw  ymm1,ymm0,[rdx]
       vptest    ymm1,ymm1
       jne       near ptr M07_L16
       jmp       short M07_L00
M07_L01:
       mov       edx,r8d
       test      dl,0F
       jne       near ptr M07_L17
M07_L02:
       mov       eax,0FFFFFFFF
       vzeroupper
       ret
M07_L03:
       xor       r10d,r10d
       cmp       r8d,8
       jl        near ptr M07_L05
M07_L04:
       add       r8d,0FFFFFFF8
       movsx     rax,word ptr [rcx+r10*2]
       movsx     r9,dx
       cmp       eax,r9d
       je        near ptr M07_L15
       movsx     rax,word ptr [rcx+r10*2+2]
       movsx     r9,dx
       cmp       eax,r9d
       je        near ptr M07_L14
       movsx     rax,word ptr [rcx+r10*2+4]
       movsx     r9,dx
       cmp       eax,r9d
       je        near ptr M07_L13
       movsx     rax,word ptr [rcx+r10*2+6]
       movsx     r9,dx
       cmp       eax,r9d
       je        near ptr M07_L12
       movsx     rax,word ptr [rcx+r10*2+8]
       movsx     r9,dx
       cmp       eax,r9d
       je        near ptr M07_L11
       movsx     rax,word ptr [rcx+r10*2+0A]
       movsx     r9,dx
       cmp       eax,r9d
       je        near ptr M07_L10
       movsx     rax,word ptr [rcx+r10*2+0C]
       movsx     r9,dx
       cmp       eax,r9d
       je        near ptr M07_L09
       movsx     rax,word ptr [rcx+r10*2+0E]
       movsx     r9,dx
       cmp       eax,r9d
       je        near ptr M07_L08
       add       r10,8
       cmp       r8d,8
       jge       near ptr M07_L04
M07_L05:
       cmp       r8d,4
       jl        short M07_L07
       add       r8d,0FFFFFFFC
       movsx     rax,word ptr [rcx+r10*2]
       movsx     r9,dx
       cmp       eax,r9d
       je        near ptr M07_L15
       movsx     rax,word ptr [rcx+r10*2+2]
       movsx     r9,dx
       cmp       eax,r9d
       je        near ptr M07_L14
       movsx     rax,word ptr [rcx+r10*2+4]
       movsx     r9,dx
       cmp       eax,r9d
       je        short M07_L13
       movsx     rax,word ptr [rcx+r10*2+6]
       movsx     r9,dx
       cmp       eax,r9d
       je        short M07_L12
       add       r10,4
       test      r8d,r8d
       jle       near ptr M07_L02
M07_L06:
       dec       r8d
       movsx     rax,word ptr [rcx+r10*2]
       movsx     r9,dx
       cmp       eax,r9d
       je        short M07_L15
       inc       r10
M07_L07:
       test      r8d,r8d
       jg        short M07_L06
       jmp       near ptr M07_L02
M07_L08:
       lea       eax,[r10+7]
       jmp       near ptr M07_L22
M07_L09:
       lea       eax,[r10+6]
       jmp       near ptr M07_L22
M07_L10:
       lea       eax,[r10+5]
       jmp       near ptr M07_L22
M07_L11:
       lea       eax,[r10+4]
       jmp       near ptr M07_L22
M07_L12:
       lea       eax,[r10+3]
       jmp       near ptr M07_L22
M07_L13:
       lea       eax,[r10+2]
       jmp       near ptr M07_L22
M07_L14:
       lea       eax,[r10+1]
       jmp       near ptr M07_L22
M07_L15:
       mov       eax,r10d
       jmp       near ptr M07_L22
M07_L16:
       mov       rax,rdx
       sub       rax,rcx
       shr       rax,1
       vpshufb   ymm0,ymm1,[7FF9778C1EE0]
       vpermq    ymm0,ymm0,0D8
       vpmovmskb ecx,xmm0
       tzcnt     ecx,ecx
       add       eax,ecx
       jmp       near ptr M07_L22
M07_L17:
       vpcmpeqw  ymm1,ymm0,[rax]
       vptest    ymm1,ymm1
       je        near ptr M07_L02
       sub       rax,rcx
       shr       rax,1
       vpshufb   ymm1,ymm1,[7FF9778C1EE0]
       vpermq    ymm0,ymm1,0D8
       vpmovmskb ecx,xmm0
       xor       r8d,r8d
       tzcnt     r8d,ecx
       add       eax,r8d
       jmp       near ptr M07_L22
M07_L18:
       vmovd     xmm0,edx
       vpbroadcastw xmm0,xmm0
       mov       rax,rcx
       lea       edx,[r8-8]
       movsxd    rdx,edx
       lea       rdx,[rax+rdx*2]
M07_L19:
       vpcmpeqw  xmm1,xmm0,[rax]
       vptest    xmm1,xmm1
       jne       short M07_L20
       add       rax,10
       cmp       rax,rdx
       jbe       short M07_L19
       jmp       short M07_L21
M07_L20:
       sub       rax,rcx
       shr       rax,1
       vpshufb   xmm0,xmm1,[7FF9778C1EE0]
       vpmovmskb r8d,xmm0
       xor       ecx,ecx
       tzcnt     ecx,r8d
       add       eax,ecx
       jmp       short M07_L22
M07_L21:
       mov       eax,r8d
       test      al,7
       je        near ptr M07_L02
       vpcmpeqw  xmm1,xmm0,[rdx]
       vptest    xmm1,xmm1
       je        near ptr M07_L02
       sub       rdx,rcx
       shr       rdx,1
       vpshufb   xmm1,xmm1,[7FF9778C1EE0]
       vpmovmskb ecx,xmm1
       xor       eax,eax
       tzcnt     eax,ecx
       add       eax,edx
M07_L22:
       vzeroupper
       ret
; Total bytes of code 700
```
```assembly
; System.SpanHelpers.SequenceEqual(Byte ByRef, Byte ByRef, UIntPtr)
       cmp       r8,8
       jb        short M08_L06
       cmp       rcx,rdx
       je        short M08_L04
       cmp       r8,10
       jae       short M08_L01
       add       r8,0FFFFFFFFFFFFFFF8
       mov       rax,[rcx]
       sub       rax,[rdx]
       mov       rcx,[rcx+r8]
       sub       rcx,[rdx+r8]
       or        rax,rcx
       sete      al
       movzx     eax,al
M08_L00:
       ret
M08_L01:
       xor       eax,eax
       add       r8,0FFFFFFFFFFFFFFF0
       je        short M08_L03
       movups    xmm0,[rcx]
       movups    xmm1,[rdx]
       pcmpeqb   xmm0,xmm1
       pmovmskb  r10d,xmm0
       cmp       r10d,0FFFF
       jne       short M08_L05
M08_L02:
       add       rax,10
       cmp       r8,rax
       ja        short M08_L10
M08_L03:
       movups    xmm0,[rcx+r8]
       movups    xmm1,[rdx+r8]
       pcmpeqb   xmm0,xmm1
       pmovmskb  eax,xmm0
       cmp       eax,0FFFF
       jne       short M08_L05
M08_L04:
       mov       eax,1
       ret
M08_L05:
       xor       eax,eax
       ret
M08_L06:
       cmp       r8,4
       jb        short M08_L07
       add       r8,0FFFFFFFFFFFFFFFC
       mov       eax,[rcx]
       sub       eax,[rdx]
       mov       ecx,[rcx+r8]
       sub       ecx,[rdx+r8]
       or        eax,ecx
       sete      al
       movzx     eax,al
       jmp       short M08_L00
M08_L07:
       xor       eax,eax
       mov       r10,r8
       and       r10,2
       je        short M08_L08
       movzx     eax,word ptr [rcx]
       movzx     r9d,word ptr [rdx]
       sub       eax,r9d
M08_L08:
       test      r8b,1
       je        short M08_L09
       movzx     ecx,byte ptr [rcx+r10]
       movzx     edx,byte ptr [rdx+r10]
       sub       ecx,edx
       or        eax,ecx
M08_L09:
       test      eax,eax
       sete      al
       movzx     eax,al
       jmp       near ptr M08_L00
M08_L10:
       movups    xmm0,[rcx+rax]
       movups    xmm1,[rdx+rax]
       pcmpeqb   xmm0,xmm1
       pmovmskb  r10d,xmm0
       cmp       r10d,0FFFF
       jne       short M08_L05
       jmp       near ptr M08_L02
; Total bytes of code 237
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
```assembly
; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBaseSlow(System.Runtime.CompilerServices.MethodTable*)
       push      rbx
       sub       rsp,30
       xor       eax,eax
       mov       [rsp+28],rax
       mov       rbx,rcx
       mov       rcx,rbx
       call      qword ptr [7FF96E854D70]; Precode of System.Runtime.CompilerServices.InitHelpers.InitClassSlow(System.Runtime.CompilerServices.MethodTable*)
       mov       rax,[rbx+20]
       mov       rax,[rax-10]
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
; AuroraBenchmark.SafeOpsBenchmarks.CheckedWrapper()
       sub       rsp,38
       xor       eax,eax
       mov       [rsp+28],rax
       mov       rax,[rcx+50]
       mov       rcx,[rcx+58]
       test      rax,rax
       je        short M00_L05
       mov       rdx,0BF18C00A00
       cmp       rax,[rdx]
       je        short M00_L01
       mov       rdx,0BF18C00A08
       cmp       rax,[rdx]
       je        short M00_L01
       movsx     rdx,cx
M00_L00:
       test      edx,edx
       je        near ptr M00_L09
M00_L01:
       test      rax,rax
       je        short M00_L02
       mov       rdx,offset MT_AuroraScript.Runtime.Types.ScriptInt32Array
       cmp       [rax],rdx
       jne       short M00_L02
       jmp       short M00_L04
M00_L02:
       mov       [rsp+28],rax
       mov       [rsp+30],rcx
       lea       rcx,[rsp+28]
       mov       edx,6
       call      qword ptr [7FF977C35B90]
       mov       rdx,rax
       test      rdx,rdx
       je        short M00_L03
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt32Array
       cmp       [rdx],rcx
       jne       short M00_L08
M00_L03:
       mov       rax,rdx
M00_L04:
       add       rsp,38
       ret
M00_L05:
       test      rcx,rcx
       je        short M00_L07
       lea       rdx,[rcx-1]
       mov       r8d,1
       mov       r10d,2
       cmp       rdx,1
       cmova     r8d,r10d
       mov       edx,r8d
M00_L06:
       jmp       short M00_L00
M00_L07:
       xor       edx,edx
       jmp       short M00_L06
M00_L08:
       mov       rdx,rax
       call      qword ptr [7FF977836328]; System.Runtime.CompilerServices.CastHelpers.ChkCastClass(Void*, System.Object)
       int       3
M00_L09:
       xor       eax,eax
       jmp       short M00_L04
; Total bytes of code 197
```
```assembly
; System.Runtime.CompilerServices.CastHelpers.ChkCastClass(Void*, System.Object)
       test      rdx,rdx
       je        short M01_L00
       cmp       [rdx],rcx
       jne       short M01_L01
M01_L00:
       mov       rax,rdx
       ret
M01_L01:
       jmp       qword ptr [7FF977A04D20]; System.Runtime.CompilerServices.CastHelpers.ChkCastClassSpecial(Void*, System.Object)
; Total bytes of code 20
```
```assembly
; System.Runtime.CompilerServices.CastHelpers.ChkCastClassSpecial(Void*, System.Object)
       mov       rax,[rdx]
       mov       rax,[rax+10]
       cmp       rax,rcx
       jne       short M02_L01
M02_L00:
       mov       rax,rdx
       ret
M02_L01:
       test      rax,rax
       je        short M02_L04
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M02_L00
       jmp       short M02_L03
M02_L02:
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M02_L00
       jmp       short M02_L01
M02_L03:
       test      rax,rax
       je        short M02_L04
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M02_L00
       test      rax,rax
       je        short M02_L04
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M02_L00
       test      rax,rax
       jne       short M02_L02
M02_L04:
       lea       rax,[System.Reflection.CustomAttributeExtensions.GetCustomAttribute[[System.__Canon, System.Private.CoreLib]](System.Reflection.Assembly)]
       jmp       qword ptr [rax]
; Total bytes of code 86
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
       je        short M03_L00
       mov       rcx,rsi
       cmp       [r8],rcx
       je        short M03_L00
       mov       rdx,rax
       call      qword ptr [7FF96E839090]; Precode of System.Runtime.CompilerServices.CastHelpers.ChkCastAny(Void*, System.Object)
       mov       r8,rax
M03_L00:
       mov       rax,r8
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
; Total bytes of code 88
```

## .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3 (Job: Job-BZNPUE(IterationCount=5, IterationTime=200ms, LaunchCount=1, WarmupCount=3))

```assembly
; AuroraBenchmark.SafeOpsBenchmarks.NullWrapper()
       push      rsi
       push      rbx
       sub       rsp,38
       xor       eax,eax
       mov       [rsp+28],rax
       mov       rbx,[rcx+60]
       mov       rsi,[rcx+68]
       test      rbx,rbx
       jne       short M00_L02
       test      rsi,rsi
       jne       short M00_L03
       xor       edx,edx
M00_L00:
       test      edx,edx
       jne       short M00_L04
       xor       eax,eax
M00_L01:
       add       rsp,38
       pop       rbx
       pop       rsi
       ret
M00_L02:
       mov       rdx,8001400A00
       cmp       rbx,[rdx]
       je        short M00_L04
       mov       rdx,8001400A08
       cmp       rbx,[rdx]
       je        short M00_L04
       movsx     rdx,si
       jmp       short M00_L00
M00_L03:
       lea       rdx,[rsi-1]
       mov       ecx,1
       mov       eax,2
       cmp       rdx,1
       mov       edx,eax
       cmovbe    edx,ecx
       jmp       short M00_L00
M00_L04:
       mov       rdx,rbx
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt32Array
       call      qword ptr [7FF977856850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rax,rax
       jne       short M00_L05
       mov       [rsp+28],rbx
       mov       [rsp+30],rsi
       lea       rcx,[rsp+28]
       mov       edx,6
       call      qword ptr [7FF977C55B78]
       mov       rdx,rax
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt32Array
       call      qword ptr [7FF977856328]; System.Runtime.CompilerServices.CastHelpers.ChkCastClass(Void*, System.Object)
       jmp       near ptr M00_L01
M00_L05:
       jmp       near ptr M00_L01
; Total bytes of code 186
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
; System.Runtime.CompilerServices.CastHelpers.ChkCastClass(Void*, System.Object)
       test      rdx,rdx
       je        short M02_L00
       cmp       [rdx],rcx
       jne       short M02_L01
M02_L00:
       mov       rax,rdx
       ret
M02_L01:
       jmp       qword ptr [7FF977A24D20]; System.Runtime.CompilerServices.CastHelpers.ChkCastClassSpecial(Void*, System.Object)
; Total bytes of code 20
```
```assembly
; System.Runtime.CompilerServices.CastHelpers.ChkCastClassSpecial(Void*, System.Object)
       mov       rax,[rdx]
       mov       rax,[rax+10]
       cmp       rax,rcx
       jne       short M03_L01
M03_L00:
       mov       rax,rdx
       ret
M03_L01:
       test      rax,rax
       je        short M03_L04
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M03_L00
       jmp       short M03_L03
M03_L02:
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M03_L00
       jmp       short M03_L01
M03_L03:
       test      rax,rax
       je        short M03_L04
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M03_L00
       test      rax,rax
       je        short M03_L04
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M03_L00
       test      rax,rax
       jne       short M03_L02
M03_L04:
       lea       rax,[System.Reflection.CustomAttributeExtensions.GetCustomAttribute[[System.__Canon, System.Private.CoreLib]](System.Reflection.Assembly)]
       jmp       qword ptr [rax]
; Total bytes of code 86
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
; AuroraBenchmark.SafeOpsBenchmarks.ArrayMiss()
       push      r14
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,80
       xor       eax,eax
       mov       [rsp+28],rax
       vxorps    xmm4,xmm4,xmm4
       vmovdqu   ymmword ptr [rsp+30],ymm4
       vmovdqu   ymmword ptr [rsp+50],ymm4
       vmovdqa   xmmword ptr [rsp+70],xmm4
M00_L00:
       mov       rdx,[rcx+10]
       mov       rbx,[rcx+18]
       mov       rsi,[rcx+70]
       mov       rdi,[rcx+78]
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rsp+50],xmm0
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rsp+60],xmm0
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rsp+70],xmm0
       test      rdx,rdx
       jne       near ptr M00_L14
       test      rbx,rbx
       je        near ptr M00_L15
       lea       rcx,[rbx-1]
       mov       eax,1
       mov       r8d,2
       cmp       rcx,1
       cmova     eax,r8d
M00_L01:
       cmp       eax,4
       je        near ptr M00_L16
M00_L02:
       mov       [rsp+40],rdx
       mov       [rsp+48],rbx
       cmp       qword ptr [rsp+40],0
       jne       near ptr M00_L27
       mov       rcx,[rsp+48]
       test      rcx,rcx
       je        near ptr M00_L30
       dec       rcx
       mov       eax,1
       mov       edx,2
       cmp       rcx,1
       cmova     eax,edx
M00_L03:
       cmp       eax,4
       jbe       near ptr M00_L29
       cmp       eax,1000
       je        near ptr M00_L51
       cmp       eax,2000
       je        near ptr M00_L52
       mov       rdx,[rsp+40]
       mov       rbx,rdx
       test      rbx,rbx
       je        short M00_L04
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptArray
       cmp       [rbx],rcx
       jne       near ptr M00_L31
M00_L04:
       test      rbx,rbx
       je        near ptr M00_L53
M00_L05:
       test      rbx,rbx
       je        short M00_L09
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptArray
       cmp       [rbx],rcx
       jne       short M00_L09
       mov       ebp,[rbx+34]
       xor       r14d,r14d
       test      ebp,ebp
       jle       short M00_L07
M00_L06:
       mov       rcx,[rbx+38]
       cmp       r14d,[rcx+8]
       jae       near ptr M00_L57
       mov       rdx,r14
       shl       rdx,4
       lea       rcx,[rcx+rdx+10]
       mov       [rsp+28],rsi
       mov       [rsp+30],rdi
       lea       rdx,[rsp+28]
       call      qword ptr [7FF977C25C38]; AuroraScript.Runtime.ScriptDatum.Equals(AuroraScript.Runtime.ScriptDatum)
       test      eax,eax
       jne       near ptr M00_L13
       inc       r14d
       cmp       r14d,ebp
       jl        short M00_L06
M00_L07:
       xor       eax,eax
M00_L08:
       add       rsp,80
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       ret
M00_L09:
       mov       rdx,rbx
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptPackedArray
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       je        short M00_L11
       mov       rcx,rbp
       mov       rax,[rbp]
       mov       rax,[rax+58]
       call      qword ptr [rax+8]
       mov       ebx,eax
       xor       r14d,r14d
       test      ebx,ebx
       jle       short M00_L07
       cmp       [rbp],ebp
       mov       rdx,[rbp]
M00_L10:
       lea       rdx,[rsp+60]
       mov       rcx,rbp
       mov       r8d,r14d
       mov       rax,[rbp]
       mov       rax,[rax+58]
       call      qword ptr [rax+10]
       mov       [rsp+28],rsi
       mov       [rsp+30],rdi
       lea       rdx,[rsp+28]
       lea       rcx,[rsp+60]
       call      qword ptr [7FF977C25C38]; AuroraScript.Runtime.ScriptDatum.Equals(AuroraScript.Runtime.ScriptDatum)
       test      eax,eax
       jne       short M00_L13
       inc       r14d
       cmp       r14d,ebx
       jl        short M00_L10
       jmp       near ptr M00_L07
M00_L11:
       mov       rcx,rbx
       mov       rax,[rbx]
       mov       rax,[rax+58]
       call      qword ptr [rax]
       mov       rbx,rax
       lea       rdx,[rsp+70]
       mov       rcx,rbx
       cmp       [rcx],ecx
       call      qword ptr [7FF977C25CB0]
       test      eax,eax
       je        near ptr M00_L07
M00_L12:
       mov       [rsp+28],rsi
       mov       [rsp+30],rdi
       lea       rdx,[rsp+28]
       lea       rcx,[rsp+70]
       call      qword ptr [7FF977C25C38]; AuroraScript.Runtime.ScriptDatum.Equals(AuroraScript.Runtime.ScriptDatum)
       test      eax,eax
       jne       short M00_L13
       lea       rdx,[rsp+70]
       mov       rcx,rbx
       cmp       [rcx],ecx
       call      qword ptr [7FF977C25CB0]
       test      eax,eax
       je        near ptr M00_L07
       jmp       short M00_L12
M00_L13:
       mov       eax,1
       jmp       near ptr M00_L08
M00_L14:
       mov       rax,94C4800A00
       cmp       rdx,[rax]
       je        near ptr M00_L02
       mov       rcx,94C4800A08
       cmp       rdx,[rcx]
       je        near ptr M00_L02
       movsx     rax,bx
       jmp       near ptr M00_L01
M00_L15:
       xor       eax,eax
       jmp       near ptr M00_L01
M00_L16:
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbx,rax
       mov       rdx,0B4F7020008
       test      rbx,rbx
       cmove     rbx,rdx
       mov       rbp,rbx
       test      rsi,rsi
       je        short M00_L17
       mov       rcx,94C4800A00
       cmp       rsi,[rcx]
       je        near ptr M00_L21
       mov       rdx,94C4800A08
       cmp       rsi,[rdx]
       je        near ptr M00_L21
       movsx     rcx,di
       jmp       short M00_L20
M00_L17:
       test      rdi,rdi
       je        short M00_L19
       lea       rdx,[rdi-1]
       mov       ecx,1
       mov       eax,2
       cmp       rdx,1
       cmova     ecx,eax
M00_L18:
       jmp       short M00_L20
M00_L19:
       xor       ecx,ecx
       jmp       short M00_L18
M00_L20:
       cmp       ecx,4
       jne       short M00_L21
       mov       rdx,rsi
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdx,rax
       mov       r9,0B4F7020008
       test      rdx,rdx
       cmove     rdx,r9
       cmp       dword ptr [rdx+8],0
       je        near ptr M00_L07
       mov       dword ptr [rsp+20],4
       mov       r9d,[rbx+8]
       mov       rcx,rbx
       xor       r8d,r8d
       call      qword ptr [7FF977C27CA8]
       not       eax
       shr       eax,1F
       jmp       near ptr M00_L08
M00_L21:
       xor       ebx,ebx
       jmp       near ptr M00_L26
M00_L22:
       movzx     r14d,word ptr [rbp+rbx*2+0C]
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,94C4801498
       mov       rcx,[rcx]
       cmp       [rcx+8],r14d
       jg        short M00_L23
       mov       ecx,1
       call      qword ptr [7FF977826670]; System.String.FastAllocateString(IntPtr)
       mov       [rax+0C],r14w
       jmp       short M00_L24
M00_L23:
       mov       ecx,r14d
       call      qword ptr [7FF977C27CC0]
       mov       rax,[rax+38]
M00_L24:
       test      rax,rax
       jne       short M00_L25
       mov       rax,0B4F7020008
M00_L25:
       mov       [rsp+50],rax
       mov       qword ptr [rsp+58],4
       mov       [rsp+28],rsi
       mov       [rsp+30],rdi
       lea       rdx,[rsp+28]
       lea       rcx,[rsp+50]
       call      qword ptr [7FF977C25C38]; AuroraScript.Runtime.ScriptDatum.Equals(AuroraScript.Runtime.ScriptDatum)
       test      eax,eax
       jne       near ptr M00_L13
       inc       ebx
M00_L26:
       cmp       [rbp+8],ebx
       jg        near ptr M00_L22
       jmp       near ptr M00_L07
M00_L27:
       mov       rax,[rsp+40]
       mov       rcx,94C4800A00
       cmp       rax,[rcx]
       jne       short M00_L28
       mov       eax,1000
       jmp       near ptr M00_L03
M00_L28:
       mov       rcx,[rsp+40]
       mov       rax,94C4800A08
       movsx     rdx,word ptr [rsp+48]
       mov       r8d,2000
       cmp       rcx,[rax]
       mov       eax,r8d
       cmovne    eax,edx
       jmp       near ptr M00_L03
M00_L29:
       mov       ecx,eax
       lea       rax,[7FF9778A1BD0]
       mov       eax,[rax+rcx*4]
       lea       rdx,[M00_L00]
       add       rax,rdx
       jmp       rax
M00_L30:
       xor       eax,eax
       jmp       near ptr M00_L03
M00_L31:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbx,rax
       jmp       near ptr M00_L04
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,94C4800A50
       mov       rbx,[rcx]
       jmp       near ptr M00_L05
       cmp       qword ptr [rsp+48],2
       je        short M00_L32
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BooleanValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,94C4801520
       mov       rbx,[rcx]
       jmp       short M00_L33
M00_L32:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BooleanValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,94C4801518
       mov       rbx,[rcx]
M00_L33:
       jmp       near ptr M00_L05
       mov       rcx,[rsp+48]
       mov       rax,8007FFFFFFFFFFFF
       add       rax,rcx
       cmp       rax,3
       jbe       short M00_L34
       vmovq     xmm1,rcx
       jmp       short M00_L36
M00_L34:
       cmp       rax,2
       jbe       short M00_L35
       vmovsd    xmm1,qword ptr [7FF9778A1BE8]
       jmp       short M00_L36
M00_L35:
       vmovq     xmm1,rax
M00_L36:
       vucomisd  xmm1,xmm1
       jp        short M00_L37
       je        short M00_L38
M00_L37:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,94C4801508
       mov       rbx,[rcx]
       jmp       near ptr M00_L50
M00_L38:
       vxorps    xmm0,xmm0,xmm0
       vucomisd  xmm1,xmm0
       jp        short M00_L39
       jne       short M00_L39
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,94C48014B8
       mov       rbx,[rcx]
       jmp       near ptr M00_L50
M00_L39:
       vucomisd  xmm1,qword ptr [7FF9778A1BF0]
       jp        short M00_L40
       jne       short M00_L40
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,94C48014C0
       mov       rbx,[rcx]
       jmp       near ptr M00_L50
M00_L40:
       vucomisd  xmm1,qword ptr [7FF9778A1BF8]
       jp        short M00_L41
       jne       short M00_L41
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,94C48014C8
       mov       rbx,[rcx]
       jmp       near ptr M00_L50
M00_L41:
       vucomisd  xmm1,qword ptr [7FF9778A1C00]
       jp        short M00_L42
       jne       short M00_L42
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,94C48014D0
       mov       rbx,[rcx]
       jmp       near ptr M00_L50
M00_L42:
       vucomisd  xmm1,qword ptr [7FF9778A1C08]
       jp        short M00_L43
       jne       short M00_L43
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,94C48014D8
       mov       rbx,[rcx]
       jmp       near ptr M00_L50
M00_L43:
       vucomisd  xmm1,qword ptr [7FF9778A1C10]
       jp        short M00_L44
       jne       short M00_L44
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,94C48014E0
       mov       rbx,[rcx]
       jmp       near ptr M00_L50
M00_L44:
       vucomisd  xmm1,qword ptr [7FF9778A1C18]
       jp        short M00_L45
       jne       short M00_L45
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,94C48014E8
       mov       rbx,[rcx]
       jmp       near ptr M00_L50
M00_L45:
       vucomisd  xmm1,qword ptr [7FF9778A1C20]
       jp        short M00_L46
       jne       short M00_L46
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,94C48014F0
       mov       rbx,[rcx]
       jmp       near ptr M00_L50
M00_L46:
       vucomisd  xmm1,qword ptr [7FF9778A1C28]
       jp        short M00_L47
       jne       short M00_L47
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,94C48014F8
       mov       rbx,[rcx]
       jmp       near ptr M00_L50
M00_L47:
       vucomisd  xmm1,qword ptr [7FF9778A1C30]
       jp        short M00_L48
       jne       short M00_L48
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,94C4801500
       mov       rbx,[rcx]
       jmp       short M00_L50
M00_L48:
       vmovsd    qword ptr [rsp+38],xmm1
       vucomisd  xmm1,qword ptr [7FF9778A1C38]
       jp        short M00_L49
       jne       short M00_L49
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,94C48014B0
       mov       rbx,[rcx]
       jmp       short M00_L50
M00_L49:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       rcx,rbx
       vmovsd    xmm1,qword ptr [rsp+38]
       call      qword ptr [7FF977C27CD8]
M00_L50:
       jmp       near ptr M00_L05
M00_L51:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.Int64Value
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       rcx,rbx
       mov       rdx,[rsp+48]
       call      qword ptr [7FF977C25D40]
       jmp       near ptr M00_L05
M00_L52:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.UInt64Value
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       rcx,rbx
       mov       rdx,[rsp+48]
       call      qword ptr [7FF977C25D70]
       jmp       near ptr M00_L05
       lea       rcx,[rsp+40]
       call      qword ptr [7FF977C25D88]
       mov       rbx,rax
       jmp       near ptr M00_L05
M00_L53:
       mov       rdx,[rsp+40]
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       short M00_L54
       xor       ebx,ebx
       jmp       near ptr M00_L05
M00_L54:
       cmp       dword ptr [rbp+8],0
       jne       short M00_L55
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,94C48014A0
       mov       rbx,[rcx]
       jmp       short M00_L56
M00_L55:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       rcx,rbx
       mov       rdx,rbp
       call      qword ptr [7FF977C27CF0]
M00_L56:
       jmp       near ptr M00_L05
M00_L57:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 2095
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
       jne       near ptr M01_L07
       mov       rdx,[rsp+48]
       test      rdx,rdx
       je        near ptr M01_L09
       dec       rdx
       mov       eax,1
       mov       ecx,2
       cmp       rdx,1
       cmova     eax,ecx
M01_L01:
       cmp       qword ptr [rsp+30],0
       jne       near ptr M01_L10
       mov       rdx,[rsp+38]
       test      rdx,rdx
       je        near ptr M01_L12
       dec       rdx
       mov       r8d,1
       mov       ecx,2
       cmp       rdx,1
       cmova     r8d,ecx
M01_L02:
       cmp       eax,r8d
       jne       near ptr M01_L21
       cmp       r8d,2
       jne       near ptr M01_L13
       mov       rax,[rsp+48]
       mov       rcx,8007FFFFFFFFFFFF
       add       rcx,rax
       cmp       rcx,3
       jbe       near ptr M01_L15
       vmovq     xmm0,rax
M01_L03:
       mov       rcx,[rsp+38]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       near ptr M01_L17
       vmovq     xmm1,rcx
M01_L04:
       vucomisd  xmm0,xmm1
       setnp     al
       jp        short M01_L05
       sete      al
M01_L05:
       movzx     eax,al
M01_L06:
       movzx     eax,al
       add       rsp,50
       pop       rbx
       ret
M01_L07:
       mov       rax,[rsp+40]
       mov       rdx,94C4800A00
       cmp       rax,[rdx]
       jne       short M01_L08
       mov       eax,1000
       jmp       near ptr M01_L01
M01_L08:
       mov       rdx,[rsp+40]
       mov       rcx,94C4800A08
       movsx     rax,word ptr [rsp+48]
       mov       r8d,2000
       cmp       rdx,[rcx]
       cmove     eax,r8d
       jmp       near ptr M01_L01
M01_L09:
       xor       eax,eax
       jmp       near ptr M01_L01
M01_L10:
       mov       r8,[rsp+30]
       mov       rdx,94C4800A00
       cmp       r8,[rdx]
       jne       short M01_L11
       mov       r8d,1000
       jmp       near ptr M01_L02
M01_L11:
       mov       rdx,[rsp+30]
       mov       rcx,94C4800A08
       movsx     r8,word ptr [rsp+38]
       mov       r10d,2000
       cmp       rdx,[rcx]
       cmove     r8d,r10d
       jmp       near ptr M01_L02
M01_L12:
       xor       r8d,r8d
       jmp       near ptr M01_L02
M01_L13:
       cmp       r8d,4
       ja        short M01_L14
       mov       edx,r8d
       lea       rcx,[7FF9778A1338]
       mov       ecx,[rcx+rdx*4]
       lea       rax,[M01_L00]
       add       rcx,rax
       jmp       rcx
M01_L14:
       cmp       r8d,1000
       je        near ptr M01_L19
       cmp       r8d,2000
       jne       near ptr M01_L20
       mov       rax,[rsp+48]
       cmp       rax,[rsp+38]
       sete      al
       movzx     eax,al
       jmp       near ptr M01_L06
       mov       eax,1
       jmp       near ptr M01_L06
       cmp       qword ptr [rsp+48],2
       sete      al
       movzx     eax,al
       cmp       qword ptr [rsp+38],2
       sete      cl
       movzx     ecx,cl
       cmp       eax,ecx
       sete      al
       movzx     eax,al
       jmp       near ptr M01_L06
M01_L15:
       cmp       rcx,2
       jbe       short M01_L16
       vmovsd    xmm0,qword ptr [7FF9778A1350]
       jmp       near ptr M01_L03
M01_L16:
       vmovq     xmm0,rcx
       jmp       near ptr M01_L03
M01_L17:
       cmp       rdx,2
       jbe       short M01_L18
       vmovsd    xmm1,qword ptr [7FF9778A1350]
       jmp       near ptr M01_L04
M01_L18:
       vmovq     xmm1,rdx
       jmp       near ptr M01_L04
M01_L19:
       mov       rax,[rsp+48]
       cmp       rax,[rsp+38]
       sete      al
       movzx     eax,al
       jmp       near ptr M01_L06
       mov       rdx,[rsp+40]
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbx,rax
       mov       rdx,[rsp+30]
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdx,rax
       mov       rcx,rbx
       call      qword ptr [7FF977826E08]; System.String.Equals(System.String, System.String)
       jmp       near ptr M01_L06
M01_L20:
       mov       rax,[rsp+40]
       cmp       rax,[rsp+30]
       sete      al
       movzx     eax,al
       jmp       near ptr M01_L06
M01_L21:
       cmp       eax,1000
       jne       short M01_L22
       cmp       r8d,2000
       jne       short M01_L22
       cmp       qword ptr [rsp+48],0
       jl        near ptr M01_L35
       mov       rax,[rsp+48]
       cmp       rax,[rsp+38]
       sete      al
       movzx     eax,al
       jmp       near ptr M01_L06
M01_L22:
       cmp       eax,2000
       jne       short M01_L23
       cmp       r8d,1000
       jne       short M01_L23
       cmp       qword ptr [rsp+38],0
       jl        near ptr M01_L35
       mov       rcx,[rsp+48]
       cmp       rcx,[rsp+38]
       sete      al
       movzx     eax,al
       jmp       near ptr M01_L06
M01_L23:
       cmp       qword ptr [rsp+40],0
       jne       short M01_L27
       cmp       qword ptr [rsp+48],2
       jbe       short M01_L27
       mov       rcx,[rsp+48]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M01_L24
       vmovq     xmm0,rcx
       jmp       short M01_L26
M01_L24:
       cmp       rdx,2
       jbe       short M01_L25
       vmovsd    xmm0,qword ptr [7FF9778A1350]
       jmp       short M01_L26
M01_L25:
       vmovq     xmm0,rdx
M01_L26:
       vmovsd    qword ptr [rsp+28],xmm0
       jmp       short M01_L28
M01_L27:
       lea       rcx,[rsp+40]
       lea       rdx,[rsp+28]
       call      qword ptr [7FF977C27C60]
       test      eax,eax
       je        near ptr M01_L35
M01_L28:
       cmp       qword ptr [rsp+30],0
       jne       short M01_L32
       cmp       qword ptr [rsp+38],2
       jbe       short M01_L32
       mov       rcx,[rsp+38]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M01_L29
       vmovq     xmm0,rcx
       jmp       short M01_L31
M01_L29:
       cmp       rdx,2
       jbe       short M01_L30
       vmovsd    xmm0,qword ptr [7FF9778A1350]
       jmp       short M01_L31
M01_L30:
       vmovq     xmm0,rdx
M01_L31:
       vmovsd    qword ptr [rsp+20],xmm0
       jmp       short M01_L33
M01_L32:
       lea       rcx,[rsp+30]
       lea       rdx,[rsp+20]
       call      qword ptr [7FF977C27C60]
       test      eax,eax
       je        short M01_L35
M01_L33:
       vmovsd    xmm0,qword ptr [rsp+28]
       vucomisd  xmm0,qword ptr [rsp+20]
       setnp     al
       jp        short M01_L34
       sete      al
M01_L34:
       movzx     eax,al
       jmp       near ptr M01_L06
M01_L35:
       xor       eax,eax
       add       rsp,50
       pop       rbx
       ret
; Total bytes of code 1013
```
```assembly
; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rdx,rdx
       je        short M02_L01
       mov       rax,[rdx]
       cmp       rax,rcx
       je        short M02_L01
       mov       rax,[rax+10]
M02_L00:
       cmp       rax,rcx
       jne       short M02_L02
M02_L01:
       mov       rax,rdx
       ret
M02_L02:
       test      rax,rax
       je        short M02_L03
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M02_L01
       test      rax,rax
       je        short M02_L03
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M02_L01
       test      rax,rax
       je        short M02_L03
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M02_L01
       test      rax,rax
       je        short M02_L03
       mov       rax,[rax+10]
       jmp       short M02_L00
M02_L03:
       xor       edx,edx
       jmp       short M02_L01
; Total bytes of code 83
```
```assembly
; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rax,[rcx+20]
       mov       rax,[rax-18]
       mov       rdx,rax
       test      dl,1
       jne       short M03_L00
       ret
M03_L00:
       jmp       qword ptr [7FF977825C38]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBaseSlow(System.Runtime.CompilerServices.MethodTable*)
; Total bytes of code 23
```
```assembly
; System.String.FastAllocateString(IntPtr)
       mov       rdx,rcx
       mov       rcx,offset MT_System.String
       jmp       near ptr 00007FF9D74C4E80
; Total bytes of code 18
```
```assembly
; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       push      rsi
       push      rbx
       sub       rsp,28
       mov       rbx,rcx
       mov       rcx,[rbx+20]
       mov       rsi,[rcx-10]
       mov       rcx,rsi
       test      cl,1
       jne       short M05_L00
       mov       rcx,7FF977C955BC
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rax,rsi
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
M05_L00:
       mov       rcx,7FF977C955B8
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rcx,rbx
       add       rsp,28
       pop       rbx
       pop       rsi
       jmp       qword ptr [7FF9779FE838]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBaseSlow(System.Runtime.CompilerServices.MethodTable*)
; Total bytes of code 80
```
```assembly
; System.String.Equals(System.String, System.String)
       sub       rsp,28
       cmp       rcx,rdx
       je        short M06_L02
       test      rcx,rcx
       je        short M06_L00
       test      rdx,rdx
       je        short M06_L00
       mov       r8d,[rcx+8]
       cmp       r8d,[rdx+8]
       je        short M06_L01
M06_L00:
       xor       eax,eax
       add       rsp,28
       ret
M06_L01:
       lea       rax,[rcx+0C]
       add       rdx,0C
       mov       r8d,[rcx+8]
       add       r8d,r8d
       mov       rcx,rax
       call      qword ptr [7FF96E84D900]; Precode of System.SpanHelpers.SequenceEqual(Byte ByRef, Byte ByRef, UIntPtr)
       nop
       add       rsp,28
       ret
M06_L02:
       mov       eax,1
       add       rsp,28
       ret
; Total bytes of code 76
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
```assembly
; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBaseSlow(System.Runtime.CompilerServices.MethodTable*)
       push      rbx
       sub       rsp,30
       xor       eax,eax
       mov       [rsp+28],rax
       mov       rbx,rcx
       mov       rcx,rbx
       call      qword ptr [7FF96E854D70]; Precode of System.Runtime.CompilerServices.InitHelpers.InitClassSlow(System.Runtime.CompilerServices.MethodTable*)
       mov       rax,[rbx+20]
       mov       rax,[rax-10]
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
; AuroraBenchmark.SafeOpsBenchmarks.PackedMiss()
       push      r15
       push      r14
       push      r13
       push      r12
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,88
       vxorps    xmm4,xmm4,xmm4
       vmovdqu   ymmword ptr [rsp+30],ymm4
       vmovdqu   ymmword ptr [rsp+50],ymm4
       vmovdqa   xmmword ptr [rsp+70],xmm4
       xor       eax,eax
       mov       [rsp+80],rax
M00_L00:
       mov       rdx,[rcx+20]
       mov       rbx,[rcx+28]
       mov       rsi,[rcx+70]
       mov       rdi,[rcx+78]
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rsp+58],xmm0
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rsp+68],xmm0
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rsp+78],xmm0
       test      rdx,rdx
       jne       near ptr M00_L20
       test      rbx,rbx
       je        near ptr M00_L21
       lea       rcx,[rbx-1]
       mov       eax,1
       mov       r8d,2
       cmp       rcx,1
       cmova     eax,r8d
M00_L01:
       cmp       eax,4
       je        near ptr M00_L22
M00_L02:
       mov       [rsp+48],rdx
       mov       [rsp+50],rbx
       cmp       qword ptr [rsp+48],0
       jne       near ptr M00_L33
       mov       rcx,[rsp+50]
       test      rcx,rcx
       je        near ptr M00_L36
       dec       rcx
       mov       eax,1
       mov       edx,2
       cmp       rcx,1
       cmova     eax,edx
M00_L03:
       cmp       eax,4
       jbe       near ptr M00_L35
       cmp       eax,1000
       je        near ptr M00_L57
       cmp       eax,2000
       je        near ptr M00_L58
       mov       rdx,[rsp+48]
       mov       rbx,rdx
       test      rbx,rbx
       je        short M00_L04
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt32Array
       cmp       [rbx],rcx
       jne       near ptr M00_L37
M00_L04:
       test      rbx,rbx
       je        near ptr M00_L59
M00_L05:
       test      rbx,rbx
       je        short M00_L09
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptArray
       cmp       [rbx],rcx
       jne       short M00_L09
       mov       ebp,[rbx+34]
       xor       r14d,r14d
       cmp       r14d,ebp
       jge       short M00_L07
M00_L06:
       mov       rcx,[rbx+38]
       cmp       r14d,[rcx+8]
       jae       near ptr M00_L69
       mov       rdx,r14
       shl       rdx,4
       lea       rcx,[rcx+rdx+10]
       mov       [rsp+30],rsi
       mov       [rsp+38],rdi
       lea       rdx,[rsp+30]
       call      qword ptr [7FF977C15C50]; AuroraScript.Runtime.ScriptDatum.Equals(AuroraScript.Runtime.ScriptDatum)
       test      eax,eax
       jne       near ptr M00_L19
       inc       r14d
       cmp       r14d,ebp
       jl        short M00_L06
M00_L07:
       xor       eax,eax
M00_L08:
       add       rsp,88
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       ret
M00_L09:
       mov       rbp,rbx
       test      rbp,rbp
       je        short M00_L10
       mov       rdx,offset MT_AuroraScript.Runtime.Types.ScriptInt32Array
       cmp       [rbp],rdx
       jne       near ptr M00_L63
M00_L10:
       test      rbp,rbp
       je        near ptr M00_L67
       mov       rbx,offset MT_AuroraScript.Runtime.Types.ScriptInt32Array
       cmp       [rbp],rbx
       jne       near ptr M00_L64
       mov       rcx,[rbp+38]
       mov       r14d,[rcx+8]
M00_L11:
       xor       r15d,r15d
       test      r14d,r14d
       jle       short M00_L07
       cmp       [rbp],rbx
       jne       near ptr M00_L18
       jmp       short M00_L14
M00_L12:
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2sd xmm0,xmm0,r12d
       vmovq     rax,xmm0
M00_L13:
       xor       edx,edx
       mov       [rsp+68],rdx
       mov       [rsp+70],rax
       mov       [rsp+30],rsi
       mov       [rsp+38],rdi
       lea       rdx,[rsp+30]
       lea       rcx,[rsp+68]
       call      qword ptr [7FF977C15C50]; AuroraScript.Runtime.ScriptDatum.Equals(AuroraScript.Runtime.ScriptDatum)
       test      eax,eax
       jne       near ptr M00_L19
       inc       r15d
       cmp       r15d,r14d
       jge       near ptr M00_L07
M00_L14:
       mov       r13,[rbp+38]
       cmp       r15d,[r13+8]
       jae       near ptr M00_L69
       mov       edx,r15d
       mov       r12d,[r13+rdx*4+10]
       test      r12d,r12d
       jne       short M00_L12
       mov       rax,7FF8000000000001
       jmp       short M00_L13
M00_L15:
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2sd xmm0,xmm0,r12d
       vmovq     rax,xmm0
M00_L16:
       xor       edx,edx
       mov       [rsp+68],rdx
       mov       [rsp+70],rax
M00_L17:
       mov       [rsp+30],rsi
       mov       [rsp+38],rdi
       lea       rdx,[rsp+30]
       lea       rcx,[rsp+68]
       call      qword ptr [7FF977C15C50]; AuroraScript.Runtime.ScriptDatum.Equals(AuroraScript.Runtime.ScriptDatum)
       test      eax,eax
       jne       short M00_L19
       inc       r15d
       cmp       r15d,r14d
       jge       near ptr M00_L07
M00_L18:
       cmp       [rbp],rbx
       jne       near ptr M00_L65
       mov       r13,[rbp+38]
       cmp       r15d,[r13+8]
       jae       near ptr M00_L69
       mov       edx,r15d
       mov       r12d,[r13+rdx*4+10]
       test      r12d,r12d
       je        near ptr M00_L66
       jmp       short M00_L15
M00_L19:
       mov       eax,1
       jmp       near ptr M00_L08
M00_L20:
       mov       rax,0E62D400A00
       cmp       rdx,[rax]
       je        near ptr M00_L02
       mov       rcx,0E62D400A08
       cmp       rdx,[rcx]
       je        near ptr M00_L02
       movsx     rax,bx
       jmp       near ptr M00_L01
M00_L21:
       xor       eax,eax
       jmp       near ptr M00_L01
M00_L22:
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbx,rax
       mov       rdx,1065FEF0008
       test      rbx,rbx
       cmove     rbx,rdx
       mov       rbp,rbx
       test      rsi,rsi
       je        short M00_L23
       mov       rcx,0E62D400A00
       cmp       rsi,[rcx]
       je        near ptr M00_L27
       mov       rdx,0E62D400A08
       cmp       rsi,[rdx]
       je        near ptr M00_L27
       movsx     rcx,di
       jmp       short M00_L26
M00_L23:
       test      rdi,rdi
       je        short M00_L25
       lea       rdx,[rdi-1]
       mov       ecx,1
       mov       eax,2
       cmp       rdx,1
       cmova     ecx,eax
M00_L24:
       jmp       short M00_L26
M00_L25:
       xor       ecx,ecx
       jmp       short M00_L24
M00_L26:
       cmp       ecx,4
       jne       short M00_L27
       mov       rdx,rsi
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdx,rax
       mov       r9,1065FEF0008
       test      rdx,rdx
       cmove     rdx,r9
       cmp       dword ptr [rdx+8],0
       je        near ptr M00_L07
       mov       dword ptr [rsp+20],4
       mov       r9d,[rbx+8]
       mov       rcx,rbx
       xor       r8d,r8d
       call      qword ptr [7FF977C17CD8]
       not       eax
       shr       eax,1F
       jmp       near ptr M00_L08
M00_L27:
       xor       ebx,ebx
       jmp       near ptr M00_L32
M00_L28:
       movzx     r14d,word ptr [rbp+rbx*2+0C]
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      qword ptr [7FF977815728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0E62D401498
       mov       rcx,[rcx]
       cmp       [rcx+8],r14d
       jg        short M00_L29
       mov       ecx,1
       call      qword ptr [7FF977816670]; System.String.FastAllocateString(IntPtr)
       mov       [rax+0C],r14w
       jmp       short M00_L30
M00_L29:
       mov       ecx,r14d
       call      qword ptr [7FF977C17CF0]
       mov       rax,[rax+38]
M00_L30:
       test      rax,rax
       jne       short M00_L31
       mov       rax,1065FEF0008
M00_L31:
       mov       [rsp+58],rax
       mov       qword ptr [rsp+60],4
       mov       [rsp+30],rsi
       mov       [rsp+38],rdi
       lea       rdx,[rsp+30]
       lea       rcx,[rsp+58]
       call      qword ptr [7FF977C15C50]; AuroraScript.Runtime.ScriptDatum.Equals(AuroraScript.Runtime.ScriptDatum)
       test      eax,eax
       jne       near ptr M00_L19
       inc       ebx
M00_L32:
       cmp       [rbp+8],ebx
       jg        near ptr M00_L28
       jmp       near ptr M00_L07
M00_L33:
       mov       rax,[rsp+48]
       mov       rcx,0E62D400A00
       cmp       rax,[rcx]
       jne       short M00_L34
       mov       eax,1000
       jmp       near ptr M00_L03
M00_L34:
       mov       rcx,[rsp+48]
       mov       rax,0E62D400A08
       movsx     rdx,word ptr [rsp+50]
       mov       r8d,2000
       cmp       rcx,[rax]
       mov       eax,r8d
       cmovne    eax,edx
       jmp       near ptr M00_L03
M00_L35:
       mov       ecx,eax
       lea       rax,[7FF977892040]
       mov       eax,[rax+rcx*4]
       lea       rdx,[M00_L00]
       add       rax,rdx
       jmp       rax
M00_L36:
       xor       eax,eax
       jmp       near ptr M00_L03
M00_L37:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbx,rax
       jmp       near ptr M00_L04
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       call      qword ptr [7FF977815728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0E62D400A50
       mov       rbx,[rcx]
       jmp       near ptr M00_L05
       cmp       qword ptr [rsp+50],2
       je        short M00_L38
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BooleanValue
       call      qword ptr [7FF977815740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0E62D401520
       mov       rbx,[rcx]
       jmp       short M00_L39
M00_L38:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BooleanValue
       call      qword ptr [7FF977815740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0E62D401518
       mov       rbx,[rcx]
M00_L39:
       jmp       near ptr M00_L05
       mov       rcx,[rsp+50]
       mov       rax,8007FFFFFFFFFFFF
       add       rax,rcx
       cmp       rax,3
       jbe       short M00_L40
       vmovq     xmm1,rcx
       jmp       short M00_L42
M00_L40:
       cmp       rax,2
       jbe       short M00_L41
       vmovsd    xmm1,qword ptr [7FF977892058]
       jmp       short M00_L42
M00_L41:
       vmovq     xmm1,rax
M00_L42:
       vucomisd  xmm1,xmm1
       jp        short M00_L43
       je        short M00_L44
M00_L43:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977815740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0E62D401508
       mov       rbx,[rcx]
       jmp       near ptr M00_L56
M00_L44:
       vxorps    xmm0,xmm0,xmm0
       vucomisd  xmm1,xmm0
       jp        short M00_L45
       jne       short M00_L45
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977815740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0E62D4014B8
       mov       rbx,[rcx]
       jmp       near ptr M00_L56
M00_L45:
       vucomisd  xmm1,qword ptr [7FF977892060]
       jp        short M00_L46
       jne       short M00_L46
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977815740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0E62D4014C0
       mov       rbx,[rcx]
       jmp       near ptr M00_L56
M00_L46:
       vucomisd  xmm1,qword ptr [7FF977892068]
       jp        short M00_L47
       jne       short M00_L47
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977815740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0E62D4014C8
       mov       rbx,[rcx]
       jmp       near ptr M00_L56
M00_L47:
       vucomisd  xmm1,qword ptr [7FF977892070]
       jp        short M00_L48
       jne       short M00_L48
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977815740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0E62D4014D0
       mov       rbx,[rcx]
       jmp       near ptr M00_L56
M00_L48:
       vucomisd  xmm1,qword ptr [7FF977892078]
       jp        short M00_L49
       jne       short M00_L49
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977815740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0E62D4014D8
       mov       rbx,[rcx]
       jmp       near ptr M00_L56
M00_L49:
       vucomisd  xmm1,qword ptr [7FF977892080]
       jp        short M00_L50
       jne       short M00_L50
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977815740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0E62D4014E0
       mov       rbx,[rcx]
       jmp       near ptr M00_L56
M00_L50:
       vucomisd  xmm1,qword ptr [7FF977892088]
       jp        short M00_L51
       jne       short M00_L51
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977815740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0E62D4014E8
       mov       rbx,[rcx]
       jmp       near ptr M00_L56
M00_L51:
       vucomisd  xmm1,qword ptr [7FF977892090]
       jp        short M00_L52
       jne       short M00_L52
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977815740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0E62D4014F0
       mov       rbx,[rcx]
       jmp       near ptr M00_L56
M00_L52:
       vucomisd  xmm1,qword ptr [7FF977892098]
       jp        short M00_L53
       jne       short M00_L53
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977815740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0E62D4014F8
       mov       rbx,[rcx]
       jmp       near ptr M00_L56
M00_L53:
       vucomisd  xmm1,qword ptr [7FF9778920A0]
       jp        short M00_L54
       jne       short M00_L54
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977815740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0E62D401500
       mov       rbx,[rcx]
       jmp       short M00_L56
M00_L54:
       vmovsd    qword ptr [rsp+40],xmm1
       vucomisd  xmm1,qword ptr [7FF9778920A8]
       jp        short M00_L55
       jne       short M00_L55
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977815740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0E62D4014B0
       mov       rbx,[rcx]
       jmp       short M00_L56
M00_L55:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       rcx,rbx
       vmovsd    xmm1,qword ptr [rsp+40]
       call      qword ptr [7FF977C17D08]
M00_L56:
       jmp       near ptr M00_L05
M00_L57:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.Int64Value
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       rcx,rbx
       mov       rdx,[rsp+50]
       call      qword ptr [7FF977C15D58]
       jmp       near ptr M00_L05
M00_L58:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.UInt64Value
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       rcx,rbx
       mov       rdx,[rsp+50]
       call      qword ptr [7FF977C15D88]
       jmp       near ptr M00_L05
       lea       rcx,[rsp+48]
       call      qword ptr [7FF977C15DA0]
       mov       rbx,rax
       jmp       near ptr M00_L05
M00_L59:
       mov       rdx,[rsp+48]
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       short M00_L60
       xor       ebx,ebx
       jmp       near ptr M00_L05
M00_L60:
       cmp       dword ptr [rbp+8],0
       jne       short M00_L61
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      qword ptr [7FF977815728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0E62D4014A0
       mov       rbx,[rcx]
       jmp       short M00_L62
M00_L61:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       rcx,rbx
       mov       rdx,rbp
       call      qword ptr [7FF977C17D20]
M00_L62:
       jmp       near ptr M00_L05
M00_L63:
       mov       rdx,rbx
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptPackedArray
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       jmp       near ptr M00_L10
M00_L64:
       mov       rcx,rbp
       mov       rax,[rbp]
       mov       rax,[rax+58]
       call      qword ptr [rax+8]
       mov       r14d,eax
       jmp       near ptr M00_L11
M00_L65:
       lea       rdx,[rsp+68]
       mov       rcx,rbp
       mov       r8d,r15d
       mov       rax,[rbp]
       mov       rax,[rax+58]
       call      qword ptr [rax+10]
       jmp       near ptr M00_L17
M00_L66:
       mov       rax,7FF8000000000001
       jmp       near ptr M00_L16
M00_L67:
       mov       rcx,rbx
       mov       rax,[rbx]
       mov       rax,[rax+58]
       call      qword ptr [rax]
       mov       rbx,rax
M00_L68:
       lea       rdx,[rsp+78]
       mov       rcx,rbx
       cmp       [rcx],ecx
       call      qword ptr [7FF977C15CC8]
       test      eax,eax
       je        near ptr M00_L07
       mov       [rsp+30],rsi
       mov       [rsp+38],rdi
       lea       rdx,[rsp+30]
       lea       rcx,[rsp+78]
       call      qword ptr [7FF977C15C50]; AuroraScript.Runtime.ScriptDatum.Equals(AuroraScript.Runtime.ScriptDatum)
       test      eax,eax
       je        short M00_L68
       jmp       near ptr M00_L19
M00_L69:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 2366
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
       jne       near ptr M01_L07
       mov       rdx,[rsp+48]
       test      rdx,rdx
       je        near ptr M01_L09
       dec       rdx
       mov       eax,1
       mov       ecx,2
       cmp       rdx,1
       cmova     eax,ecx
M01_L01:
       cmp       qword ptr [rsp+30],0
       jne       near ptr M01_L10
       mov       rdx,[rsp+38]
       test      rdx,rdx
       je        near ptr M01_L12
       dec       rdx
       mov       r8d,1
       mov       ecx,2
       cmp       rdx,1
       cmova     r8d,ecx
M01_L02:
       cmp       eax,r8d
       jne       near ptr M01_L21
       cmp       r8d,2
       jne       near ptr M01_L13
       mov       rax,[rsp+48]
       mov       rcx,8007FFFFFFFFFFFF
       add       rcx,rax
       cmp       rcx,3
       jbe       near ptr M01_L15
       vmovq     xmm0,rax
M01_L03:
       mov       rcx,[rsp+38]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       near ptr M01_L17
       vmovq     xmm1,rcx
M01_L04:
       vucomisd  xmm0,xmm1
       setnp     al
       jp        short M01_L05
       sete      al
M01_L05:
       movzx     eax,al
M01_L06:
       movzx     eax,al
       add       rsp,50
       pop       rbx
       ret
M01_L07:
       mov       rax,[rsp+40]
       mov       rdx,0E62D400A00
       cmp       rax,[rdx]
       jne       short M01_L08
       mov       eax,1000
       jmp       near ptr M01_L01
M01_L08:
       mov       rdx,[rsp+40]
       mov       rcx,0E62D400A08
       movsx     rax,word ptr [rsp+48]
       mov       r8d,2000
       cmp       rdx,[rcx]
       cmove     eax,r8d
       jmp       near ptr M01_L01
M01_L09:
       xor       eax,eax
       jmp       near ptr M01_L01
M01_L10:
       mov       r8,[rsp+30]
       mov       rdx,0E62D400A00
       cmp       r8,[rdx]
       jne       short M01_L11
       mov       r8d,1000
       jmp       near ptr M01_L02
M01_L11:
       mov       rdx,[rsp+30]
       mov       rcx,0E62D400A08
       movsx     r8,word ptr [rsp+38]
       mov       r10d,2000
       cmp       rdx,[rcx]
       cmove     r8d,r10d
       jmp       near ptr M01_L02
M01_L12:
       xor       r8d,r8d
       jmp       near ptr M01_L02
M01_L13:
       cmp       r8d,4
       ja        short M01_L14
       mov       edx,r8d
       lea       rcx,[7FF977891698]
       mov       ecx,[rcx+rdx*4]
       lea       rax,[M01_L00]
       add       rcx,rax
       jmp       rcx
M01_L14:
       cmp       r8d,1000
       je        near ptr M01_L19
       cmp       r8d,2000
       jne       near ptr M01_L20
       mov       rax,[rsp+48]
       cmp       rax,[rsp+38]
       sete      al
       movzx     eax,al
       jmp       near ptr M01_L06
       mov       eax,1
       jmp       near ptr M01_L06
       cmp       qword ptr [rsp+48],2
       sete      al
       movzx     eax,al
       cmp       qword ptr [rsp+38],2
       sete      cl
       movzx     ecx,cl
       cmp       eax,ecx
       sete      al
       movzx     eax,al
       jmp       near ptr M01_L06
M01_L15:
       cmp       rcx,2
       jbe       short M01_L16
       vmovsd    xmm0,qword ptr [7FF9778916B0]
       jmp       near ptr M01_L03
M01_L16:
       vmovq     xmm0,rcx
       jmp       near ptr M01_L03
M01_L17:
       cmp       rdx,2
       jbe       short M01_L18
       vmovsd    xmm1,qword ptr [7FF9778916B0]
       jmp       near ptr M01_L04
M01_L18:
       vmovq     xmm1,rdx
       jmp       near ptr M01_L04
M01_L19:
       mov       rax,[rsp+48]
       cmp       rax,[rsp+38]
       sete      al
       movzx     eax,al
       jmp       near ptr M01_L06
       mov       rdx,[rsp+40]
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbx,rax
       mov       rdx,[rsp+30]
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdx,rax
       mov       rcx,rbx
       call      qword ptr [7FF977816E08]; System.String.Equals(System.String, System.String)
       jmp       near ptr M01_L06
M01_L20:
       mov       rax,[rsp+40]
       cmp       rax,[rsp+30]
       sete      al
       movzx     eax,al
       jmp       near ptr M01_L06
M01_L21:
       cmp       eax,1000
       jne       short M01_L22
       cmp       r8d,2000
       jne       short M01_L22
       cmp       qword ptr [rsp+48],0
       jl        near ptr M01_L35
       mov       rax,[rsp+48]
       cmp       rax,[rsp+38]
       sete      al
       movzx     eax,al
       jmp       near ptr M01_L06
M01_L22:
       cmp       eax,2000
       jne       short M01_L23
       cmp       r8d,1000
       jne       short M01_L23
       cmp       qword ptr [rsp+38],0
       jl        near ptr M01_L35
       mov       rcx,[rsp+48]
       cmp       rcx,[rsp+38]
       sete      al
       movzx     eax,al
       jmp       near ptr M01_L06
M01_L23:
       cmp       qword ptr [rsp+40],0
       jne       short M01_L27
       cmp       qword ptr [rsp+48],2
       jbe       short M01_L27
       mov       rcx,[rsp+48]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M01_L24
       vmovq     xmm0,rcx
       jmp       short M01_L26
M01_L24:
       cmp       rdx,2
       jbe       short M01_L25
       vmovsd    xmm0,qword ptr [7FF9778916B0]
       jmp       short M01_L26
M01_L25:
       vmovq     xmm0,rdx
M01_L26:
       vmovsd    qword ptr [rsp+28],xmm0
       jmp       short M01_L28
M01_L27:
       lea       rcx,[rsp+40]
       lea       rdx,[rsp+28]
       call      qword ptr [7FF977C17C90]
       test      eax,eax
       je        near ptr M01_L35
M01_L28:
       cmp       qword ptr [rsp+30],0
       jne       short M01_L32
       cmp       qword ptr [rsp+38],2
       jbe       short M01_L32
       mov       rcx,[rsp+38]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M01_L29
       vmovq     xmm0,rcx
       jmp       short M01_L31
M01_L29:
       cmp       rdx,2
       jbe       short M01_L30
       vmovsd    xmm0,qword ptr [7FF9778916B0]
       jmp       short M01_L31
M01_L30:
       vmovq     xmm0,rdx
M01_L31:
       vmovsd    qword ptr [rsp+20],xmm0
       jmp       short M01_L33
M01_L32:
       lea       rcx,[rsp+30]
       lea       rdx,[rsp+20]
       call      qword ptr [7FF977C17C90]
       test      eax,eax
       je        short M01_L35
M01_L33:
       vmovsd    xmm0,qword ptr [rsp+28]
       vucomisd  xmm0,qword ptr [rsp+20]
       setnp     al
       jp        short M01_L34
       sete      al
M01_L34:
       movzx     eax,al
       jmp       near ptr M01_L06
M01_L35:
       xor       eax,eax
       add       rsp,50
       pop       rbx
       ret
; Total bytes of code 1013
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
       jmp       short M02_L00
; Total bytes of code 86
```
```assembly
; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rax,[rcx+20]
       mov       rax,[rax-18]
       mov       rdx,rax
       test      dl,1
       jne       short M03_L00
       ret
M03_L00:
       jmp       qword ptr [7FF977815C38]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBaseSlow(System.Runtime.CompilerServices.MethodTable*)
; Total bytes of code 23
```
```assembly
; System.String.FastAllocateString(IntPtr)
       mov       rdx,rcx
       mov       rcx,offset MT_System.String
       jmp       near ptr 00007FF9D74C4E80
; Total bytes of code 18
```
```assembly
; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       push      rsi
       push      rbx
       sub       rsp,28
       mov       rbx,rcx
       mov       rcx,[rbx+20]
       mov       rsi,[rcx-10]
       mov       rcx,rsi
       test      cl,1
       jne       short M05_L00
       mov       rcx,7FF977C86104
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rax,rsi
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
M05_L00:
       mov       rcx,7FF977C86100
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rcx,rbx
       add       rsp,28
       pop       rbx
       pop       rsi
       jmp       qword ptr [7FF9779EE838]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBaseSlow(System.Runtime.CompilerServices.MethodTable*)
; Total bytes of code 80
```
```assembly
; System.String.Equals(System.String, System.String)
       sub       rsp,28
       cmp       rcx,rdx
       je        short M06_L02
       test      rcx,rcx
       je        short M06_L00
       test      rdx,rdx
       je        short M06_L00
       mov       r8d,[rcx+8]
       cmp       r8d,[rdx+8]
       je        short M06_L01
M06_L00:
       xor       eax,eax
       add       rsp,28
       ret
M06_L01:
       lea       rax,[rcx+0C]
       add       rdx,0C
       mov       r8d,[rcx+8]
       add       r8d,r8d
       mov       rcx,rax
       call      qword ptr [7FF96E84D900]; Precode of System.SpanHelpers.SequenceEqual(Byte ByRef, Byte ByRef, UIntPtr)
       nop
       add       rsp,28
       ret
M06_L02:
       mov       eax,1
       add       rsp,28
       ret
; Total bytes of code 76
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
```assembly
; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBaseSlow(System.Runtime.CompilerServices.MethodTable*)
       push      rbx
       sub       rsp,30
       xor       eax,eax
       mov       [rsp+28],rax
       mov       rbx,rcx
       mov       rcx,rbx
       call      qword ptr [7FF96E854D70]; Precode of System.Runtime.CompilerServices.InitHelpers.InitClassSlow(System.Runtime.CompilerServices.MethodTable*)
       mov       rax,[rbx+20]
       mov       rax,[rax-10]
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
; AuroraBenchmark.SafeOpsBenchmarks.CharacterMiss()
       push      r14
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,80
       xor       eax,eax
       mov       [rsp+28],rax
       vxorps    xmm4,xmm4,xmm4
       vmovdqu   ymmword ptr [rsp+30],ymm4
       vmovdqu   ymmword ptr [rsp+50],ymm4
       vmovdqa   xmmword ptr [rsp+70],xmm4
M00_L00:
       mov       rbx,[rcx+30]
       mov       rsi,[rcx+38]
       mov       rdi,[rcx+40]
       mov       rbp,[rcx+48]
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rsp+50],xmm0
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rsp+60],xmm0
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rsp+70],xmm0
       test      rbx,rbx
       je        near ptr M00_L08
       mov       rcx,0B855000A00
       cmp       rbx,[rcx]
       je        near ptr M00_L20
       mov       rcx,0B855000A08
       cmp       rbx,[rcx]
       je        near ptr M00_L20
       movsx     rcx,si
M00_L01:
       cmp       ecx,4
       jne       near ptr M00_L20
       test      rbx,rbx
       je        short M00_L02
       mov       rcx,offset MT_System.String
       cmp       [rbx],rcx
       jne       short M00_L02
       jmp       short M00_L03
M00_L02:
       xor       ebx,ebx
M00_L03:
       mov       rcx,0B84A2F0008
       test      rbx,rbx
       cmove     rbx,rcx
       mov       rsi,rbx
       test      rdi,rdi
       je        near ptr M00_L11
       mov       rcx,0B855000A00
       cmp       rdi,[rcx]
       je        near ptr M00_L14
       mov       rcx,0B855000A08
       cmp       rdi,[rcx]
       je        near ptr M00_L14
       movsx     rcx,bp
M00_L04:
       cmp       ecx,4
       jne       near ptr M00_L14
       test      rdi,rdi
       je        short M00_L05
       mov       rcx,offset MT_System.String
       cmp       [rdi],rcx
       jne       short M00_L05
       jmp       short M00_L06
M00_L05:
       xor       edi,edi
M00_L06:
       mov       rcx,0B84A2F0008
       test      rdi,rdi
       cmove     rdi,rcx
       mov       r9d,[rdi+8]
       test      r9d,r9d
       je        near ptr M00_L54
       mov       edx,[rbx+8]
       lea       rcx,[rbx+0C]
       lea       r8,[rdi+0C]
       call      qword ptr [7FF977C15CF8]; System.SpanHelpers.IndexOf(Char ByRef, Int32, Char ByRef, Int32)
       not       eax
       shr       eax,1F
M00_L07:
       add       rsp,80
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       ret
M00_L08:
       test      rsi,rsi
       je        short M00_L10
       lea       rcx,[rsi-1]
       mov       edx,1
       mov       r8d,2
       cmp       rcx,1
       cmova     edx,r8d
       mov       ecx,edx
M00_L09:
       jmp       near ptr M00_L01
M00_L10:
       xor       ecx,ecx
       jmp       short M00_L09
M00_L11:
       test      rbp,rbp
       je        short M00_L13
       lea       rcx,[rbp-1]
       mov       edx,1
       mov       r8d,2
       cmp       rcx,1
       cmova     edx,r8d
       mov       ecx,edx
M00_L12:
       jmp       near ptr M00_L04
M00_L13:
       xor       ecx,ecx
       jmp       short M00_L12
M00_L14:
       xor       ebx,ebx
       jmp       near ptr M00_L19
M00_L15:
       movzx     r14d,word ptr [rsi+rbx*2+0C]
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      qword ptr [7FF977815728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0B8550014D0
       mov       rcx,[rcx]
       cmp       [rcx+8],r14d
       jg        short M00_L16
       mov       ecx,1
       call      qword ptr [7FF977816670]; System.String.FastAllocateString(IntPtr)
       mov       [rax+0C],r14w
       jmp       short M00_L17
M00_L16:
       mov       ecx,r14d
       call      qword ptr [7FF977C1C720]
       mov       rax,[rax+38]
M00_L17:
       test      rax,rax
       jne       short M00_L18
       mov       rax,0B84A2F0008
M00_L18:
       mov       [rsp+50],rax
       mov       qword ptr [rsp+58],4
       mov       [rsp+28],rdi
       mov       [rsp+30],rbp
       lea       rdx,[rsp+28]
       lea       rcx,[rsp+50]
       call      qword ptr [7FF977C15C38]
       test      eax,eax
       jne       near ptr M00_L57
       inc       ebx
M00_L19:
       mov       edx,[rsi+8]
       cmp       edx,ebx
       jg        near ptr M00_L15
       jmp       near ptr M00_L54
M00_L20:
       mov       [rsp+40],rbx
       mov       [rsp+48],rsi
       cmp       qword ptr [rsp+40],0
       je        short M00_L23
       mov       rcx,[rsp+40]
       mov       rax,0B855000A00
       cmp       rcx,[rax]
       jne       short M00_L21
       mov       edx,1000
       jmp       short M00_L22
M00_L21:
       mov       rcx,[rsp+40]
       mov       rax,0B855000A08
       movsx     rdx,word ptr [rsp+48]
       mov       r8d,2000
       cmp       rcx,[rax]
       cmove     edx,r8d
M00_L22:
       cmp       edx,4
       ja        short M00_L26
       mov       ecx,edx
       lea       rax,[7FF9778926C0]
       mov       eax,[rax+rcx*4]
       lea       rdx,[M00_L00]
       add       rax,rdx
       jmp       rax
M00_L23:
       mov       rdx,[rsp+48]
       test      rdx,rdx
       je        short M00_L25
       dec       rdx
       mov       ecx,1
       mov       eax,2
       cmp       rdx,1
       cmova     ecx,eax
       mov       edx,ecx
M00_L24:
       jmp       short M00_L22
M00_L25:
       xor       edx,edx
       jmp       short M00_L24
M00_L26:
       cmp       edx,1000
       je        near ptr M00_L46
       cmp       edx,2000
       jne       near ptr M00_L47
       mov       rcx,offset MT_AuroraScript.Runtime.Types.UInt64Value
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       rcx,rbx
       mov       rdx,[rsp+48]
       call      qword ptr [7FF977C1C738]
       jmp       near ptr M00_L48
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       call      qword ptr [7FF977815728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0B855000A50
       mov       rbx,[rcx]
       jmp       near ptr M00_L48
       cmp       qword ptr [rsp+48],2
       je        short M00_L27
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BooleanValue
       call      qword ptr [7FF977815740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0B855001558
       mov       rbx,[rcx]
       jmp       short M00_L28
M00_L27:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BooleanValue
       call      qword ptr [7FF977815740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0B855001550
       mov       rbx,[rcx]
M00_L28:
       jmp       near ptr M00_L48
       mov       rcx,[rsp+48]
       mov       rax,8007FFFFFFFFFFFF
       add       rax,rcx
       cmp       rax,3
       jbe       short M00_L29
       vmovq     xmm1,rcx
       jmp       short M00_L31
M00_L29:
       cmp       rax,2
       jbe       short M00_L30
       vmovsd    xmm1,qword ptr [7FF9778926D8]
       jmp       short M00_L31
M00_L30:
       vmovq     xmm1,rax
M00_L31:
       vucomisd  xmm1,xmm1
       jp        short M00_L32
       je        short M00_L33
M00_L32:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977815740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0B855001540
       mov       rbx,[rcx]
       jmp       near ptr M00_L45
M00_L33:
       vxorps    xmm0,xmm0,xmm0
       vucomisd  xmm1,xmm0
       jp        short M00_L34
       jne       short M00_L34
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977815740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0B8550014F0
       mov       rbx,[rcx]
       jmp       near ptr M00_L45
M00_L34:
       vucomisd  xmm1,qword ptr [7FF9778926E0]
       jp        short M00_L35
       jne       short M00_L35
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977815740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0B8550014F8
       mov       rbx,[rcx]
       jmp       near ptr M00_L45
M00_L35:
       vucomisd  xmm1,qword ptr [7FF9778926E8]
       jp        short M00_L36
       jne       short M00_L36
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977815740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0B855001500
       mov       rbx,[rcx]
       jmp       near ptr M00_L45
M00_L36:
       vucomisd  xmm1,qword ptr [7FF9778926F0]
       jp        short M00_L37
       jne       short M00_L37
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977815740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0B855001508
       mov       rbx,[rcx]
       jmp       near ptr M00_L45
M00_L37:
       vucomisd  xmm1,qword ptr [7FF9778926F8]
       jp        short M00_L38
       jne       short M00_L38
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977815740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0B855001510
       mov       rbx,[rcx]
       jmp       near ptr M00_L45
M00_L38:
       vucomisd  xmm1,qword ptr [7FF977892700]
       jp        short M00_L39
       jne       short M00_L39
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977815740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0B855001518
       mov       rbx,[rcx]
       jmp       near ptr M00_L45
M00_L39:
       vucomisd  xmm1,qword ptr [7FF977892708]
       jp        short M00_L40
       jne       short M00_L40
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977815740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0B855001520
       mov       rbx,[rcx]
       jmp       near ptr M00_L45
M00_L40:
       vucomisd  xmm1,qword ptr [7FF977892710]
       jp        short M00_L41
       jne       short M00_L41
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977815740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0B855001528
       mov       rbx,[rcx]
       jmp       near ptr M00_L45
M00_L41:
       vucomisd  xmm1,qword ptr [7FF977892718]
       jp        short M00_L42
       jne       short M00_L42
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977815740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0B855001530
       mov       rbx,[rcx]
       jmp       near ptr M00_L45
M00_L42:
       vucomisd  xmm1,qword ptr [7FF977892720]
       jp        short M00_L43
       jne       short M00_L43
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977815740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0B855001538
       mov       rbx,[rcx]
       jmp       short M00_L45
M00_L43:
       vmovsd    qword ptr [rsp+38],xmm1
       vucomisd  xmm1,qword ptr [7FF977892728]
       jp        short M00_L44
       jne       short M00_L44
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977815740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0B8550014E8
       mov       rbx,[rcx]
       jmp       short M00_L45
M00_L44:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       rcx,rbx
       vmovsd    xmm1,qword ptr [rsp+38]
       call      qword ptr [7FF977C1C750]
M00_L45:
       jmp       short M00_L48
M00_L46:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.Int64Value
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       rcx,rbx
       mov       rdx,[rsp+48]
       call      qword ptr [7FF977C1C768]
       jmp       short M00_L48
       lea       rcx,[rsp+40]
       call      qword ptr [7FF977C1C780]
       mov       rbx,rax
       jmp       short M00_L48
M00_L47:
       lea       rcx,[rsp+40]
       call      qword ptr [7FF977C1C798]
       mov       rbx,rax
M00_L48:
       mov       rdx,rbx
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptArray
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rsi,rax
       test      rsi,rsi
       je        short M00_L51
       mov       ebx,[rsi+34]
       xor       r14d,r14d
       jmp       short M00_L50
M00_L49:
       mov       rcx,[rsi+38]
       cmp       r14d,[rcx+8]
       jae       near ptr M00_L58
       mov       rdx,r14
       shl       rdx,4
       lea       rcx,[rcx+rdx+10]
       mov       [rsp+28],rdi
       mov       [rsp+30],rbp
       lea       rdx,[rsp+28]
       call      qword ptr [7FF977C15C38]
       test      eax,eax
       jne       near ptr M00_L57
       inc       r14d
M00_L50:
       cmp       r14d,ebx
       jl        short M00_L49
       jmp       short M00_L54
M00_L51:
       mov       rdx,rbx
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptPackedArray
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rsi,rax
       test      rsi,rsi
       je        short M00_L55
       mov       rcx,rsi
       mov       rax,[rsi]
       mov       rax,[rax+58]
       call      qword ptr [rax+8]
       mov       ebx,eax
       xor       r14d,r14d
       jmp       short M00_L53
M00_L52:
       lea       rdx,[rsp+60]
       mov       rcx,rsi
       mov       r8d,r14d
       mov       rax,[rsi]
       mov       rax,[rax+58]
       call      qword ptr [rax+10]
       mov       [rsp+28],rdi
       mov       [rsp+30],rbp
       lea       rdx,[rsp+28]
       lea       rcx,[rsp+60]
       call      qword ptr [7FF977C15C38]
       test      eax,eax
       jne       short M00_L57
       inc       r14d
M00_L53:
       cmp       r14d,ebx
       jl        short M00_L52
M00_L54:
       xor       eax,eax
       jmp       near ptr M00_L07
M00_L55:
       mov       rcx,rbx
       mov       rax,[rbx]
       mov       rax,[rax+58]
       call      qword ptr [rax]
       mov       rbx,rax
M00_L56:
       lea       rdx,[rsp+70]
       mov       rcx,rbx
       cmp       [rcx],ecx
       call      qword ptr [7FF977C15CB0]
       test      eax,eax
       je        short M00_L54
       mov       [rsp+28],rdi
       mov       [rsp+30],rbp
       lea       rdx,[rsp+28]
       lea       rcx,[rsp+70]
       call      qword ptr [7FF977C15C38]
       test      eax,eax
       je        short M00_L56
M00_L57:
       mov       eax,1
       jmp       near ptr M00_L07
M00_L58:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 1885
```
```assembly
; System.SpanHelpers.IndexOf(Char ByRef, Int32, Char ByRef, Int32)
       push      r15
       push      r14
       push      r13
       push      r12
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,78
       vmovaps   [rsp+60],xmm6
       vmovaps   [rsp+50],xmm7
       vmovaps   [rsp+40],xmm8
       vmovaps   [rsp+30],xmm9
       mov       rsi,rcx
       mov       rdi,r8
       mov       r8d,edx
       mov       ebx,r9d
       test      ebx,ebx
       je        short M01_L00
       lea       ebp,[rbx-1]
       test      ebp,ebp
       jne       near ptr M01_L02
       movsx     rdx,word ptr [rdi]
       movzx     ecx,dx
       dec       ecx
       cmp       ecx,0FE
       jae       short M01_L01
       movsx     rdx,dx
       mov       rcx,rsi
       vzeroupper
       vmovaps   xmm6,[rsp+60]
       vmovaps   xmm7,[rsp+50]
       vmovaps   xmm8,[rsp+40]
       vmovaps   xmm9,[rsp+30]
       add       rsp,78
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       jmp       qword ptr [7FF9779EC420]; System.PackedSpanHelpers.IndexOf[[System.SpanHelpers+DontNegate`1[[System.Int16, System.Private.CoreLib]], System.Private.CoreLib],[System.PackedSpanHelpers+NopTransform, System.Private.CoreLib]](Int16 ByRef, Int16, Int32)
M01_L00:
       xor       eax,eax
       vzeroupper
       vmovaps   xmm6,[rsp+60]
       vmovaps   xmm7,[rsp+50]
       vmovaps   xmm8,[rsp+40]
       vmovaps   xmm9,[rsp+30]
       add       rsp,78
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       ret
M01_L01:
       mov       rcx,rsi
       vzeroupper
       vmovaps   xmm6,[rsp+60]
       vmovaps   xmm7,[rsp+50]
       vmovaps   xmm8,[rsp+40]
       vmovaps   xmm9,[rsp+30]
       add       rsp,78
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       jmp       qword ptr [7FF977817288]; System.SpanHelpers.NonPackedIndexOfValueType[[System.Int16, System.Private.CoreLib],[System.SpanHelpers+DontNegate`1[[System.Int16, System.Private.CoreLib]], System.Private.CoreLib]](Int16 ByRef, Int16, Int32)
M01_L02:
       xor       r14d,r14d
       movzx     r15d,word ptr [rdi]
       mov       r13d,r8d
       sub       r13d,ebp
       cmp       r13d,8
       jge       near ptr M01_L06
       add       rdi,2
       test      r13d,r13d
       jle       short M01_L05
M01_L03:
       lea       rcx,[rsi+r14*2]
       movsx     rdx,r15w
       mov       r8d,r13d
       call      qword ptr [7FF977817288]; System.SpanHelpers.NonPackedIndexOfValueType[[System.Int16, System.Private.CoreLib],[System.SpanHelpers+DontNegate`1[[System.Int16, System.Private.CoreLib]], System.Private.CoreLib]](Int16 ByRef, Int16, Int32)
       test      eax,eax
       jl        short M01_L05
       sub       r13d,eax
       mov       ecx,eax
       add       r14,rcx
       test      r13d,r13d
       jle       short M01_L05
       lea       rcx,[rsi+r14*2+2]
       mov       r8d,ebp
       add       r8,r8
       mov       rdx,rdi
       call      qword ptr [7FF97781C330]; System.SpanHelpers.SequenceEqual(Byte ByRef, Byte ByRef, UIntPtr)
       test      eax,eax
       jne       short M01_L04
       dec       r13d
       inc       r14
       test      r13d,r13d
       jg        short M01_L03
       jmp       short M01_L05
M01_L04:
       mov       eax,r14d
       jmp       near ptr M01_L26
M01_L05:
       mov       eax,0FFFFFFFF
       vzeroupper
       vmovaps   xmm6,[rsp+60]
       vmovaps   xmm7,[rsp+50]
       vmovaps   xmm8,[rsp+40]
       vmovaps   xmm9,[rsp+30]
       add       rsp,78
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       ret
M01_L06:
       lea       ecx,[r13-10]
       test      ecx,ecx
       jl        near ptr M01_L16
       movsxd    rcx,ebp
       movzx     ecx,word ptr [rdi+rcx*2]
       movsxd    rbp,ebp
       jmp       short M01_L08
M01_L07:
       dec       rbp
       movzx     ecx,word ptr [rdi+rbp*2]
M01_L08:
       cmp       ecx,r15d
       jne       short M01_L09
       cmp       rbp,1
       jg        short M01_L07
M01_L09:
       vmovd     xmm6,r15d
       vpbroadcastw ymm6,xmm6
       vmovd     xmm7,ecx
       vpbroadcastw ymm7,xmm7
       mov       r15d,r13d
       add       r15,0FFFFFFFFFFFFFFF0
M01_L10:
       vpcmpeqw  ymm0,ymm6,[rsi+r14*2]
       lea       rcx,[r14+rbp]
       vpcmpeqw  ymm1,ymm7,[rsi+rcx*2]
       vpand     ymm0,ymm1,ymm0
       vptest    ymm0,ymm0
       jne       short M01_L12
       jmp       short M01_L14
M01_L11:
       cmp       r14,r15
       jle       short M01_L10
       mov       r14,r15
       jmp       short M01_L10
M01_L12:
       vpmovmskb r12d,ymm0
M01_L13:
       xor       ecx,ecx
       tzcnt     ecx,r12d
       shr       ecx,1
       mov       eax,ecx
       mov       [rsp+28],rax
       cmp       ebx,2
       je        short M01_L15
       lea       rcx,[r14+rax]
       lea       rcx,[rsi+rcx*2]
       mov       r8d,ebx
       add       r8,r8
       mov       rdx,rdi
       vextractf128 xmm8,ymm6,1
       vextractf128 xmm9,ymm7,1
       call      qword ptr [7FF97781C330]; System.SpanHelpers.SequenceEqual(Byte ByRef, Byte ByRef, UIntPtr)
       test      eax,eax
       vinsertf128 ymm6,ymm6,xmm8,1
       vinsertf128 ymm7,ymm7,xmm9,1
       mov       rax,[rsp+28]
       jne       short M01_L15
       blsr      ecx,r12d
       blsr      r12d,ecx
       jne       short M01_L13
M01_L14:
       add       r14,10
       mov       ecx,r13d
       cmp       r14,rcx
       je        near ptr M01_L05
       jmp       short M01_L11
M01_L15:
       add       eax,r14d
       jmp       near ptr M01_L26
M01_L16:
       movsxd    rcx,ebp
       movzx     ecx,word ptr [rdi+rcx*2]
       movsxd    rbp,ebp
       jmp       short M01_L18
M01_L17:
       dec       rbp
       movzx     ecx,word ptr [rdi+rbp*2]
M01_L18:
       cmp       ecx,r15d
       jne       short M01_L19
       cmp       rbp,1
       jg        short M01_L17
M01_L19:
       vmovd     xmm6,r15d
       vpbroadcastw xmm6,xmm6
       vmovd     xmm7,ecx
       vpbroadcastw xmm7,xmm7
       mov       r15d,r13d
       add       r15,0FFFFFFFFFFFFFFF8
M01_L20:
       vpcmpeqw  xmm0,xmm6,[rsi+r14*2]
       lea       rcx,[r14+rbp]
       vpcmpeqw  xmm1,xmm7,[rsi+rcx*2]
       vpand     xmm0,xmm1,xmm0
       vptest    xmm0,xmm0
       jne       short M01_L22
       jmp       short M01_L24
M01_L21:
       cmp       r14,r15
       jle       short M01_L20
       mov       r14,r15
       jmp       short M01_L20
M01_L22:
       vpmovmskb r12d,xmm0
M01_L23:
       xor       ecx,ecx
       tzcnt     ecx,r12d
       shr       ecx,1
       mov       eax,ecx
       mov       [rsp+20],rax
       cmp       ebx,2
       je        short M01_L25
       lea       rcx,[r14+rax]
       lea       rcx,[rsi+rcx*2]
       mov       r8d,ebx
       add       r8,r8
       mov       rdx,rdi
       call      qword ptr [7FF97781C330]; System.SpanHelpers.SequenceEqual(Byte ByRef, Byte ByRef, UIntPtr)
       test      eax,eax
       mov       rax,[rsp+20]
       jne       short M01_L25
       blsr      eax,r12d
       blsr      r12d,eax
       jne       short M01_L23
M01_L24:
       add       r14,8
       mov       eax,r13d
       cmp       r14,rax
       je        near ptr M01_L05
       jmp       short M01_L21
M01_L25:
       add       eax,r14d
M01_L26:
       vzeroupper
       vmovaps   xmm6,[rsp+60]
       vmovaps   xmm7,[rsp+50]
       vmovaps   xmm8,[rsp+40]
       vmovaps   xmm9,[rsp+30]
       add       rsp,78
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       ret
; Total bytes of code 851
```
```assembly
; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rax,[rcx+20]
       mov       rax,[rax-18]
       mov       rdx,rax
       test      dl,1
       jne       short M02_L00
       ret
M02_L00:
       jmp       qword ptr [7FF977815C38]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBaseSlow(System.Runtime.CompilerServices.MethodTable*)
; Total bytes of code 23
```
```assembly
; System.String.FastAllocateString(IntPtr)
       mov       rdx,rcx
       mov       rcx,offset MT_System.String
       jmp       near ptr 00007FF9D74C4E80
; Total bytes of code 18
```
```assembly
; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rax,[rcx+20]
       mov       rax,[rax-10]
       mov       rdx,rax
       test      dl,1
       jne       short M04_L00
       ret
M04_L00:
       jmp       qword ptr [7FF9779EE838]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBaseSlow(System.Runtime.CompilerServices.MethodTable*)
; Total bytes of code 23
```
```assembly
; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rdx,rdx
       je        short M05_L00
       cmp       [rdx],rcx
       jne       short M05_L01
M05_L00:
       mov       rax,rdx
       ret
M05_L01:
       mov       rax,[rdx]
       mov       rax,[rax+10]
M05_L02:
       cmp       rax,rcx
       je        short M05_L00
       test      rax,rax
       je        short M05_L03
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M05_L00
       test      rax,rax
       je        short M05_L03
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M05_L00
       test      rax,rax
       je        short M05_L03
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M05_L00
       test      rax,rax
       je        short M05_L03
       mov       rax,[rax+10]
       jmp       short M05_L02
M05_L03:
       xor       edx,edx
       jmp       short M05_L00
; Total bytes of code 83
```
```assembly
; System.PackedSpanHelpers.IndexOf[[System.SpanHelpers+DontNegate`1[[System.Int16, System.Private.CoreLib]], System.Private.CoreLib],[System.PackedSpanHelpers+NopTransform, System.Private.CoreLib]](Int16 ByRef, Int16, Int32)
       cmp       r8d,8
       jl        near ptr M06_L09
       mov       rax,rcx
       movzx     edx,dl
       movd      xmm0,edx
       xorps     xmm1,xmm1
       pshufb    xmm0,xmm1
       cmp       r8d,10
       jle       short M06_L02
       lea       edx,[r8-10]
       movsxd    rdx,edx
       lea       rdx,[rax+rdx*2]
       movups    xmm1,[rcx]
       movups    xmm2,[rcx+10]
       packuswb  xmm1,xmm2
       pcmpeqb   xmm1,xmm0
       ptest     xmm1,xmm1
       jne       short M06_L01
M06_L00:
       add       rax,20
       cmp       rax,rdx
       jae       short M06_L02
       movups    xmm1,[rax]
       movups    xmm2,[rax+10]
       packuswb  xmm1,xmm2
       pcmpeqb   xmm1,xmm0
       ptest     xmm1,xmm1
       je        short M06_L00
M06_L01:
       pmovmskb  r8d,xmm1
       bsf       edx,r8d
       mov       r10d,20
       test      r8d,r8d
       cmove     edx,r10d
       sub       rax,rcx
       shr       rax,1
       add       eax,edx
       jmp       short M06_L04
M06_L02:
       add       r8d,0FFFFFFF8
       movsxd    rdx,r8d
       lea       rdx,[rcx+rdx*2]
       cmp       rax,rdx
       cmova     rax,rdx
       movups    xmm1,[rax]
       movups    xmm2,[rdx]
       packuswb  xmm1,xmm2
       pcmpeqb   xmm0,xmm1
       ptest     xmm0,xmm0
       jne       short M06_L05
M06_L03:
       mov       eax,0FFFFFFFF
M06_L04:
       ret
M06_L05:
       pmovmskb  r8d,xmm0
       test      r8d,r8d
       je        short M06_L06
       bsf       r8d,r8d
       cmp       r8d,8
       jl        short M06_L08
       jmp       short M06_L07
M06_L06:
       mov       r8d,20
M06_L07:
       mov       rax,rdx
       add       r8d,0FFFFFFF8
M06_L08:
       sub       rax,rcx
       shr       rax,1
       add       eax,r8d
       jmp       short M06_L04
M06_L09:
       xor       r10d,r10d
       cmp       r8d,4
       jl        short M06_L10
       add       r8d,0FFFFFFFC
       movsx     r10,word ptr [rcx]
       movsx     r9,dx
       cmp       r10d,r9d
       je        short M06_L13
       movsx     rax,word ptr [rcx+2]
       cmp       eax,r9d
       je        short M06_L14
       movsx     rax,word ptr [rcx+4]
       cmp       eax,r9d
       je        short M06_L15
       movsx     rax,word ptr [rcx+6]
       cmp       eax,r9d
       je        short M06_L16
       mov       r10d,4
M06_L10:
       test      r8d,r8d
       jle       short M06_L03
       movsx     r9,dx
M06_L11:
       dec       r8d
       movsx     rax,word ptr [rcx+r10*2]
       cmp       eax,r9d
       je        short M06_L12
       inc       r10
       test      r8d,r8d
       jg        short M06_L11
       jmp       near ptr M06_L03
M06_L12:
       mov       eax,r10d
       jmp       near ptr M06_L04
M06_L13:
       xor       eax,eax
       ret
M06_L14:
       mov       eax,1
       ret
M06_L15:
       mov       eax,2
       ret
M06_L16:
       mov       eax,3
       jmp       near ptr M06_L04
; Total bytes of code 351
```
```assembly
; System.SpanHelpers.NonPackedIndexOfValueType[[System.Int16, System.Private.CoreLib],[System.SpanHelpers+DontNegate`1[[System.Int16, System.Private.CoreLib]], System.Private.CoreLib]](Int16 ByRef, Int16, Int32)
       cmp       r8d,8
       jl        short M07_L03
       cmp       r8d,10
       jl        near ptr M07_L18
       vmovd     xmm0,edx
       vpbroadcastw ymm0,xmm0
       mov       rdx,rcx
       lea       eax,[r8-10]
       cdqe
       lea       rax,[rdx+rax*2]
       vpcmpeqw  ymm1,ymm0,[rcx]
       vptest    ymm1,ymm1
       jne       near ptr M07_L16
       nop       word ptr [rax+rax]
M07_L00:
       add       rdx,20
       cmp       rdx,rax
       ja        short M07_L01
       vpcmpeqw  ymm1,ymm0,[rdx]
       vptest    ymm1,ymm1
       jne       near ptr M07_L16
       jmp       short M07_L00
M07_L01:
       mov       edx,r8d
       test      dl,0F
       jne       near ptr M07_L17
M07_L02:
       mov       eax,0FFFFFFFF
       vzeroupper
       ret
M07_L03:
       xor       r10d,r10d
       cmp       r8d,8
       jl        near ptr M07_L05
M07_L04:
       add       r8d,0FFFFFFF8
       movsx     rax,word ptr [rcx+r10*2]
       movsx     r9,dx
       cmp       eax,r9d
       je        near ptr M07_L15
       movsx     rax,word ptr [rcx+r10*2+2]
       movsx     r9,dx
       cmp       eax,r9d
       je        near ptr M07_L14
       movsx     rax,word ptr [rcx+r10*2+4]
       movsx     r9,dx
       cmp       eax,r9d
       je        near ptr M07_L13
       movsx     rax,word ptr [rcx+r10*2+6]
       movsx     r9,dx
       cmp       eax,r9d
       je        near ptr M07_L12
       movsx     rax,word ptr [rcx+r10*2+8]
       movsx     r9,dx
       cmp       eax,r9d
       je        near ptr M07_L11
       movsx     rax,word ptr [rcx+r10*2+0A]
       movsx     r9,dx
       cmp       eax,r9d
       je        near ptr M07_L10
       movsx     rax,word ptr [rcx+r10*2+0C]
       movsx     r9,dx
       cmp       eax,r9d
       je        near ptr M07_L09
       movsx     rax,word ptr [rcx+r10*2+0E]
       movsx     r9,dx
       cmp       eax,r9d
       je        near ptr M07_L08
       add       r10,8
       cmp       r8d,8
       jge       near ptr M07_L04
M07_L05:
       cmp       r8d,4
       jl        short M07_L07
       add       r8d,0FFFFFFFC
       movsx     rax,word ptr [rcx+r10*2]
       movsx     r9,dx
       cmp       eax,r9d
       je        near ptr M07_L15
       movsx     rax,word ptr [rcx+r10*2+2]
       movsx     r9,dx
       cmp       eax,r9d
       je        near ptr M07_L14
       movsx     rax,word ptr [rcx+r10*2+4]
       movsx     r9,dx
       cmp       eax,r9d
       je        short M07_L13
       movsx     rax,word ptr [rcx+r10*2+6]
       movsx     r9,dx
       cmp       eax,r9d
       je        short M07_L12
       add       r10,4
       test      r8d,r8d
       jle       near ptr M07_L02
M07_L06:
       dec       r8d
       movsx     rax,word ptr [rcx+r10*2]
       movsx     r9,dx
       cmp       eax,r9d
       je        short M07_L15
       inc       r10
M07_L07:
       test      r8d,r8d
       jg        short M07_L06
       jmp       near ptr M07_L02
M07_L08:
       lea       eax,[r10+7]
       jmp       near ptr M07_L22
M07_L09:
       lea       eax,[r10+6]
       jmp       near ptr M07_L22
M07_L10:
       lea       eax,[r10+5]
       jmp       near ptr M07_L22
M07_L11:
       lea       eax,[r10+4]
       jmp       near ptr M07_L22
M07_L12:
       lea       eax,[r10+3]
       jmp       near ptr M07_L22
M07_L13:
       lea       eax,[r10+2]
       jmp       near ptr M07_L22
M07_L14:
       lea       eax,[r10+1]
       jmp       near ptr M07_L22
M07_L15:
       mov       eax,r10d
       jmp       near ptr M07_L22
M07_L16:
       mov       rax,rdx
       sub       rax,rcx
       shr       rax,1
       vpshufb   ymm0,ymm1,[7FF9778914A0]
       vpermq    ymm0,ymm0,0D8
       vpmovmskb ecx,xmm0
       tzcnt     ecx,ecx
       add       eax,ecx
       jmp       near ptr M07_L22
M07_L17:
       vpcmpeqw  ymm1,ymm0,[rax]
       vptest    ymm1,ymm1
       je        near ptr M07_L02
       sub       rax,rcx
       shr       rax,1
       vpshufb   ymm1,ymm1,[7FF9778914A0]
       vpermq    ymm0,ymm1,0D8
       vpmovmskb ecx,xmm0
       xor       r8d,r8d
       tzcnt     r8d,ecx
       add       eax,r8d
       jmp       near ptr M07_L22
M07_L18:
       vmovd     xmm0,edx
       vpbroadcastw xmm0,xmm0
       mov       rax,rcx
       lea       edx,[r8-8]
       movsxd    rdx,edx
       lea       rdx,[rax+rdx*2]
M07_L19:
       vpcmpeqw  xmm1,xmm0,[rax]
       vptest    xmm1,xmm1
       jne       short M07_L20
       add       rax,10
       cmp       rax,rdx
       jbe       short M07_L19
       jmp       short M07_L21
M07_L20:
       sub       rax,rcx
       shr       rax,1
       vpshufb   xmm0,xmm1,[7FF9778914A0]
       vpmovmskb r8d,xmm0
       xor       ecx,ecx
       tzcnt     ecx,r8d
       add       eax,ecx
       jmp       short M07_L22
M07_L21:
       mov       eax,r8d
       test      al,7
       je        near ptr M07_L02
       vpcmpeqw  xmm1,xmm0,[rdx]
       vptest    xmm1,xmm1
       je        near ptr M07_L02
       sub       rdx,rcx
       shr       rdx,1
       vpshufb   xmm1,xmm1,[7FF9778914A0]
       vpmovmskb ecx,xmm1
       xor       eax,eax
       tzcnt     eax,ecx
       add       eax,edx
M07_L22:
       vzeroupper
       ret
; Total bytes of code 711
```
```assembly
; System.SpanHelpers.SequenceEqual(Byte ByRef, Byte ByRef, UIntPtr)
       cmp       r8,8
       jb        short M08_L06
       cmp       rcx,rdx
       je        short M08_L04
       cmp       r8,10
       jae       short M08_L01
       add       r8,0FFFFFFFFFFFFFFF8
       mov       rax,[rcx]
       sub       rax,[rdx]
       mov       rcx,[rcx+r8]
       sub       rcx,[rdx+r8]
       or        rax,rcx
       sete      al
       movzx     eax,al
M08_L00:
       ret
M08_L01:
       xor       eax,eax
       add       r8,0FFFFFFFFFFFFFFF0
       je        short M08_L03
       movups    xmm0,[rcx]
       movups    xmm1,[rdx]
       pcmpeqb   xmm0,xmm1
       pmovmskb  r10d,xmm0
       cmp       r10d,0FFFF
       jne       short M08_L05
M08_L02:
       add       rax,10
       cmp       r8,rax
       ja        short M08_L10
M08_L03:
       movups    xmm0,[rcx+r8]
       movups    xmm1,[rdx+r8]
       pcmpeqb   xmm0,xmm1
       pmovmskb  eax,xmm0
       cmp       eax,0FFFF
       jne       short M08_L05
M08_L04:
       mov       eax,1
       ret
M08_L05:
       xor       eax,eax
       ret
M08_L06:
       cmp       r8,4
       jb        short M08_L07
       add       r8,0FFFFFFFFFFFFFFFC
       mov       eax,[rcx]
       sub       eax,[rdx]
       mov       ecx,[rcx+r8]
       sub       ecx,[rdx+r8]
       or        eax,ecx
       sete      al
       movzx     eax,al
       jmp       short M08_L00
M08_L07:
       xor       eax,eax
       mov       r10,r8
       and       r10,2
       je        short M08_L08
       movzx     eax,word ptr [rcx]
       movzx     r9d,word ptr [rdx]
       sub       eax,r9d
M08_L08:
       test      r8b,1
       je        short M08_L09
       movzx     ecx,byte ptr [rcx+r10]
       movzx     edx,byte ptr [rdx+r10]
       sub       ecx,edx
       or        eax,ecx
M08_L09:
       test      eax,eax
       sete      al
       movzx     eax,al
       jmp       near ptr M08_L00
M08_L10:
       movups    xmm0,[rcx+rax]
       movups    xmm1,[rdx+rax]
       pcmpeqb   xmm0,xmm1
       pmovmskb  r10d,xmm0
       cmp       r10d,0FFFF
       jne       short M08_L05
       jmp       near ptr M08_L02
; Total bytes of code 237
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
```assembly
; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBaseSlow(System.Runtime.CompilerServices.MethodTable*)
       push      rbx
       sub       rsp,30
       xor       eax,eax
       mov       [rsp+28],rax
       mov       rbx,rcx
       mov       rcx,rbx
       call      qword ptr [7FF96E854D70]; Precode of System.Runtime.CompilerServices.InitHelpers.InitClassSlow(System.Runtime.CompilerServices.MethodTable*)
       mov       rax,[rbx+20]
       mov       rax,[rax-10]
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
; AuroraBenchmark.SafeOpsBenchmarks.CheckedWrapper()
       sub       rsp,38
       xor       eax,eax
       mov       [rsp+28],rax
       mov       rax,[rcx+50]
       mov       rcx,[rcx+58]
       test      rax,rax
       je        short M00_L05
       mov       rdx,8001400A00
       cmp       rax,[rdx]
       je        short M00_L01
       mov       rdx,8001400A08
       cmp       rax,[rdx]
       je        short M00_L01
       movsx     rdx,cx
M00_L00:
       test      edx,edx
       je        near ptr M00_L09
M00_L01:
       test      rax,rax
       je        short M00_L02
       mov       rdx,offset MT_AuroraScript.Runtime.Types.ScriptInt32Array
       cmp       [rax],rdx
       jne       short M00_L02
       jmp       short M00_L04
M00_L02:
       mov       [rsp+28],rax
       mov       [rsp+30],rcx
       lea       rcx,[rsp+28]
       mov       edx,6
       call      qword ptr [7FF977C35B78]
       mov       rdx,rax
       test      rdx,rdx
       je        short M00_L03
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt32Array
       cmp       [rdx],rcx
       jne       short M00_L08
M00_L03:
       mov       rax,rdx
M00_L04:
       add       rsp,38
       ret
M00_L05:
       test      rcx,rcx
       je        short M00_L07
       lea       rdx,[rcx-1]
       mov       r8d,1
       mov       r10d,2
       cmp       rdx,1
       cmova     r8d,r10d
       mov       edx,r8d
M00_L06:
       jmp       short M00_L00
M00_L07:
       xor       edx,edx
       jmp       short M00_L06
M00_L08:
       mov       rdx,rax
       call      qword ptr [7FF977836328]; System.Runtime.CompilerServices.CastHelpers.ChkCastClass(Void*, System.Object)
       int       3
M00_L09:
       xor       eax,eax
       jmp       short M00_L04
; Total bytes of code 197
```
```assembly
; System.Runtime.CompilerServices.CastHelpers.ChkCastClass(Void*, System.Object)
       test      rdx,rdx
       je        short M01_L00
       cmp       [rdx],rcx
       jne       short M01_L01
M01_L00:
       mov       rax,rdx
       ret
M01_L01:
       jmp       qword ptr [7FF977A04D20]; System.Runtime.CompilerServices.CastHelpers.ChkCastClassSpecial(Void*, System.Object)
; Total bytes of code 20
```
```assembly
; System.Runtime.CompilerServices.CastHelpers.ChkCastClassSpecial(Void*, System.Object)
       mov       rax,[rdx]
       mov       rax,[rax+10]
       cmp       rax,rcx
       jne       short M02_L01
M02_L00:
       mov       rax,rdx
       ret
M02_L01:
       test      rax,rax
       je        short M02_L04
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M02_L00
       jmp       short M02_L03
M02_L02:
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M02_L00
       jmp       short M02_L01
M02_L03:
       test      rax,rax
       je        short M02_L04
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M02_L00
       test      rax,rax
       je        short M02_L04
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M02_L00
       test      rax,rax
       jne       short M02_L02
M02_L04:
       lea       rax,[System.Reflection.CustomAttributeExtensions.GetCustomAttribute[[System.__Canon, System.Private.CoreLib]](System.Reflection.Assembly)]
       jmp       qword ptr [rax]
; Total bytes of code 86
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
       je        short M03_L00
       mov       rcx,rsi
       cmp       [r8],rcx
       je        short M03_L00
       mov       rdx,rax
       call      qword ptr [7FF96E839090]; Precode of System.Runtime.CompilerServices.CastHelpers.ChkCastAny(Void*, System.Object)
       mov       r8,rax
M03_L00:
       mov       rax,r8
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
; Total bytes of code 88
```

## .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3 (Job: Job-BZNPUE(IterationCount=5, IterationTime=200ms, LaunchCount=1, WarmupCount=3))

```assembly
; AuroraBenchmark.SafeOpsBenchmarks.NullWrapper()
       push      rsi
       push      rbx
       sub       rsp,38
       xor       eax,eax
       mov       [rsp+28],rax
       mov       rbx,[rcx+60]
       mov       rsi,[rcx+68]
       test      rbx,rbx
       jne       short M00_L02
       test      rsi,rsi
       jne       short M00_L03
       xor       edx,edx
M00_L00:
       test      edx,edx
       jne       short M00_L04
       xor       eax,eax
M00_L01:
       add       rsp,38
       pop       rbx
       pop       rsi
       ret
M00_L02:
       mov       rdx,0B820000A00
       cmp       rbx,[rdx]
       je        short M00_L04
       mov       rdx,0B820000A08
       cmp       rbx,[rdx]
       je        short M00_L04
       movsx     rdx,si
       jmp       short M00_L00
M00_L03:
       lea       rdx,[rsi-1]
       mov       ecx,1
       mov       eax,2
       cmp       rdx,1
       mov       edx,eax
       cmovbe    edx,ecx
       jmp       short M00_L00
M00_L04:
       mov       rdx,rbx
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt32Array
       call      qword ptr [7FF977816850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rax,rax
       jne       short M00_L05
       mov       [rsp+28],rbx
       mov       [rsp+30],rsi
       lea       rcx,[rsp+28]
       mov       edx,6
       call      qword ptr [7FF977C15B78]
       mov       rdx,rax
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt32Array
       call      qword ptr [7FF977816328]; System.Runtime.CompilerServices.CastHelpers.ChkCastClass(Void*, System.Object)
       jmp       near ptr M00_L01
M00_L05:
       jmp       near ptr M00_L01
; Total bytes of code 186
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
; System.Runtime.CompilerServices.CastHelpers.ChkCastClass(Void*, System.Object)
       test      rdx,rdx
       je        short M02_L00
       cmp       [rdx],rcx
       jne       short M02_L01
M02_L00:
       mov       rax,rdx
       ret
M02_L01:
       jmp       qword ptr [7FF9779E4D20]; System.Runtime.CompilerServices.CastHelpers.ChkCastClassSpecial(Void*, System.Object)
; Total bytes of code 20
```
```assembly
; System.Runtime.CompilerServices.CastHelpers.ChkCastClassSpecial(Void*, System.Object)
       mov       rax,[rdx]
       mov       rax,[rax+10]
       cmp       rax,rcx
       jne       short M03_L01
M03_L00:
       mov       rax,rdx
       ret
M03_L01:
       test      rax,rax
       je        short M03_L04
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M03_L00
       jmp       short M03_L03
M03_L02:
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M03_L00
       jmp       short M03_L01
M03_L03:
       test      rax,rax
       je        short M03_L04
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M03_L00
       test      rax,rax
       je        short M03_L04
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M03_L00
       test      rax,rax
       jne       short M03_L02
M03_L04:
       lea       rax,[System.Reflection.CustomAttributeExtensions.GetCustomAttribute[[System.__Canon, System.Private.CoreLib]](System.Reflection.Assembly)]
       jmp       qword ptr [rax]
; Total bytes of code 86
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

