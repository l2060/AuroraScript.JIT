## .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3 (Job: Job-BZNPUE(IterationCount=5, IterationTime=200ms, LaunchCount=1, WarmupCount=3))

```assembly
; AuroraBenchmark.SafeOpsBenchmarks.ArrayMiss()
       push      r14
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,70
       xor       eax,eax
       mov       [rsp+28],rax
       vxorps    xmm4,xmm4,xmm4
       vmovdqu   ymmword ptr [rsp+30],ymm4
       vmovdqu   ymmword ptr [rsp+50],ymm4
M00_L00:
       mov       rdx,[rcx+10]
       mov       rbx,[rcx+18]
       mov       rsi,[rcx+70]
       mov       rdi,[rcx+78]
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rsp+50],xmm0
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rsp+60],xmm0
       test      rdx,rdx
       jne       near ptr M00_L11
       test      rbx,rbx
       je        near ptr M00_L13
       lea       rcx,[rbx-1]
       mov       eax,1
       mov       r8d,2
       cmp       rcx,1
       cmova     eax,r8d
M00_L01:
       cmp       eax,4
       je        near ptr M00_L14
M00_L02:
       mov       [rsp+40],rdx
       mov       [rsp+48],rbx
       cmp       qword ptr [rsp+40],0
       jne       near ptr M00_L12
       mov       rcx,[rsp+48]
       test      rcx,rcx
       je        near ptr M00_L27
       dec       rcx
       mov       eax,1
       mov       edx,2
       cmp       rcx,1
       cmova     eax,edx
M00_L03:
       cmp       eax,4
       jbe       near ptr M00_L26
       cmp       eax,1000
       je        near ptr M00_L48
       cmp       eax,2000
       je        near ptr M00_L49
       mov       rdx,[rsp+40]
       mov       rbx,rdx
       test      rbx,rbx
       je        short M00_L04
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptArray
       cmp       [rbx],rcx
       jne       near ptr M00_L28
M00_L04:
       test      rbx,rbx
       je        near ptr M00_L50
M00_L05:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptArray
       cmp       [rbx],rcx
       jne       near ptr M00_L54
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptEnumerator
       call      CORINFO_HELP_NEWSFAST
       mov       rbp,rax
       mov       rcx,0F4CA3C6C60
       mov       [rbp+10],rcx
       mov       rcx,0F4CD800A28
       mov       rdx,[rcx]
       lea       rcx,[rbp+20]
       call      CORINFO_HELP_ASSIGN_REF
       mov       rcx,0F4CD800A30
       mov       rdx,[rcx]
       lea       rcx,[rbp+8]
       call      CORINFO_HELP_ASSIGN_REF
       mov       dword ptr [rbp+34],1
       lea       rcx,[rbp+40]
       mov       rdx,rbx
       call      CORINFO_HELP_ASSIGN_REF
       mov       ecx,[rbx+34]
       mov       [rbp+58],ecx
       xor       ecx,ecx
       mov       [rbp+5C],ecx
M00_L06:
       mov       edx,[rbp+5C]
       cmp       edx,[rbp+58]
       jge       short M00_L08
M00_L07:
       lea       rdx,[rsp+60]
       mov       rcx,rbp
       call      qword ptr [7FF977C35DB8]; AuroraScript.Runtime.Types.ScriptEnumerator.Value()
       inc       dword ptr [rbp+5C]
       mov       [rsp+28],rsi
       mov       [rsp+30],rdi
       lea       rdx,[rsp+28]
       lea       rcx,[rsp+60]
       call      qword ptr [7FF977C35C38]; AuroraScript.Runtime.ScriptDatum.Equals(AuroraScript.Runtime.ScriptDatum)
       test      eax,eax
       jne       near ptr M00_L55
       mov       eax,[rbp+5C]
       cmp       eax,[rbp+58]
       jl        short M00_L07
M00_L08:
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rsp+60],xmm0
M00_L09:
       xor       eax,eax
M00_L10:
       add       rsp,70
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       ret
M00_L11:
       mov       rax,0F4CD800A00
       cmp       rdx,[rax]
       je        near ptr M00_L02
       mov       rcx,0F4CD800A08
       cmp       rdx,[rcx]
       je        near ptr M00_L02
       movsx     rax,bx
       jmp       near ptr M00_L01
M00_L12:
       mov       rax,[rsp+40]
       mov       rcx,0F4CD800A00
       cmp       rax,[rcx]
       je        near ptr M00_L25
       mov       rcx,[rsp+40]
       mov       rax,0F4CD800A08
       movsx     rdx,word ptr [rsp+48]
       mov       r8d,2000
       cmp       rcx,[rax]
       mov       eax,r8d
       cmovne    eax,edx
       jmp       near ptr M00_L03
M00_L13:
       xor       eax,eax
       jmp       near ptr M00_L01
M00_L14:
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbx,rax
       mov       rdx,0F4CA3C0008
       test      rbx,rbx
       cmove     rbx,rdx
       mov       rbp,rbx
       test      rsi,rsi
       je        short M00_L15
       mov       rcx,0F4CD800A00
       cmp       rsi,[rcx]
       je        near ptr M00_L19
       mov       rdx,0F4CD800A08
       cmp       rsi,[rdx]
       je        short M00_L19
       movsx     rcx,di
       jmp       short M00_L18
M00_L15:
       test      rdi,rdi
       je        short M00_L17
       lea       rdx,[rdi-1]
       mov       ecx,1
       mov       eax,2
       cmp       rdx,1
       cmova     ecx,eax
M00_L16:
       jmp       short M00_L18
M00_L17:
       xor       ecx,ecx
       jmp       short M00_L16
M00_L18:
       cmp       ecx,4
       jne       short M00_L19
       mov       rdx,rsi
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdx,rax
       mov       r9,0F4CA3C0008
       test      rdx,rdx
       cmove     rdx,r9
       cmp       dword ptr [rdx+8],1
       jle       short M00_L19
       mov       dword ptr [rsp+20],4
       mov       r9d,[rbx+8]
       mov       rcx,rbx
       xor       r8d,r8d
       call      qword ptr [7FF977C37D38]
       not       eax
       shr       eax,1F
       jmp       near ptr M00_L10
M00_L19:
       xor       ebx,ebx
       jmp       near ptr M00_L24
M00_L20:
       movzx     r14d,word ptr [rbp+rbx*2+0C]
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0F4CD801498
       mov       rcx,[rcx]
       cmp       [rcx+8],r14d
       jg        short M00_L21
       mov       ecx,1
       call      qword ptr [7FF977836670]; System.String.FastAllocateString(IntPtr)
       mov       [rax+0C],r14w
       jmp       short M00_L22
M00_L21:
       mov       ecx,r14d
       call      qword ptr [7FF977C37D08]
       mov       rax,[rax+38]
M00_L22:
       test      rax,rax
       jne       short M00_L23
       mov       rax,0F4CA3C0008
M00_L23:
       mov       [rsp+50],rax
       mov       qword ptr [rsp+58],4
       mov       [rsp+28],rsi
       mov       [rsp+30],rdi
       lea       rdx,[rsp+28]
       lea       rcx,[rsp+50]
       call      qword ptr [7FF977C35C38]; AuroraScript.Runtime.ScriptDatum.Equals(AuroraScript.Runtime.ScriptDatum)
       test      eax,eax
       jne       near ptr M00_L55
       inc       ebx
M00_L24:
       cmp       [rbp+8],ebx
       jg        near ptr M00_L20
       jmp       near ptr M00_L09
M00_L25:
       mov       eax,1000
       jmp       near ptr M00_L03
M00_L26:
       mov       ecx,eax
       lea       rax,[7FF9778B1FD8]
       mov       eax,[rax+rcx*4]
       lea       rdx,[M00_L00]
       add       rax,rdx
       jmp       rax
M00_L27:
       xor       eax,eax
       jmp       near ptr M00_L03
M00_L28:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbx,rax
       jmp       near ptr M00_L04
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0F4CD800A50
       mov       rbx,[rcx]
       jmp       near ptr M00_L05
       cmp       qword ptr [rsp+48],2
       je        short M00_L29
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BooleanValue
       call      qword ptr [7FF977835740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0F4CD801520
       mov       rbx,[rcx]
       jmp       short M00_L30
M00_L29:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BooleanValue
       call      qword ptr [7FF977835740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0F4CD801518
       mov       rbx,[rcx]
M00_L30:
       jmp       near ptr M00_L05
       mov       rcx,[rsp+48]
       mov       rax,8007FFFFFFFFFFFF
       add       rax,rcx
       cmp       rax,3
       jbe       short M00_L31
       vmovq     xmm1,rcx
       jmp       short M00_L33
M00_L31:
       cmp       rax,2
       jbe       short M00_L32
       vmovsd    xmm1,qword ptr [7FF9778B1FF0]
       jmp       short M00_L33
M00_L32:
       vmovq     xmm1,rax
M00_L33:
       vucomisd  xmm1,xmm1
       jp        short M00_L34
       je        short M00_L35
M00_L34:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977835740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0F4CD801508
       mov       rbx,[rcx]
       jmp       near ptr M00_L47
M00_L35:
       vxorps    xmm0,xmm0,xmm0
       vucomisd  xmm1,xmm0
       jp        short M00_L36
       jne       short M00_L36
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977835740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0F4CD8014B8
       mov       rbx,[rcx]
       jmp       near ptr M00_L47
M00_L36:
       vucomisd  xmm1,qword ptr [7FF9778B1FF8]
       jp        short M00_L37
       jne       short M00_L37
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977835740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0F4CD8014C0
       mov       rbx,[rcx]
       jmp       near ptr M00_L47
M00_L37:
       vucomisd  xmm1,qword ptr [7FF9778B2000]
       jp        short M00_L38
       jne       short M00_L38
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977835740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0F4CD8014C8
       mov       rbx,[rcx]
       jmp       near ptr M00_L47
M00_L38:
       vucomisd  xmm1,qword ptr [7FF9778B2008]
       jp        short M00_L39
       jne       short M00_L39
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977835740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0F4CD8014D0
       mov       rbx,[rcx]
       jmp       near ptr M00_L47
M00_L39:
       vucomisd  xmm1,qword ptr [7FF9778B2010]
       jp        short M00_L40
       jne       short M00_L40
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977835740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0F4CD8014D8
       mov       rbx,[rcx]
       jmp       near ptr M00_L47
M00_L40:
       vucomisd  xmm1,qword ptr [7FF9778B2018]
       jp        short M00_L41
       jne       short M00_L41
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977835740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0F4CD8014E0
       mov       rbx,[rcx]
       jmp       near ptr M00_L47
M00_L41:
       vucomisd  xmm1,qword ptr [7FF9778B2020]
       jp        short M00_L42
       jne       short M00_L42
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977835740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0F4CD8014E8
       mov       rbx,[rcx]
       jmp       near ptr M00_L47
M00_L42:
       vucomisd  xmm1,qword ptr [7FF9778B2028]
       jp        short M00_L43
       jne       short M00_L43
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977835740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0F4CD8014F0
       mov       rbx,[rcx]
       jmp       near ptr M00_L47
M00_L43:
       vucomisd  xmm1,qword ptr [7FF9778B2030]
       jp        short M00_L44
       jne       short M00_L44
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977835740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0F4CD8014F8
       mov       rbx,[rcx]
       jmp       near ptr M00_L47
M00_L44:
       vucomisd  xmm1,qword ptr [7FF9778B2038]
       jp        short M00_L45
       jne       short M00_L45
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977835740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0F4CD801500
       mov       rbx,[rcx]
       jmp       short M00_L47
M00_L45:
       vmovsd    qword ptr [rsp+38],xmm1
       vucomisd  xmm1,qword ptr [7FF9778B2040]
       jp        short M00_L46
       jne       short M00_L46
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977835740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0F4CD8014B0
       mov       rbx,[rcx]
       jmp       short M00_L47
M00_L46:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       rcx,rbx
       vmovsd    xmm1,qword ptr [rsp+38]
       call      qword ptr [7FF977C37D50]
M00_L47:
       jmp       near ptr M00_L05
M00_L48:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.Int64Value
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       rcx,rbx
       mov       rdx,[rsp+48]
       call      qword ptr [7FF977C35CF8]
       jmp       near ptr M00_L05
M00_L49:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.UInt64Value
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       rcx,rbx
       mov       rdx,[rsp+48]
       call      qword ptr [7FF977C35D28]
       jmp       near ptr M00_L05
       lea       rcx,[rsp+40]
       call      qword ptr [7FF977C35D40]
       mov       rbx,rax
       jmp       near ptr M00_L05
M00_L50:
       mov       rdx,[rsp+40]
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       short M00_L51
       xor       ebx,ebx
       jmp       near ptr M00_L05
M00_L51:
       cmp       dword ptr [rbp+8],0
       jne       short M00_L52
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0F4CD8014A0
       mov       rbx,[rcx]
       jmp       short M00_L53
M00_L52:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       rcx,rbx
       mov       rdx,rbp
       call      qword ptr [7FF977C37D68]
M00_L53:
       jmp       near ptr M00_L05
M00_L54:
       mov       rcx,rbx
       mov       rax,[rbx]
       mov       rax,[rax+58]
       call      qword ptr [rax]
       mov       rbp,rax
       jmp       near ptr M00_L06
M00_L55:
       mov       eax,1
       jmp       near ptr M00_L10
; Total bytes of code 1973
```
```assembly
; AuroraScript.Runtime.Types.ScriptEnumerator.Value()
       push      rsi
       push      rbx
       sub       rsp,38
       xor       eax,eax
       mov       [rsp+28],rax
       mov       [rsp+30],rax
       mov       rax,rcx
       mov       rbx,rdx
M01_L00:
       mov       ecx,[rax+34]
       cmp       ecx,1
       jne       short M01_L02
       mov       rcx,[rax+40]
       mov       rcx,[rcx+38]
       mov       eax,[rax+5C]
       cmp       eax,[rcx+8]
       jae       near ptr M01_L06
       shl       rax,4
       vmovdqu   xmm0,xmmword ptr [rcx+rax+10]
       vmovdqu   xmmword ptr [rsp+28],xmm0
M01_L01:
       vmovdqu   xmm0,xmmword ptr [rsp+28]
       vmovdqu   xmmword ptr [rbx],xmm0
       mov       rax,rbx
       add       rsp,38
       pop       rbx
       pop       rsi
       ret
M01_L02:
       cmp       ecx,3
       ja        short M01_L03
       mov       ecx,ecx
       lea       rdx,[7FF9778B1310]
       mov       edx,[rdx+rcx*4]
       lea       r8,[M01_L00]
       add       rdx,r8
       jmp       rdx
M01_L03:
       mov       rcx,offset MT_System.InvalidOperationException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       ecx,1AD3
       mov       rdx,7FF977A78948
       call      qword ptr [7FF97783F210]
       mov       rdx,rax
       mov       rcx,rbx
       call      qword ptr [7FF977B37BE8]
       mov       rcx,rbx
       call      CORINFO_HELP_THROW
       int       3
       mov       rcx,[rax+48]
       mov       r8d,[rax+5C]
       lea       rdx,[rsp+28]
       mov       rax,[rcx]
       mov       rax,[rax+58]
       call      qword ptr [rax+10]
       jmp       near ptr M01_L01
       mov       rcx,[rax+38]
       mov       eax,[rax+5C]
       cmp       eax,[rcx+8]
       jae       near ptr M01_L06
       shl       rax,4
       vmovdqu   xmm0,xmmword ptr [rcx+rax+10]
       vmovdqu   xmmword ptr [rsp+28],xmm0
       jmp       near ptr M01_L01
       mov       rcx,[rax+50]
       mov       eax,[rax+5C]
       cmp       eax,[rcx+8]
       jae       short M01_L06
       movzx     esi,word ptr [rcx+rax*2+0C]
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0F4CD801498
       mov       rcx,[rcx]
       cmp       [rcx+8],esi
       jg        short M01_L04
       mov       ecx,1
       call      qword ptr [7FF977836670]; System.String.FastAllocateString(IntPtr)
       mov       [rax+0C],si
       jmp       short M01_L05
M01_L04:
       mov       ecx,esi
       call      qword ptr [7FF977C37D08]
       mov       rax,[rax+38]
M01_L05:
       mov       rcx,0F4CA3C0008
       test      rax,rax
       cmove     rax,rcx
       mov       [rsp+28],rax
       mov       qword ptr [rsp+30],4
       jmp       near ptr M01_L01
M01_L06:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 363
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
       mov       rdx,0F4CD800A00
       cmp       rax,[rdx]
       je        near ptr M02_L11
       mov       rdx,[rsp+40]
       mov       rcx,0F4CD800A08
       movsx     rax,word ptr [rsp+48]
       mov       r8d,2000
       cmp       rdx,[rcx]
       cmove     eax,r8d
       jmp       near ptr M02_L01
M02_L08:
       mov       r8,[rsp+30]
       mov       rdx,0F4CD800A00
       cmp       r8,[rdx]
       je        short M02_L13
       mov       rdx,[rsp+30]
       mov       rcx,0F4CD800A08
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
       lea       rcx,[7FF9778B1740]
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
       vmovsd    xmm0,qword ptr [7FF9778B1758]
       jmp       near ptr M02_L03
M02_L18:
       vmovsd    xmm1,qword ptr [7FF9778B1758]
       jmp       near ptr M02_L04
M02_L19:
       mov       rax,[rsp+48]
       cmp       rax,[rsp+38]
       sete      al
       movzx     eax,al
       jmp       near ptr M02_L06
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
       vmovsd    xmm0,qword ptr [7FF9778B1758]
       jmp       short M02_L26
M02_L25:
       vmovq     xmm0,rdx
M02_L26:
       vmovsd    qword ptr [rsp+28],xmm0
       jmp       short M02_L28
M02_L27:
       lea       rcx,[rsp+40]
       lea       rdx,[rsp+28]
       call      qword ptr [7FF977C37D20]
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
       vmovsd    xmm0,qword ptr [7FF9778B1758]
       jmp       short M02_L31
M02_L30:
       vmovq     xmm0,rdx
M02_L31:
       vmovsd    qword ptr [rsp+20],xmm0
       jmp       short M02_L33
M02_L32:
       lea       rcx,[rsp+30]
       lea       rdx,[rsp+20]
       call      qword ptr [7FF977C37D20]
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
; Total bytes of code 1023
```
```assembly
; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rdx,rdx
       je        short M03_L01
       mov       rax,[rdx]
       cmp       rax,rcx
       je        short M03_L01
       mov       rax,[rax+10]
M03_L00:
       cmp       rax,rcx
       jne       short M03_L02
M03_L01:
       mov       rax,rdx
       ret
M03_L02:
       test      rax,rax
       je        short M03_L03
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M03_L01
       test      rax,rax
       je        short M03_L03
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M03_L01
       test      rax,rax
       je        short M03_L03
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M03_L01
       test      rax,rax
       je        short M03_L03
       mov       rax,[rax+10]
       jmp       short M03_L00
M03_L03:
       xor       edx,edx
       jmp       short M03_L01
; Total bytes of code 83
```
```assembly
; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rax,[rcx+20]
       mov       rax,[rax-18]
       mov       rdx,rax
       test      dl,1
       jne       short M04_L00
       ret
M04_L00:
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
       jne       short M06_L00
       mov       rcx,7FF977CA5C74
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rax,rsi
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
M06_L00:
       mov       rcx,7FF977CA5C70
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
       je        short M07_L02
       test      rcx,rcx
       je        short M07_L00
       test      rdx,rdx
       je        short M07_L00
       mov       r8d,[rcx+8]
       cmp       r8d,[rdx+8]
       je        short M07_L01
M07_L00:
       xor       eax,eax
       add       rsp,28
       ret
M07_L01:
       lea       rax,[rcx+0C]
       add       rdx,0C
       mov       r8d,[rcx+8]
       add       r8d,r8d
       mov       rcx,rax
       call      qword ptr [7FF96E84D900]; Precode of System.SpanHelpers.SequenceEqual(Byte ByRef, Byte ByRef, UIntPtr)
       nop
       add       rsp,28
       ret
M07_L02:
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
       push      r14
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,70
       xor       eax,eax
       mov       [rsp+28],rax
       vxorps    xmm4,xmm4,xmm4
       vmovdqu   ymmword ptr [rsp+30],ymm4
       vmovdqu   ymmword ptr [rsp+50],ymm4
M00_L00:
       mov       rdx,[rcx+20]
       mov       rbx,[rcx+28]
       mov       rsi,[rcx+70]
       mov       rdi,[rcx+78]
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rsp+50],xmm0
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rsp+60],xmm0
       test      rdx,rdx
       jne       near ptr M00_L11
       test      rbx,rbx
       je        near ptr M00_L13
       lea       rcx,[rbx-1]
       mov       eax,1
       mov       r8d,2
       cmp       rcx,1
       cmova     eax,r8d
M00_L01:
       cmp       eax,4
       je        near ptr M00_L14
M00_L02:
       mov       [rsp+40],rdx
       mov       [rsp+48],rbx
       cmp       qword ptr [rsp+40],0
       jne       near ptr M00_L12
       mov       rcx,[rsp+48]
       test      rcx,rcx
       je        near ptr M00_L27
       dec       rcx
       mov       eax,1
       mov       edx,2
       cmp       rcx,1
       cmova     eax,edx
M00_L03:
       cmp       eax,4
       jbe       near ptr M00_L26
       cmp       eax,1000
       je        near ptr M00_L48
       cmp       eax,2000
       je        near ptr M00_L49
       mov       rdx,[rsp+40]
       mov       rbx,rdx
       test      rbx,rbx
       je        short M00_L04
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt32Array
       cmp       [rbx],rcx
       jne       near ptr M00_L28
M00_L04:
       test      rbx,rbx
       je        near ptr M00_L50
M00_L05:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt32Array
       cmp       [rbx],rcx
       jne       near ptr M00_L54
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptEnumerator
       call      CORINFO_HELP_NEWSFAST
       mov       rbp,rax
       mov       rcx,0DCE8266C60
       mov       [rbp+10],rcx
       mov       rcx,0BCB5800A28
       mov       rdx,[rcx]
       lea       rcx,[rbp+20]
       call      CORINFO_HELP_ASSIGN_REF
       mov       rcx,0BCB5800A30
       mov       rdx,[rcx]
       lea       rcx,[rbp+8]
       call      CORINFO_HELP_ASSIGN_REF
       mov       dword ptr [rbp+34],2
       lea       rcx,[rbp+48]
       mov       rdx,rbx
       call      CORINFO_HELP_ASSIGN_REF
       mov       rcx,[rbx+38]
       mov       ecx,[rcx+8]
       mov       [rbp+58],ecx
       xor       ecx,ecx
       mov       [rbp+5C],ecx
M00_L06:
       mov       edx,[rbp+5C]
       cmp       edx,[rbp+58]
       jge       short M00_L08
M00_L07:
       lea       rdx,[rsp+60]
       mov       rcx,rbp
       call      qword ptr [7FF977C45DD0]; AuroraScript.Runtime.Types.ScriptEnumerator.Value()
       inc       dword ptr [rbp+5C]
       mov       [rsp+28],rsi
       mov       [rsp+30],rdi
       lea       rdx,[rsp+28]
       lea       rcx,[rsp+60]
       call      qword ptr [7FF977C45C50]; AuroraScript.Runtime.ScriptDatum.Equals(AuroraScript.Runtime.ScriptDatum)
       test      eax,eax
       jne       near ptr M00_L55
       mov       eax,[rbp+5C]
       cmp       eax,[rbp+58]
       jl        short M00_L07
M00_L08:
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rsp+60],xmm0
M00_L09:
       xor       eax,eax
M00_L10:
       add       rsp,70
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       ret
M00_L11:
       mov       rax,0BCB5800A00
       cmp       rdx,[rax]
       je        near ptr M00_L02
       mov       rcx,0BCB5800A08
       cmp       rdx,[rcx]
       je        near ptr M00_L02
       movsx     rax,bx
       jmp       near ptr M00_L01
M00_L12:
       mov       rax,[rsp+40]
       mov       rcx,0BCB5800A00
       cmp       rax,[rcx]
       je        near ptr M00_L25
       mov       rcx,[rsp+40]
       mov       rax,0BCB5800A08
       movsx     rdx,word ptr [rsp+48]
       mov       r8d,2000
       cmp       rcx,[rax]
       mov       eax,r8d
       cmovne    eax,edx
       jmp       near ptr M00_L03
M00_L13:
       xor       eax,eax
       jmp       near ptr M00_L01
M00_L14:
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbx,rax
       mov       rdx,0DCE8260008
       test      rbx,rbx
       cmove     rbx,rdx
       mov       rbp,rbx
       test      rsi,rsi
       je        short M00_L15
       mov       rcx,0BCB5800A00
       cmp       rsi,[rcx]
       je        near ptr M00_L19
       mov       rdx,0BCB5800A08
       cmp       rsi,[rdx]
       je        short M00_L19
       movsx     rcx,di
       jmp       short M00_L18
M00_L15:
       test      rdi,rdi
       je        short M00_L17
       lea       rdx,[rdi-1]
       mov       ecx,1
       mov       eax,2
       cmp       rdx,1
       cmova     ecx,eax
M00_L16:
       jmp       short M00_L18
M00_L17:
       xor       ecx,ecx
       jmp       short M00_L16
M00_L18:
       cmp       ecx,4
       jne       short M00_L19
       mov       rdx,rsi
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdx,rax
       mov       r9,0DCE8260008
       test      rdx,rdx
       cmove     rdx,r9
       cmp       dword ptr [rdx+8],1
       jle       short M00_L19
       mov       dword ptr [rsp+20],4
       mov       r9d,[rbx+8]
       mov       rcx,rbx
       xor       r8d,r8d
       call      qword ptr [7FF977C47D68]
       not       eax
       shr       eax,1F
       jmp       near ptr M00_L10
M00_L19:
       xor       ebx,ebx
       jmp       near ptr M00_L24
M00_L20:
       movzx     r14d,word ptr [rbp+rbx*2+0C]
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0BCB5801498
       mov       rcx,[rcx]
       cmp       [rcx+8],r14d
       jg        short M00_L21
       mov       ecx,1
       call      qword ptr [7FF977846670]; System.String.FastAllocateString(IntPtr)
       mov       [rax+0C],r14w
       jmp       short M00_L22
M00_L21:
       mov       ecx,r14d
       call      qword ptr [7FF977C47D38]
       mov       rax,[rax+38]
M00_L22:
       test      rax,rax
       jne       short M00_L23
       mov       rax,0DCE8260008
M00_L23:
       mov       [rsp+50],rax
       mov       qword ptr [rsp+58],4
       mov       [rsp+28],rsi
       mov       [rsp+30],rdi
       lea       rdx,[rsp+28]
       lea       rcx,[rsp+50]
       call      qword ptr [7FF977C45C50]; AuroraScript.Runtime.ScriptDatum.Equals(AuroraScript.Runtime.ScriptDatum)
       test      eax,eax
       jne       near ptr M00_L55
       inc       ebx
M00_L24:
       cmp       [rbp+8],ebx
       jg        near ptr M00_L20
       jmp       near ptr M00_L09
M00_L25:
       mov       eax,1000
       jmp       near ptr M00_L03
M00_L26:
       mov       ecx,eax
       lea       rax,[7FF9778C24A0]
       mov       eax,[rax+rcx*4]
       lea       rdx,[M00_L00]
       add       rax,rdx
       jmp       rax
M00_L27:
       xor       eax,eax
       jmp       near ptr M00_L03
M00_L28:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbx,rax
       jmp       near ptr M00_L04
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0BCB5800A50
       mov       rbx,[rcx]
       jmp       near ptr M00_L05
       cmp       qword ptr [rsp+48],2
       je        short M00_L29
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BooleanValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0BCB5801520
       mov       rbx,[rcx]
       jmp       short M00_L30
M00_L29:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BooleanValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0BCB5801518
       mov       rbx,[rcx]
M00_L30:
       jmp       near ptr M00_L05
       mov       rcx,[rsp+48]
       mov       rax,8007FFFFFFFFFFFF
       add       rax,rcx
       cmp       rax,3
       jbe       short M00_L31
       vmovq     xmm1,rcx
       jmp       short M00_L33
M00_L31:
       cmp       rax,2
       jbe       short M00_L32
       vmovsd    xmm1,qword ptr [7FF9778C24B8]
       jmp       short M00_L33
M00_L32:
       vmovq     xmm1,rax
M00_L33:
       vucomisd  xmm1,xmm1
       jp        short M00_L34
       je        short M00_L35
M00_L34:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0BCB5801508
       mov       rbx,[rcx]
       jmp       near ptr M00_L47
M00_L35:
       vxorps    xmm0,xmm0,xmm0
       vucomisd  xmm1,xmm0
       jp        short M00_L36
       jne       short M00_L36
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0BCB58014B8
       mov       rbx,[rcx]
       jmp       near ptr M00_L47
M00_L36:
       vucomisd  xmm1,qword ptr [7FF9778C24C0]
       jp        short M00_L37
       jne       short M00_L37
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0BCB58014C0
       mov       rbx,[rcx]
       jmp       near ptr M00_L47
M00_L37:
       vucomisd  xmm1,qword ptr [7FF9778C24C8]
       jp        short M00_L38
       jne       short M00_L38
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0BCB58014C8
       mov       rbx,[rcx]
       jmp       near ptr M00_L47
M00_L38:
       vucomisd  xmm1,qword ptr [7FF9778C24D0]
       jp        short M00_L39
       jne       short M00_L39
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0BCB58014D0
       mov       rbx,[rcx]
       jmp       near ptr M00_L47
M00_L39:
       vucomisd  xmm1,qword ptr [7FF9778C24D8]
       jp        short M00_L40
       jne       short M00_L40
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0BCB58014D8
       mov       rbx,[rcx]
       jmp       near ptr M00_L47
M00_L40:
       vucomisd  xmm1,qword ptr [7FF9778C24E0]
       jp        short M00_L41
       jne       short M00_L41
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0BCB58014E0
       mov       rbx,[rcx]
       jmp       near ptr M00_L47
M00_L41:
       vucomisd  xmm1,qword ptr [7FF9778C24E8]
       jp        short M00_L42
       jne       short M00_L42
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0BCB58014E8
       mov       rbx,[rcx]
       jmp       near ptr M00_L47
M00_L42:
       vucomisd  xmm1,qword ptr [7FF9778C24F0]
       jp        short M00_L43
       jne       short M00_L43
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0BCB58014F0
       mov       rbx,[rcx]
       jmp       near ptr M00_L47
M00_L43:
       vucomisd  xmm1,qword ptr [7FF9778C24F8]
       jp        short M00_L44
       jne       short M00_L44
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0BCB58014F8
       mov       rbx,[rcx]
       jmp       near ptr M00_L47
M00_L44:
       vucomisd  xmm1,qword ptr [7FF9778C2500]
       jp        short M00_L45
       jne       short M00_L45
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0BCB5801500
       mov       rbx,[rcx]
       jmp       short M00_L47
M00_L45:
       vmovsd    qword ptr [rsp+38],xmm1
       vucomisd  xmm1,qword ptr [7FF9778C2508]
       jp        short M00_L46
       jne       short M00_L46
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0BCB58014B0
       mov       rbx,[rcx]
       jmp       short M00_L47
M00_L46:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       rcx,rbx
       vmovsd    xmm1,qword ptr [rsp+38]
       call      qword ptr [7FF977C47D80]
M00_L47:
       jmp       near ptr M00_L05
M00_L48:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.Int64Value
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       rcx,rbx
       mov       rdx,[rsp+48]
       call      qword ptr [7FF977C45D10]
       jmp       near ptr M00_L05
M00_L49:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.UInt64Value
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       rcx,rbx
       mov       rdx,[rsp+48]
       call      qword ptr [7FF977C45D40]
       jmp       near ptr M00_L05
       lea       rcx,[rsp+40]
       call      qword ptr [7FF977C45D58]
       mov       rbx,rax
       jmp       near ptr M00_L05
M00_L50:
       mov       rdx,[rsp+40]
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       short M00_L51
       xor       ebx,ebx
       jmp       near ptr M00_L05
M00_L51:
       cmp       dword ptr [rbp+8],0
       jne       short M00_L52
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0BCB58014A0
       mov       rbx,[rcx]
       jmp       short M00_L53
M00_L52:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       rcx,rbx
       mov       rdx,rbp
       call      qword ptr [7FF977C47D98]
M00_L53:
       jmp       near ptr M00_L05
M00_L54:
       mov       rcx,rbx
       mov       rax,[rbx]
       mov       rax,[rax+58]
       call      qword ptr [rax]
       mov       rbp,rax
       jmp       near ptr M00_L06
M00_L55:
       mov       eax,1
       jmp       near ptr M00_L10
; Total bytes of code 1977
```
```assembly
; AuroraScript.Runtime.Types.ScriptEnumerator.Value()
       push      rsi
       push      rbx
       sub       rsp,38
       xor       eax,eax
       mov       [rsp+28],rax
       mov       [rsp+30],rax
       mov       rbx,rdx
M01_L00:
       mov       eax,[rcx+34]
       cmp       eax,2
       jne       short M01_L04
       mov       rax,[rcx+48]
       mov       r8d,[rcx+5C]
       mov       rdx,offset MT_AuroraScript.Runtime.Types.ScriptInt32Array
       cmp       [rax],rdx
       jne       near ptr M01_L06
       mov       rdx,[rax+38]
       cmp       r8d,[rdx+8]
       jae       near ptr M01_L09
       mov       eax,r8d
       mov       edx,[rdx+rax*4+10]
       test      edx,edx
       je        short M01_L03
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2sd xmm0,xmm0,edx
       vmovq     rdx,xmm0
M01_L01:
       xor       ecx,ecx
       mov       [rsp+28],rcx
       mov       [rsp+30],rdx
M01_L02:
       mov       rax,[rsp+28]
       mov       [rbx],rax
       mov       rax,[rsp+30]
       mov       [rbx+8],rax
       mov       rax,rbx
       add       rsp,38
       pop       rbx
       pop       rsi
       ret
M01_L03:
       mov       rdx,7FF8000000000001
       jmp       short M01_L01
M01_L04:
       cmp       eax,3
       ja        short M01_L05
       mov       eax,eax
       lea       rdx,[7FF9778C1698]
       mov       edx,[rdx+rax*4]
       lea       r8,[M01_L00]
       add       rdx,r8
       jmp       rdx
M01_L05:
       mov       rcx,offset MT_System.InvalidOperationException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       ecx,1AD3
       mov       rdx,7FF977A88948
       call      qword ptr [7FF97784F210]
       mov       rdx,rax
       mov       rcx,rbx
       call      qword ptr [7FF977B47BE8]
       mov       rcx,rbx
       call      CORINFO_HELP_THROW
       int       3
       mov       rax,[rcx+40]
       mov       rax,[rax+38]
       mov       ecx,[rcx+5C]
       cmp       ecx,[rax+8]
       jae       near ptr M01_L09
       shl       rcx,4
       vmovdqu   xmm0,xmmword ptr [rax+rcx+10]
       vmovdqu   xmmword ptr [rsp+28],xmm0
       jmp       near ptr M01_L02
M01_L06:
       lea       rdx,[rsp+28]
       mov       rcx,rax
       mov       rax,[rax]
       mov       rax,[rax+58]
       call      qword ptr [rax+10]
       jmp       near ptr M01_L02
       mov       rax,[rcx+38]
       mov       ecx,[rcx+5C]
       cmp       ecx,[rax+8]
       jae       near ptr M01_L09
       shl       rcx,4
       vmovdqu   xmm0,xmmword ptr [rax+rcx+10]
       vmovdqu   xmmword ptr [rsp+28],xmm0
       jmp       near ptr M01_L02
       mov       rax,[rcx+50]
       mov       ecx,[rcx+5C]
       cmp       ecx,[rax+8]
       jae       short M01_L09
       movzx     esi,word ptr [rax+rcx*2+0C]
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0BCB5801498
       mov       rcx,[rcx]
       cmp       [rcx+8],esi
       jg        short M01_L07
       mov       ecx,1
       call      qword ptr [7FF977846670]; System.String.FastAllocateString(IntPtr)
       mov       [rax+0C],si
       jmp       short M01_L08
M01_L07:
       mov       ecx,esi
       call      qword ptr [7FF977C47D38]
       mov       rax,[rax+38]
M01_L08:
       mov       rdx,0DCE8260008
       test      rax,rax
       cmove     rax,rdx
       mov       [rsp+28],rax
       mov       qword ptr [rsp+30],4
       jmp       near ptr M01_L02
M01_L09:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 456
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
       je        near ptr M02_L14
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
       je        near ptr M02_L16
       dec       rdx
       mov       r8d,1
       mov       ecx,2
       cmp       rdx,1
       cmova     r8d,ecx
M02_L02:
       cmp       eax,r8d
       jne       near ptr M02_L21
       cmp       r8d,2
       jne       near ptr M02_L09
       mov       rax,[rsp+48]
       mov       rcx,8007FFFFFFFFFFFF
       add       rcx,rax
       cmp       rcx,3
       jbe       near ptr M02_L11
       vmovq     xmm0,rax
M02_L03:
       mov       rcx,[rsp+38]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       near ptr M02_L12
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
       mov       rdx,0BCB5800A00
       cmp       rax,[rdx]
       je        near ptr M02_L13
       mov       rdx,[rsp+40]
       mov       rcx,0BCB5800A08
       movsx     rax,word ptr [rsp+48]
       mov       r8d,2000
       cmp       rdx,[rcx]
       cmove     eax,r8d
       jmp       near ptr M02_L01
M02_L08:
       mov       r8,[rsp+30]
       mov       rdx,0BCB5800A00
       cmp       r8,[rdx]
       je        near ptr M02_L15
       mov       rdx,[rsp+30]
       mov       rcx,0BCB5800A08
       movsx     r8,word ptr [rsp+38]
       mov       r10d,2000
       cmp       rdx,[rcx]
       cmove     r8d,r10d
       jmp       near ptr M02_L02
M02_L09:
       cmp       r8d,4
       ja        short M02_L17
       mov       edx,r8d
       lea       rcx,[7FF9778C1C00]
       mov       ecx,[rcx+rdx*4]
       lea       rax,[M02_L00]
       add       rcx,rax
       jmp       rcx
M02_L10:
       mov       rax,[rsp+40]
       cmp       rax,[rsp+30]
       sete      al
       movzx     eax,al
       jmp       near ptr M02_L06
M02_L11:
       cmp       rcx,2
       ja        near ptr M02_L18
       vmovq     xmm0,rcx
       jmp       near ptr M02_L03
M02_L12:
       cmp       rdx,2
       ja        near ptr M02_L19
       vmovq     xmm1,rdx
       jmp       near ptr M02_L04
M02_L13:
       mov       eax,1000
       jmp       near ptr M02_L01
M02_L14:
       xor       eax,eax
       jmp       near ptr M02_L01
M02_L15:
       mov       r8d,1000
       jmp       near ptr M02_L02
M02_L16:
       xor       r8d,r8d
       jmp       near ptr M02_L02
M02_L17:
       cmp       r8d,1000
       je        short M02_L20
       cmp       r8d,2000
       jne       short M02_L10
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
M02_L18:
       vmovsd    xmm0,qword ptr [7FF9778C1C18]
       jmp       near ptr M02_L03
M02_L19:
       vmovsd    xmm1,qword ptr [7FF9778C1C18]
       jmp       near ptr M02_L04
M02_L20:
       mov       rax,[rsp+48]
       cmp       rax,[rsp+38]
       sete      al
       movzx     eax,al
       jmp       near ptr M02_L06
       mov       rdx,[rsp+40]
       mov       rcx,offset MT_System.String
       call      qword ptr [7FF977846850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbx,rax
       mov       rdx,[rsp+30]
       mov       rcx,offset MT_System.String
       call      qword ptr [7FF977846850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdx,rax
       mov       rcx,rbx
       call      qword ptr [7FF977846E08]; System.String.Equals(System.String, System.String)
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
       vmovsd    xmm0,qword ptr [7FF9778C1C18]
       jmp       short M02_L26
M02_L25:
       vmovq     xmm0,rdx
M02_L26:
       vmovsd    qword ptr [rsp+28],xmm0
       jmp       short M02_L28
M02_L27:
       lea       rcx,[rsp+40]
       lea       rdx,[rsp+28]
       call      qword ptr [7FF977C47D50]
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
       vmovsd    xmm0,qword ptr [7FF9778C1C18]
       jmp       short M02_L31
M02_L30:
       vmovq     xmm0,rdx
M02_L31:
       vmovsd    qword ptr [rsp+20],xmm0
       jmp       short M02_L33
M02_L32:
       lea       rcx,[rsp+30]
       lea       rdx,[rsp+20]
       call      qword ptr [7FF977C47D50]
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
; Total bytes of code 1023
```
```assembly
; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rdx,rdx
       je        short M03_L01
       mov       rax,[rdx]
       cmp       rax,rcx
       je        short M03_L01
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M03_L01
M03_L00:
       test      rax,rax
       je        short M03_L03
       mov       rax,[rax+10]
       cmp       rax,rcx
       jne       short M03_L02
M03_L01:
       mov       rax,rdx
       ret
M03_L02:
       test      rax,rax
       je        short M03_L03
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M03_L01
       test      rax,rax
       je        short M03_L03
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M03_L01
       test      rax,rax
       je        short M03_L03
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M03_L01
       jmp       short M03_L00
M03_L03:
       xor       edx,edx
       jmp       short M03_L01
; Total bytes of code 88
```
```assembly
; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rax,[rcx+20]
       mov       rax,[rax-18]
       mov       rdx,rax
       test      dl,1
       jne       short M04_L00
       ret
M04_L00:
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
       push      rsi
       push      rbx
       sub       rsp,28
       mov       rbx,rcx
       mov       rcx,[rbx+20]
       mov       rsi,[rcx-10]
       mov       rcx,rsi
       test      cl,1
       jne       short M06_L00
       mov       rcx,7FF977CB5C74
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rax,rsi
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
M06_L00:
       mov       rcx,7FF977CB5C70
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rcx,rbx
       add       rsp,28
       pop       rbx
       pop       rsi
       jmp       qword ptr [7FF977A1E838]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBaseSlow(System.Runtime.CompilerServices.MethodTable*)
; Total bytes of code 80
```
```assembly
; System.String.Equals(System.String, System.String)
       sub       rsp,28
       cmp       rcx,rdx
       je        short M07_L02
       test      rcx,rcx
       je        short M07_L00
       test      rdx,rdx
       je        short M07_L00
       mov       r8d,[rcx+8]
       cmp       r8d,[rdx+8]
       je        short M07_L01
M07_L00:
       xor       eax,eax
       add       rsp,28
       ret
M07_L01:
       lea       rax,[rcx+0C]
       add       rdx,0C
       mov       r8d,[rcx+8]
       add       r8d,r8d
       mov       rcx,rax
       call      qword ptr [7FF96E84D900]; Precode of System.SpanHelpers.SequenceEqual(Byte ByRef, Byte ByRef, UIntPtr)
       nop
       add       rsp,28
       ret
M07_L02:
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
       sub       rsp,70
       xor       eax,eax
       mov       [rsp+28],rax
       vxorps    xmm4,xmm4,xmm4
       vmovdqu   ymmword ptr [rsp+30],ymm4
       vmovdqu   ymmword ptr [rsp+50],ymm4
M00_L00:
       mov       rbx,[rcx+30]
       mov       rsi,[rcx+38]
       mov       rdi,[rcx+40]
       mov       rbp,[rcx+48]
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rsp+50],xmm0
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rsp+60],xmm0
       test      rbx,rbx
       je        near ptr M00_L13
       mov       r9,8001000A00
       cmp       rbx,[r9]
       je        near ptr M00_L22
       mov       r9,8001000A08
       cmp       rbx,[r9]
       je        near ptr M00_L22
       movsx     r9,si
M00_L01:
       cmp       r9d,4
       jne       near ptr M00_L22
       test      rbx,rbx
       je        short M00_L02
       mov       r9,offset MT_System.String
       cmp       [rbx],r9
       jne       short M00_L02
       mov       rcx,rbx
       jmp       short M00_L03
M00_L02:
       xor       ecx,ecx
M00_L03:
       mov       r9,0A033D20008
       test      rcx,rcx
       cmove     rcx,r9
       mov       rbx,rcx
       test      rdi,rdi
       je        near ptr M00_L16
       mov       r9,8001000A00
       cmp       rdi,[r9]
       je        short M00_L07
       mov       r9,8001000A08
       cmp       rdi,[r9]
       je        short M00_L07
       movsx     r9,bp
M00_L04:
       cmp       r9d,4
       jne       short M00_L07
       test      rdi,rdi
       je        short M00_L05
       mov       r9,offset MT_System.String
       cmp       [rdi],r9
       jne       short M00_L05
       mov       rdx,rdi
       jmp       short M00_L06
M00_L05:
       xor       edx,edx
M00_L06:
       mov       r9,0A033D20008
       test      rdx,rdx
       cmove     rdx,r9
       cmp       dword ptr [rdx+8],1
       jg        near ptr M00_L19
M00_L07:
       mov       esi,[rbx+8]
       test      esi,esi
       jle       short M00_L11
       add       rbx,0C
M00_L08:
       movzx     r14d,word ptr [rbx]
       cmp       r14d,100
       jl        near ptr M00_L20
       mov       rcx,offset MT_System.String
       mov       edx,1
       call      00007FF9D74C4E80
       mov       [rax+0C],r14w
M00_L09:
       test      rax,rax
       je        near ptr M00_L21
M00_L10:
       mov       [rsp+50],rax
       mov       qword ptr [rsp+58],4
       mov       [rsp+28],rdi
       mov       [rsp+30],rbp
       lea       rdx,[rsp+28]
       lea       rcx,[rsp+50]
       call      qword ptr [7FF977C55C38]; AuroraScript.Runtime.ScriptDatum.Equals(AuroraScript.Runtime.ScriptDatum)
       test      eax,eax
       jne       near ptr M00_L52
       add       rbx,2
       dec       esi
       jne       short M00_L08
M00_L11:
       xor       eax,eax
M00_L12:
       add       rsp,70
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       ret
M00_L13:
       test      rsi,rsi
       je        short M00_L15
       lea       r9,[rsi-1]
       mov       ecx,1
       mov       edx,2
       cmp       r9,1
       cmova     ecx,edx
       mov       r9d,ecx
M00_L14:
       jmp       near ptr M00_L01
M00_L15:
       xor       r9d,r9d
       jmp       short M00_L14
M00_L16:
       test      rbp,rbp
       je        short M00_L18
       lea       r9,[rbp-1]
       mov       edx,1
       mov       r8d,2
       cmp       r9,1
       cmova     edx,r8d
       mov       r9d,edx
M00_L17:
       jmp       near ptr M00_L04
M00_L18:
       xor       r9d,r9d
       jmp       short M00_L17
M00_L19:
       mov       dword ptr [rsp+20],4
       mov       r9d,[rcx+8]
       mov       esi,r9d
       mov       r9d,esi
       xor       r8d,r8d
       call      qword ptr [7FF977C5C048]
       not       eax
       shr       eax,1F
       jmp       short M00_L12
M00_L20:
       mov       ecx,r14d
       call      qword ptr [7FF977C55C98]
       mov       rax,[rax+38]
       jmp       near ptr M00_L09
M00_L21:
       mov       rax,0A033D20008
       jmp       near ptr M00_L10
M00_L22:
       mov       [rsp+40],rbx
       mov       [rsp+48],rsi
       cmp       qword ptr [rsp+40],0
       je        short M00_L25
       mov       rcx,[rsp+40]
       mov       rax,8001000A00
       cmp       rcx,[rax]
       jne       short M00_L23
       mov       edx,1000
       jmp       short M00_L24
M00_L23:
       mov       rcx,[rsp+40]
       mov       rax,8001000A08
       movsx     rdx,word ptr [rsp+48]
       mov       r8d,2000
       cmp       rcx,[rax]
       cmove     edx,r8d
M00_L24:
       cmp       edx,4
       ja        short M00_L28
       mov       ecx,edx
       lea       rax,[7FF9778D32F8]
       mov       eax,[rax+rcx*4]
       lea       rdx,[M00_L00]
       add       rax,rdx
       jmp       rax
M00_L25:
       mov       rdx,[rsp+48]
       test      rdx,rdx
       je        short M00_L27
       dec       rdx
       mov       ecx,1
       mov       eax,2
       cmp       rdx,1
       cmova     ecx,eax
       mov       edx,ecx
M00_L26:
       jmp       short M00_L24
M00_L27:
       xor       edx,edx
       jmp       short M00_L26
M00_L28:
       cmp       edx,1000
       je        near ptr M00_L48
       cmp       edx,2000
       jne       near ptr M00_L49
       mov       rcx,offset MT_AuroraScript.Runtime.Types.UInt64Value
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       rcx,rbx
       mov       rdx,[rsp+48]
       call      qword ptr [7FF977C5C060]
       mov       rax,rbx
       jmp       near ptr M00_L50
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001000A50
       mov       rax,[rcx]
       jmp       near ptr M00_L50
       cmp       qword ptr [rsp+48],2
       je        short M00_L29
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BooleanValue
       call      qword ptr [7FF977855740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80010015E0
       mov       rax,[rcx]
       jmp       short M00_L30
M00_L29:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BooleanValue
       call      qword ptr [7FF977855740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80010015D8
       mov       rax,[rcx]
M00_L30:
       jmp       near ptr M00_L50
       mov       rcx,[rsp+48]
       mov       rax,8007FFFFFFFFFFFF
       add       rax,rcx
       cmp       rax,3
       jbe       short M00_L31
       vmovq     xmm1,rcx
       jmp       short M00_L33
M00_L31:
       cmp       rax,2
       jbe       short M00_L32
       vmovsd    xmm1,qword ptr [7FF9778D3310]
       jmp       short M00_L33
M00_L32:
       vmovq     xmm1,rax
M00_L33:
       vucomisd  xmm1,xmm1
       jp        short M00_L34
       je        short M00_L35
M00_L34:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977855740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80010015C8
       mov       rax,[rcx]
       jmp       near ptr M00_L47
M00_L35:
       vxorps    xmm0,xmm0,xmm0
       vucomisd  xmm1,xmm0
       jp        short M00_L36
       jne       short M00_L36
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977855740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001001578
       mov       rax,[rcx]
       jmp       near ptr M00_L47
M00_L36:
       vucomisd  xmm1,qword ptr [7FF9778D3318]
       jp        short M00_L37
       jne       short M00_L37
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977855740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001001580
       mov       rax,[rcx]
       jmp       near ptr M00_L47
M00_L37:
       vucomisd  xmm1,qword ptr [7FF9778D3320]
       jp        short M00_L38
       jne       short M00_L38
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977855740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001001588
       mov       rax,[rcx]
       jmp       near ptr M00_L47
M00_L38:
       vucomisd  xmm1,qword ptr [7FF9778D3328]
       jp        short M00_L39
       jne       short M00_L39
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977855740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001001590
       mov       rax,[rcx]
       jmp       near ptr M00_L47
M00_L39:
       vucomisd  xmm1,qword ptr [7FF9778D3330]
       jp        short M00_L40
       jne       short M00_L40
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977855740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001001598
       mov       rax,[rcx]
       jmp       near ptr M00_L47
M00_L40:
       vucomisd  xmm1,qword ptr [7FF9778D3338]
       jp        short M00_L41
       jne       short M00_L41
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977855740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80010015A0
       mov       rax,[rcx]
       jmp       near ptr M00_L47
M00_L41:
       vucomisd  xmm1,qword ptr [7FF9778D3340]
       jp        short M00_L42
       jne       short M00_L42
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977855740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80010015A8
       mov       rax,[rcx]
       jmp       near ptr M00_L47
M00_L42:
       vucomisd  xmm1,qword ptr [7FF9778D3348]
       jp        short M00_L43
       jne       short M00_L43
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977855740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80010015B0
       mov       rax,[rcx]
       jmp       near ptr M00_L47
M00_L43:
       vucomisd  xmm1,qword ptr [7FF9778D3350]
       jp        short M00_L44
       jne       short M00_L44
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977855740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80010015B8
       mov       rax,[rcx]
       jmp       near ptr M00_L47
M00_L44:
       vucomisd  xmm1,qword ptr [7FF9778D3358]
       jp        short M00_L45
       jne       short M00_L45
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977855740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80010015C0
       mov       rax,[rcx]
       jmp       short M00_L47
M00_L45:
       vmovsd    qword ptr [rsp+38],xmm1
       vucomisd  xmm1,qword ptr [7FF9778D3360]
       jp        short M00_L46
       jne       short M00_L46
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977855740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001001570
       mov       rax,[rcx]
       jmp       short M00_L47
M00_L46:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       rcx,rbx
       vmovsd    xmm1,qword ptr [rsp+38]
       call      qword ptr [7FF977C5C078]
       mov       rax,rbx
M00_L47:
       jmp       short M00_L50
M00_L48:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.Int64Value
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       rcx,rbx
       mov       rdx,[rsp+48]
       call      qword ptr [7FF977C5C090]
       mov       rax,rbx
       jmp       short M00_L50
       lea       rcx,[rsp+40]
       call      qword ptr [7FF977C5C0A8]
       jmp       short M00_L50
M00_L49:
       lea       rcx,[rsp+40]
       call      qword ptr [7FF977C5C0C0]
M00_L50:
       mov       rcx,rax
       mov       rax,[rax]
       mov       rax,[rax+58]
       call      qword ptr [rax]
       mov       rbx,rax
M00_L51:
       lea       rdx,[rsp+60]
       mov       rcx,rbx
       cmp       [rcx],ecx
       call      qword ptr [7FF977C55C68]
       test      eax,eax
       je        near ptr M00_L11
       mov       [rsp+28],rdi
       mov       [rsp+30],rbp
       lea       rdx,[rsp+28]
       lea       rcx,[rsp+60]
       call      qword ptr [7FF977C55C38]; AuroraScript.Runtime.ScriptDatum.Equals(AuroraScript.Runtime.ScriptDatum)
       test      eax,eax
       je        short M00_L51
M00_L52:
       mov       eax,1
       jmp       near ptr M00_L12
; Total bytes of code 1652
```
```assembly
; AuroraScript.Runtime.ScriptDatum.Equals(AuroraScript.Runtime.ScriptDatum)
       sub       rsp,58
       xor       eax,eax
       mov       [rsp+28],rax
       vxorps    xmm4,xmm4,xmm4
       vmovdqu   ymmword ptr [rsp+30],ymm4
       mov       [rsp+50],rax
M01_L00:
       vmovdqu   xmm0,xmmword ptr [rdx]
       vmovdqu   xmmword ptr [rsp+48],xmm0
       vmovdqu   xmm0,xmmword ptr [rcx]
       vmovdqu   xmmword ptr [rsp+38],xmm0
       cmp       qword ptr [rsp+48],0
       je        near ptr M01_L11
       mov       r8,[rsp+48]
       mov       rdx,8001000A00
       cmp       r8,[rdx]
       je        near ptr M01_L10
       mov       r8,[rsp+48]
       mov       rdx,8001000A08
       movsx     rax,word ptr [rsp+50]
       mov       ecx,2000
       cmp       r8,[rdx]
       cmove     eax,ecx
M01_L01:
       cmp       qword ptr [rsp+38],0
       je        near ptr M01_L15
       mov       r8,[rsp+38]
       mov       rdx,8001000A00
       cmp       r8,[rdx]
       je        near ptr M01_L14
       mov       r8,[rsp+38]
       mov       rdx,8001000A08
       movsx     r10,word ptr [rsp+40]
       mov       ecx,2000
       cmp       r8,[rdx]
       cmove     r10d,ecx
M01_L02:
       cmp       eax,r10d
       jne       near ptr M01_L29
       cmp       r10d,4
       jne       near ptr M01_L18
       mov       r8,[rsp+48]
       test      r8,r8
       je        short M01_L03
       mov       rdx,offset MT_System.String
       cmp       [r8],rdx
       jne       short M01_L03
       jmp       short M01_L04
M01_L03:
       xor       r8d,r8d
M01_L04:
       mov       rdx,[rsp+38]
       test      rdx,rdx
       je        short M01_L05
       mov       rcx,offset MT_System.String
       cmp       [rdx],rcx
       jne       short M01_L05
       jmp       short M01_L06
M01_L05:
       xor       edx,edx
M01_L06:
       cmp       r8,rdx
       je        short M01_L08
       test      r8,r8
       je        short M01_L07
       test      rdx,rdx
       je        short M01_L07
       mov       ecx,[r8+8]
       cmp       ecx,[rdx+8]
       jne       short M01_L07
       lea       rcx,[r8+0C]
       mov       r8d,[r8+8]
       add       r8d,r8d
       add       rdx,0C
       call      qword ptr [7FF97785C330]; System.SpanHelpers.SequenceEqual(Byte ByRef, Byte ByRef, UIntPtr)
       jmp       short M01_L09
M01_L07:
       xor       eax,eax
       jmp       short M01_L09
M01_L08:
       mov       eax,1
M01_L09:
       movzx     eax,al
       add       rsp,58
       ret
M01_L10:
       mov       eax,1000
       jmp       near ptr M01_L01
M01_L11:
       mov       rax,[rsp+50]
       test      rax,rax
       je        short M01_L13
       dec       rax
       mov       r8d,1
       mov       edx,2
       cmp       rax,1
       cmova     r8d,edx
       mov       eax,r8d
M01_L12:
       jmp       near ptr M01_L01
M01_L13:
       xor       eax,eax
       jmp       short M01_L12
M01_L14:
       mov       r10d,1000
       jmp       near ptr M01_L02
M01_L15:
       mov       r10,[rsp+40]
       test      r10,r10
       je        short M01_L17
       dec       r10
       mov       r8d,1
       mov       edx,2
       cmp       r10,1
       cmova     r8d,edx
       mov       r10d,r8d
M01_L16:
       jmp       near ptr M01_L02
M01_L17:
       xor       r10d,r10d
       jmp       short M01_L16
M01_L18:
       cmp       r10d,4
       ja        short M01_L19
       mov       r8d,r10d
       lea       rdx,[7FF9778D2A30]
       mov       edx,[rdx+r8*4]
       lea       rcx,[M01_L00]
       add       rdx,rcx
       jmp       rdx
M01_L19:
       cmp       r10d,1000
       je        near ptr M01_L27
       cmp       r10d,2000
       jne       near ptr M01_L28
       mov       rax,[rsp+50]
       cmp       rax,[rsp+40]
       sete      al
       movzx     eax,al
       jmp       near ptr M01_L09
       mov       eax,1
       jmp       near ptr M01_L09
       cmp       qword ptr [rsp+50],2
       sete      al
       movzx     eax,al
       cmp       qword ptr [rsp+40],2
       sete      cl
       movzx     ecx,cl
       cmp       eax,ecx
       sete      al
       movzx     eax,al
       jmp       near ptr M01_L09
       mov       rax,[rsp+50]
       mov       rcx,8007FFFFFFFFFFFF
       add       rcx,rax
       cmp       rcx,3
       jbe       short M01_L20
       vmovq     xmm0,rax
       jmp       short M01_L22
M01_L20:
       cmp       rcx,2
       jbe       short M01_L21
       vmovsd    xmm0,qword ptr [7FF9778D2A48]
       jmp       short M01_L22
M01_L21:
       vmovq     xmm0,rcx
M01_L22:
       mov       rcx,[rsp+40]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M01_L23
       vmovq     xmm1,rcx
       jmp       short M01_L25
M01_L23:
       cmp       rdx,2
       jbe       short M01_L24
       vmovsd    xmm1,qword ptr [7FF9778D2A48]
       jmp       short M01_L25
M01_L24:
       vmovq     xmm1,rdx
M01_L25:
       vucomisd  xmm0,xmm1
       setnp     al
       jp        short M01_L26
       sete      al
M01_L26:
       movzx     eax,al
       jmp       near ptr M01_L09
M01_L27:
       mov       rax,[rsp+50]
       cmp       rax,[rsp+40]
       sete      al
       movzx     eax,al
       jmp       near ptr M01_L09
M01_L28:
       mov       rax,[rsp+48]
       cmp       rax,[rsp+38]
       sete      al
       movzx     eax,al
       jmp       near ptr M01_L09
M01_L29:
       cmp       eax,1000
       jne       short M01_L30
       cmp       r10d,2000
       jne       short M01_L30
       cmp       qword ptr [rsp+50],0
       jl        near ptr M01_L43
       mov       rax,[rsp+50]
       cmp       rax,[rsp+40]
       sete      al
       movzx     eax,al
       jmp       near ptr M01_L09
M01_L30:
       cmp       eax,2000
       jne       short M01_L31
       cmp       r10d,1000
       jne       short M01_L31
       cmp       qword ptr [rsp+40],0
       jl        near ptr M01_L43
       mov       rcx,[rsp+50]
       cmp       rcx,[rsp+40]
       sete      al
       movzx     eax,al
       jmp       near ptr M01_L09
M01_L31:
       cmp       qword ptr [rsp+48],0
       jne       short M01_L35
       cmp       qword ptr [rsp+50],2
       jbe       short M01_L35
       mov       rcx,[rsp+50]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M01_L32
       vmovq     xmm0,rcx
       jmp       short M01_L34
M01_L32:
       cmp       rdx,2
       jbe       short M01_L33
       vmovsd    xmm0,qword ptr [7FF9778D2A48]
       jmp       short M01_L34
M01_L33:
       vmovq     xmm0,rdx
M01_L34:
       vmovsd    qword ptr [rsp+30],xmm0
       jmp       short M01_L36
M01_L35:
       lea       rcx,[rsp+48]
       lea       rdx,[rsp+30]
       call      qword ptr [7FF977C5C030]
       test      eax,eax
       je        near ptr M01_L43
M01_L36:
       cmp       qword ptr [rsp+38],0
       jne       short M01_L40
       cmp       qword ptr [rsp+40],2
       jbe       short M01_L40
       mov       rcx,[rsp+40]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M01_L37
       vmovq     xmm0,rcx
       jmp       short M01_L39
M01_L37:
       cmp       rdx,2
       jbe       short M01_L38
       vmovsd    xmm0,qword ptr [7FF9778D2A48]
       jmp       short M01_L39
M01_L38:
       vmovq     xmm0,rdx
M01_L39:
       vmovsd    qword ptr [rsp+28],xmm0
       jmp       short M01_L41
M01_L40:
       lea       rcx,[rsp+38]
       lea       rdx,[rsp+28]
       call      qword ptr [7FF977C5C030]
       test      eax,eax
       je        short M01_L43
M01_L41:
       vmovsd    xmm0,qword ptr [rsp+30]
       vucomisd  xmm0,qword ptr [rsp+28]
       setnp     al
       jp        short M01_L42
       sete      al
M01_L42:
       movzx     eax,al
       jmp       near ptr M01_L09
M01_L43:
       xor       eax,eax
       add       rsp,58
       ret
; Total bytes of code 1056
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
       jmp       qword ptr [7FF977855C38]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBaseSlow(System.Runtime.CompilerServices.MethodTable*)
; Total bytes of code 23
```
```assembly
; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rax,[rcx+20]
       mov       rax,[rax-10]
       mov       rdx,rax
       test      dl,1
       jne       short M03_L00
       ret
M03_L00:
       jmp       qword ptr [7FF977A2E838]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBaseSlow(System.Runtime.CompilerServices.MethodTable*)
; Total bytes of code 23
```
```assembly
; System.SpanHelpers.SequenceEqual(Byte ByRef, Byte ByRef, UIntPtr)
       cmp       r8,8
       jae       short M04_L05
       cmp       r8,4
       jae       short M04_L04
       xor       eax,eax
       mov       r10,r8
       and       r10,2
       je        short M04_L00
       movzx     eax,word ptr [rcx]
       movzx     r9d,word ptr [rdx]
       sub       eax,r9d
M04_L00:
       test      r8b,1
       jne       short M04_L03
M04_L01:
       test      eax,eax
       sete      al
       movzx     eax,al
M04_L02:
       vzeroupper
       ret
M04_L03:
       movzx     r8d,byte ptr [rcx+r10]
       movzx     ecx,byte ptr [rdx+r10]
       sub       r8d,ecx
       or        eax,r8d
       jmp       short M04_L01
M04_L04:
       add       r8,0FFFFFFFFFFFFFFFC
       mov       eax,[rcx]
       sub       eax,[rdx]
       mov       ecx,[rcx+r8]
       sub       ecx,[rdx+r8]
       or        eax,ecx
       sete      al
       movzx     eax,al
       jmp       short M04_L02
M04_L05:
       cmp       rcx,rdx
       je        near ptr M04_L11
       cmp       r8,20
       jb        short M04_L08
       xor       eax,eax
       add       r8,0FFFFFFFFFFFFFFE0
       je        short M04_L07
M04_L06:
       vmovups   ymm0,[rcx+rax]
       vpcmpeqb  ymm0,ymm0,[rdx+rax]
       vpmovmskb r10d,ymm0
       cmp       r10d,0FFFFFFFF
       jne       near ptr M04_L13
       add       rax,20
       cmp       r8,rax
       ja        short M04_L06
M04_L07:
       vmovups   ymm0,[rcx+r8]
       vpcmpeqb  ymm0,ymm0,[rdx+r8]
       vpmovmskb ecx,ymm0
       cmp       ecx,0FFFFFFFF
       jne       short M04_L13
       jmp       short M04_L11
M04_L08:
       cmp       r8,10
       jb        short M04_L12
       xor       eax,eax
       add       r8,0FFFFFFFFFFFFFFF0
       je        short M04_L10
M04_L09:
       vmovups   xmm0,[rcx+rax]
       vpcmpeqb  xmm0,xmm0,[rdx+rax]
       vpmovmskb r10d,xmm0
       cmp       r10d,0FFFF
       jne       short M04_L13
       add       rax,10
       cmp       r8,rax
       ja        short M04_L09
M04_L10:
       vmovups   xmm0,[rcx+r8]
       vpcmpeqb  xmm0,xmm0,[rdx+r8]
       vpmovmskb eax,xmm0
       cmp       eax,0FFFF
       jne       short M04_L13
M04_L11:
       mov       eax,1
       vzeroupper
       ret
M04_L12:
       lea       rax,[r8-8]
       mov       r8,[rcx]
       sub       r8,[rdx]
       mov       rcx,[rcx+rax]
       sub       rcx,[rdx+rax]
       or        rcx,r8
       sete      al
       movzx     eax,al
       jmp       near ptr M04_L02
M04_L13:
       xor       eax,eax
       vzeroupper
       ret
; Total bytes of code 290
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
       push      rsi
       push      rbx
       sub       rsp,48
       xor       eax,eax
       mov       [rsp+28],rax
       vxorps    xmm4,xmm4,xmm4
       vmovdqa   xmmword ptr [rsp+30],xmm4
       mov       [rsp+40],rax
       mov       rbx,[rcx+50]
       mov       r8,[rcx+58]
       test      rbx,rbx
       je        near ptr M00_L10
       mov       rcx,0D504C00A00
       cmp       rbx,[rcx]
       je        short M00_L01
       mov       rcx,0D504C00A08
       cmp       rbx,[rcx]
       je        short M00_L01
       movsx     rcx,r8w
M00_L00:
       test      ecx,ecx
       je        near ptr M00_L22
M00_L01:
       test      rbx,rbx
       je        near ptr M00_L13
       mov       rcx,0D504C00A00
       cmp       rbx,[rcx]
       je        short M00_L03
       mov       rcx,0D504C00A08
       cmp       rbx,[rcx]
       je        short M00_L03
       movsx     rcx,r8w
M00_L02:
       test      ecx,ecx
       je        short M00_L09
M00_L03:
       test      rbx,rbx
       je        short M00_L04
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt32Array
       cmp       [rbx],rcx
       je        short M00_L09
M00_L04:
       mov       [rsp+28],rbx
       mov       [rsp+30],r8
       lea       r8,[rsp+28]
       lea       rcx,[rsp+38]
       mov       edx,6
       call      qword ptr [7FF977C35BC0]
       mov       rbx,[rsp+38]
M00_L05:
       mov       rdx,rbx
       test      rdx,rdx
       je        short M00_L06
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt32Array
       cmp       [rdx],rcx
       jne       short M00_L16
M00_L06:
       test      rdx,rdx
       je        near ptr M00_L17
M00_L07:
       mov       rax,rdx
       test      rax,rax
       je        short M00_L08
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt32Array
       cmp       [rax],rcx
       jne       near ptr M00_L21
M00_L08:
       add       rsp,48
       pop       rbx
       pop       rsi
       ret
M00_L09:
       jmp       short M00_L05
M00_L10:
       test      r8,r8
       je        short M00_L12
       lea       rcx,[r8-1]
       mov       edx,1
       mov       eax,2
       cmp       rcx,1
       cmova     edx,eax
       mov       ecx,edx
M00_L11:
       jmp       near ptr M00_L00
M00_L12:
       xor       ecx,ecx
       jmp       short M00_L11
M00_L13:
       test      r8,r8
       je        short M00_L15
       lea       rcx,[r8-1]
       mov       edx,1
       mov       eax,2
       cmp       rcx,1
       cmova     edx,eax
       mov       ecx,edx
M00_L14:
       jmp       near ptr M00_L02
M00_L15:
       xor       ecx,ecx
       jmp       short M00_L14
M00_L16:
       mov       rdx,rbx
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdx,rax
       jmp       near ptr M00_L06
M00_L17:
       mov       rdx,rbx
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbx,rax
       test      rbx,rbx
       jne       short M00_L18
       xor       edx,edx
       jmp       near ptr M00_L07
M00_L18:
       cmp       dword ptr [rbx+8],0
       jne       short M00_L19
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      qword ptr [7FF977835728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0D504C014A8
       mov       rdx,[rcx]
       jmp       short M00_L20
M00_L19:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      CORINFO_HELP_NEWSFAST
       mov       rsi,rax
       mov       rcx,rsi
       mov       rdx,rbx
       call      qword ptr [7FF977C37A38]
       mov       rdx,rsi
M00_L20:
       jmp       near ptr M00_L07
M00_L21:
       call      System.Runtime.CompilerServices.CastHelpers.ChkCastClass(Void*, System.Object)
       int       3
M00_L22:
       xor       eax,eax
       jmp       near ptr M00_L08
; Total bytes of code 479
```
```assembly
; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rdx,rdx
       je        short M01_L01
       mov       rax,[rdx]
       cmp       rax,rcx
       je        short M01_L01
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M01_L01
M01_L00:
       test      rax,rax
       je        short M01_L03
       mov       rax,[rax+10]
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
       jmp       short M01_L00
M01_L03:
       xor       edx,edx
       jmp       short M01_L01
; Total bytes of code 88
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
       jmp       qword ptr [7FF977835C38]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBaseSlow(System.Runtime.CompilerServices.MethodTable*)
; Total bytes of code 23
```
```assembly
; System.Runtime.CompilerServices.CastHelpers.ChkCastClass(Void*, System.Object)
       test      rdx,rdx
       je        short M03_L00
       cmp       [rdx],rcx
       jne       short M03_L01
M03_L00:
       mov       rax,rdx
       ret
M03_L01:
       jmp       qword ptr [7FF977A04D20]; System.Runtime.CompilerServices.CastHelpers.ChkCastClassSpecial(Void*, System.Object)
; Total bytes of code 20
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
; System.Runtime.CompilerServices.CastHelpers.ChkCastClassSpecial(Void*, System.Object)
       mov       rax,[rdx]
       mov       rax,[rax+10]
       cmp       rax,rcx
       jne       short M05_L01
M05_L00:
       mov       rax,rdx
       ret
M05_L01:
       test      rax,rax
       je        short M05_L04
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M05_L00
       jmp       short M05_L03
M05_L02:
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M05_L00
       jmp       short M05_L01
M05_L03:
       test      rax,rax
       je        short M05_L04
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M05_L00
       test      rax,rax
       je        short M05_L04
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M05_L00
       test      rax,rax
       jne       short M05_L02
M05_L04:
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
       je        short M06_L00
       mov       rcx,rsi
       cmp       [r8],rcx
       je        short M06_L00
       mov       rdx,rax
       call      qword ptr [7FF96E839090]; Precode of System.Runtime.CompilerServices.CastHelpers.ChkCastAny(Void*, System.Object)
       mov       r8,rax
M06_L00:
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
       sub       rsp,58
       xor       eax,eax
       mov       [rsp+28],rax
       vxorps    xmm4,xmm4,xmm4
       vmovdqu   ymmword ptr [rsp+30],ymm4
       mov       [rsp+50],rax
       mov       rbx,[rcx+60]
       mov       rsi,[rcx+68]
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rsp+48],xmm0
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
       add       rsp,58
       pop       rbx
       pop       rsi
       ret
M00_L02:
       mov       rdx,0AF3FC00A00
       cmp       rbx,[rdx]
       je        short M00_L04
       mov       rdx,0AF3FC00A08
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
       test      rbx,rbx
       je        short M00_L05
       mov       rdx,0AF3FC00A00
       cmp       rbx,[rdx]
       je        short M00_L09
       mov       rdx,0AF3FC00A08
       cmp       rbx,[rdx]
       je        short M00_L09
       movsx     rdx,si
       jmp       short M00_L08
M00_L05:
       test      rsi,rsi
       je        short M00_L07
       lea       rdx,[rsi-1]
       mov       ecx,1
       mov       eax,2
       cmp       rdx,1
       mov       edx,eax
       cmovbe    edx,ecx
M00_L06:
       jmp       short M00_L08
M00_L07:
       xor       edx,edx
       jmp       short M00_L06
M00_L08:
       test      edx,edx
       je        short M00_L10
M00_L09:
       mov       rdx,rbx
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt32Array
       call      qword ptr [7FF977816850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rax,rax
       jne       short M00_L10
       mov       [rsp+28],rbx
       mov       [rsp+30],rsi
       lea       r8,[rsp+28]
       lea       rcx,[rsp+38]
       mov       edx,6
       call      qword ptr [7FF977C179F0]
       jmp       short M00_L11
M00_L10:
       mov       [rsp+38],rbx
       mov       [rsp+40],rsi
M00_L11:
       vmovdqu   xmm0,xmmword ptr [rsp+38]
       vmovdqu   xmmword ptr [rsp+48],xmm0
       lea       rcx,[rsp+48]
       call      qword ptr [7FF977C15B90]
       mov       rdx,rax
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt32Array
       call      qword ptr [7FF977816328]; System.Runtime.CompilerServices.CastHelpers.ChkCastClass(Void*, System.Object)
       jmp       near ptr M00_L01
; Total bytes of code 325
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

## .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3 (Job: Job-BZNPUE(IterationCount=5, IterationTime=200ms, LaunchCount=1, WarmupCount=3))

```assembly
; AuroraBenchmark.SafeOpsBenchmarks.ArrayMiss()
       push      r14
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,70
       xor       eax,eax
       mov       [rsp+28],rax
       vxorps    xmm4,xmm4,xmm4
       vmovdqu   ymmword ptr [rsp+30],ymm4
       vmovdqu   ymmword ptr [rsp+50],ymm4
M00_L00:
       mov       rdx,[rcx+10]
       mov       rbx,[rcx+18]
       mov       rsi,[rcx+70]
       mov       rdi,[rcx+78]
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rsp+50],xmm0
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rsp+60],xmm0
       test      rdx,rdx
       jne       near ptr M00_L11
       test      rbx,rbx
       je        near ptr M00_L12
       lea       rcx,[rbx-1]
       mov       eax,1
       mov       r8d,2
       cmp       rcx,1
       cmova     eax,r8d
M00_L01:
       cmp       eax,4
       je        near ptr M00_L13
M00_L02:
       mov       [rsp+40],rdx
       mov       [rsp+48],rbx
       cmp       qword ptr [rsp+40],0
       jne       near ptr M00_L24
       mov       rcx,[rsp+48]
       test      rcx,rcx
       je        near ptr M00_L27
       dec       rcx
       mov       eax,1
       mov       edx,2
       cmp       rcx,1
       cmova     eax,edx
M00_L03:
       cmp       eax,4
       jbe       near ptr M00_L26
       cmp       eax,1000
       je        near ptr M00_L48
       cmp       eax,2000
       je        near ptr M00_L49
       mov       rdx,[rsp+40]
       mov       rbx,rdx
       test      rbx,rbx
       je        short M00_L04
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptArray
       cmp       [rbx],rcx
       jne       near ptr M00_L28
M00_L04:
       test      rbx,rbx
       je        near ptr M00_L50
M00_L05:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptArray
       cmp       [rbx],rcx
       jne       near ptr M00_L54
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptEnumerator
       call      CORINFO_HELP_NEWSFAST
       mov       rbp,rax
       mov       rcx,0A033D46C60
       mov       [rbp+10],rcx
       mov       rcx,8001000A28
       mov       rdx,[rcx]
       lea       rcx,[rbp+20]
       call      CORINFO_HELP_ASSIGN_REF
       mov       rcx,8001000A30
       mov       rdx,[rcx]
       lea       rcx,[rbp+8]
       call      CORINFO_HELP_ASSIGN_REF
       mov       dword ptr [rbp+34],1
       lea       rcx,[rbp+40]
       mov       rdx,rbx
       call      CORINFO_HELP_ASSIGN_REF
       mov       ecx,[rbx+34]
       mov       [rbp+58],ecx
       xor       ecx,ecx
       mov       [rbp+5C],ecx
M00_L06:
       mov       edx,[rbp+5C]
       cmp       edx,[rbp+58]
       jge       short M00_L08
M00_L07:
       lea       rdx,[rsp+60]
       mov       rcx,rbp
       call      qword ptr [7FF977C35DB8]; AuroraScript.Runtime.Types.ScriptEnumerator.Value()
       inc       dword ptr [rbp+5C]
       mov       [rsp+28],rsi
       mov       [rsp+30],rdi
       lea       rdx,[rsp+28]
       lea       rcx,[rsp+60]
       call      qword ptr [7FF977C35C38]; AuroraScript.Runtime.ScriptDatum.Equals(AuroraScript.Runtime.ScriptDatum)
       test      eax,eax
       jne       near ptr M00_L55
       mov       eax,[rbp+5C]
       cmp       eax,[rbp+58]
       jl        short M00_L07
M00_L08:
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rsp+60],xmm0
M00_L09:
       xor       eax,eax
M00_L10:
       add       rsp,70
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       ret
M00_L11:
       mov       rax,8001000A00
       cmp       rdx,[rax]
       je        near ptr M00_L02
       mov       rcx,8001000A08
       cmp       rdx,[rcx]
       je        near ptr M00_L02
       movsx     rax,bx
       jmp       near ptr M00_L01
M00_L12:
       xor       eax,eax
       jmp       near ptr M00_L01
M00_L13:
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbx,rax
       mov       rdx,0A033D40008
       test      rbx,rbx
       cmove     rbx,rdx
       mov       rbp,rbx
       test      rsi,rsi
       je        short M00_L14
       mov       rcx,8001000A00
       cmp       rsi,[rcx]
       je        near ptr M00_L18
       mov       rdx,8001000A08
       cmp       rsi,[rdx]
       je        short M00_L18
       movsx     rcx,di
       jmp       short M00_L17
M00_L14:
       test      rdi,rdi
       je        short M00_L16
       lea       rdx,[rdi-1]
       mov       ecx,1
       mov       eax,2
       cmp       rdx,1
       cmova     ecx,eax
M00_L15:
       jmp       short M00_L17
M00_L16:
       xor       ecx,ecx
       jmp       short M00_L15
M00_L17:
       cmp       ecx,4
       jne       short M00_L18
       mov       rdx,rsi
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdx,rax
       mov       r9,0A033D40008
       test      rdx,rdx
       cmove     rdx,r9
       cmp       dword ptr [rdx+8],1
       jle       short M00_L18
       mov       dword ptr [rsp+20],4
       mov       r9d,[rbx+8]
       mov       rcx,rbx
       xor       r8d,r8d
       call      qword ptr [7FF977C37D38]
       not       eax
       shr       eax,1F
       jmp       near ptr M00_L10
M00_L18:
       xor       ebx,ebx
       jmp       near ptr M00_L23
M00_L19:
       movzx     r14d,word ptr [rbp+rbx*2+0C]
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001001498
       mov       rcx,[rcx]
       cmp       [rcx+8],r14d
       jg        short M00_L20
       mov       ecx,1
       call      qword ptr [7FF977836670]; System.String.FastAllocateString(IntPtr)
       mov       [rax+0C],r14w
       jmp       short M00_L21
M00_L20:
       mov       ecx,r14d
       call      qword ptr [7FF977C37D08]
       mov       rax,[rax+38]
M00_L21:
       test      rax,rax
       jne       short M00_L22
       mov       rax,0A033D40008
M00_L22:
       mov       [rsp+50],rax
       mov       qword ptr [rsp+58],4
       mov       [rsp+28],rsi
       mov       [rsp+30],rdi
       lea       rdx,[rsp+28]
       lea       rcx,[rsp+50]
       call      qword ptr [7FF977C35C38]; AuroraScript.Runtime.ScriptDatum.Equals(AuroraScript.Runtime.ScriptDatum)
       test      eax,eax
       jne       near ptr M00_L55
       inc       ebx
M00_L23:
       cmp       [rbp+8],ebx
       jg        near ptr M00_L19
       jmp       near ptr M00_L09
M00_L24:
       mov       rax,[rsp+40]
       mov       rcx,8001000A00
       cmp       rax,[rcx]
       jne       short M00_L25
       mov       eax,1000
       jmp       near ptr M00_L03
M00_L25:
       mov       rcx,[rsp+40]
       mov       rax,8001000A08
       movsx     rdx,word ptr [rsp+48]
       mov       r8d,2000
       cmp       rcx,[rax]
       mov       eax,r8d
       cmovne    eax,edx
       jmp       near ptr M00_L03
M00_L26:
       mov       ecx,eax
       lea       rax,[7FF9778B1FF8]
       mov       eax,[rax+rcx*4]
       lea       rdx,[M00_L00]
       add       rax,rdx
       jmp       rax
M00_L27:
       xor       eax,eax
       jmp       near ptr M00_L03
M00_L28:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbx,rax
       jmp       near ptr M00_L04
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001000A50
       mov       rbx,[rcx]
       jmp       near ptr M00_L05
       cmp       qword ptr [rsp+48],2
       je        short M00_L29
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BooleanValue
       call      qword ptr [7FF977835740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001001520
       mov       rbx,[rcx]
       jmp       short M00_L30
M00_L29:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BooleanValue
       call      qword ptr [7FF977835740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001001518
       mov       rbx,[rcx]
M00_L30:
       jmp       near ptr M00_L05
       mov       rcx,[rsp+48]
       mov       rax,8007FFFFFFFFFFFF
       add       rax,rcx
       cmp       rax,3
       jbe       short M00_L31
       vmovq     xmm1,rcx
       jmp       short M00_L33
M00_L31:
       cmp       rax,2
       jbe       short M00_L32
       vmovsd    xmm1,qword ptr [7FF9778B2010]
       jmp       short M00_L33
M00_L32:
       vmovq     xmm1,rax
M00_L33:
       vucomisd  xmm1,xmm1
       jp        short M00_L34
       je        short M00_L35
M00_L34:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977835740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001001508
       mov       rbx,[rcx]
       jmp       near ptr M00_L47
M00_L35:
       vxorps    xmm0,xmm0,xmm0
       vucomisd  xmm1,xmm0
       jp        short M00_L36
       jne       short M00_L36
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977835740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80010014B8
       mov       rbx,[rcx]
       jmp       near ptr M00_L47
M00_L36:
       vucomisd  xmm1,qword ptr [7FF9778B2018]
       jp        short M00_L37
       jne       short M00_L37
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977835740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80010014C0
       mov       rbx,[rcx]
       jmp       near ptr M00_L47
M00_L37:
       vucomisd  xmm1,qword ptr [7FF9778B2020]
       jp        short M00_L38
       jne       short M00_L38
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977835740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80010014C8
       mov       rbx,[rcx]
       jmp       near ptr M00_L47
M00_L38:
       vucomisd  xmm1,qword ptr [7FF9778B2028]
       jp        short M00_L39
       jne       short M00_L39
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977835740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80010014D0
       mov       rbx,[rcx]
       jmp       near ptr M00_L47
M00_L39:
       vucomisd  xmm1,qword ptr [7FF9778B2030]
       jp        short M00_L40
       jne       short M00_L40
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977835740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80010014D8
       mov       rbx,[rcx]
       jmp       near ptr M00_L47
M00_L40:
       vucomisd  xmm1,qword ptr [7FF9778B2038]
       jp        short M00_L41
       jne       short M00_L41
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977835740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80010014E0
       mov       rbx,[rcx]
       jmp       near ptr M00_L47
M00_L41:
       vucomisd  xmm1,qword ptr [7FF9778B2040]
       jp        short M00_L42
       jne       short M00_L42
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977835740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80010014E8
       mov       rbx,[rcx]
       jmp       near ptr M00_L47
M00_L42:
       vucomisd  xmm1,qword ptr [7FF9778B2048]
       jp        short M00_L43
       jne       short M00_L43
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977835740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80010014F0
       mov       rbx,[rcx]
       jmp       near ptr M00_L47
M00_L43:
       vucomisd  xmm1,qword ptr [7FF9778B2050]
       jp        short M00_L44
       jne       short M00_L44
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977835740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80010014F8
       mov       rbx,[rcx]
       jmp       near ptr M00_L47
M00_L44:
       vucomisd  xmm1,qword ptr [7FF9778B2058]
       jp        short M00_L45
       jne       short M00_L45
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977835740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001001500
       mov       rbx,[rcx]
       jmp       short M00_L47
M00_L45:
       vmovsd    qword ptr [rsp+38],xmm1
       vucomisd  xmm1,qword ptr [7FF9778B2060]
       jp        short M00_L46
       jne       short M00_L46
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977835740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80010014B0
       mov       rbx,[rcx]
       jmp       short M00_L47
M00_L46:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       rcx,rbx
       vmovsd    xmm1,qword ptr [rsp+38]
       call      qword ptr [7FF977C37D50]
M00_L47:
       jmp       near ptr M00_L05
M00_L48:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.Int64Value
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       rcx,rbx
       mov       rdx,[rsp+48]
       call      qword ptr [7FF977C35CF8]
       jmp       near ptr M00_L05
M00_L49:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.UInt64Value
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       rcx,rbx
       mov       rdx,[rsp+48]
       call      qword ptr [7FF977C35D28]
       jmp       near ptr M00_L05
       lea       rcx,[rsp+40]
       call      qword ptr [7FF977C35D40]
       mov       rbx,rax
       jmp       near ptr M00_L05
M00_L50:
       mov       rdx,[rsp+40]
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       short M00_L51
       xor       ebx,ebx
       jmp       near ptr M00_L05
M00_L51:
       cmp       dword ptr [rbp+8],0
       jne       short M00_L52
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80010014A0
       mov       rbx,[rcx]
       jmp       short M00_L53
M00_L52:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       rcx,rbx
       mov       rdx,rbp
       call      qword ptr [7FF977C37D68]
M00_L53:
       jmp       near ptr M00_L05
M00_L54:
       mov       rcx,rbx
       mov       rax,[rbx]
       mov       rax,[rax+58]
       call      qword ptr [rax]
       mov       rbp,rax
       jmp       near ptr M00_L06
M00_L55:
       mov       eax,1
       jmp       near ptr M00_L10
; Total bytes of code 1969
```
```assembly
; AuroraScript.Runtime.Types.ScriptEnumerator.Value()
       push      rsi
       push      rbx
       sub       rsp,38
       xor       eax,eax
       mov       [rsp+28],rax
       mov       [rsp+30],rax
       mov       rax,rcx
       mov       rbx,rdx
M01_L00:
       mov       ecx,[rax+34]
       cmp       ecx,1
       jne       short M01_L02
       mov       rcx,[rax+40]
       mov       rcx,[rcx+38]
       mov       eax,[rax+5C]
       cmp       eax,[rcx+8]
       jae       near ptr M01_L06
       shl       rax,4
       vmovdqu   xmm0,xmmword ptr [rcx+rax+10]
       vmovdqu   xmmword ptr [rsp+28],xmm0
M01_L01:
       vmovdqu   xmm0,xmmword ptr [rsp+28]
       vmovdqu   xmmword ptr [rbx],xmm0
       mov       rax,rbx
       add       rsp,38
       pop       rbx
       pop       rsi
       ret
M01_L02:
       cmp       ecx,3
       ja        short M01_L03
       mov       ecx,ecx
       lea       rdx,[7FF9778B1310]
       mov       edx,[rdx+rcx*4]
       lea       r8,[M01_L00]
       add       rdx,r8
       jmp       rdx
M01_L03:
       mov       rcx,offset MT_System.InvalidOperationException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       ecx,1AD3
       mov       rdx,7FF977A78948
       call      qword ptr [7FF97783F210]
       mov       rdx,rax
       mov       rcx,rbx
       call      qword ptr [7FF977B37BE8]
       mov       rcx,rbx
       call      CORINFO_HELP_THROW
       int       3
       mov       rcx,[rax+48]
       mov       r8d,[rax+5C]
       lea       rdx,[rsp+28]
       mov       rax,[rcx]
       mov       rax,[rax+58]
       call      qword ptr [rax+10]
       jmp       near ptr M01_L01
       mov       rcx,[rax+38]
       mov       eax,[rax+5C]
       cmp       eax,[rcx+8]
       jae       near ptr M01_L06
       shl       rax,4
       vmovdqu   xmm0,xmmword ptr [rcx+rax+10]
       vmovdqu   xmmword ptr [rsp+28],xmm0
       jmp       near ptr M01_L01
       mov       rcx,[rax+50]
       mov       eax,[rax+5C]
       cmp       eax,[rcx+8]
       jae       short M01_L06
       movzx     esi,word ptr [rcx+rax*2+0C]
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001001498
       mov       rcx,[rcx]
       cmp       [rcx+8],esi
       jg        short M01_L04
       mov       ecx,1
       call      qword ptr [7FF977836670]; System.String.FastAllocateString(IntPtr)
       mov       [rax+0C],si
       jmp       short M01_L05
M01_L04:
       mov       ecx,esi
       call      qword ptr [7FF977C37D08]
       mov       rax,[rax+38]
M01_L05:
       mov       rcx,0A033D40008
       test      rax,rax
       cmove     rax,rcx
       mov       [rsp+28],rax
       mov       qword ptr [rsp+30],4
       jmp       near ptr M01_L01
M01_L06:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 363
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
       je        near ptr M02_L09
       dec       rdx
       mov       eax,1
       mov       ecx,2
       cmp       rdx,1
       cmova     eax,ecx
M02_L01:
       cmp       qword ptr [rsp+30],0
       jne       near ptr M02_L10
       mov       rdx,[rsp+38]
       test      rdx,rdx
       je        near ptr M02_L12
       dec       rdx
       mov       r8d,1
       mov       ecx,2
       cmp       rdx,1
       cmova     r8d,ecx
M02_L02:
       cmp       eax,r8d
       jne       near ptr M02_L21
       cmp       r8d,2
       jne       near ptr M02_L13
       mov       rax,[rsp+48]
       mov       rcx,8007FFFFFFFFFFFF
       add       rcx,rax
       cmp       rcx,3
       jbe       near ptr M02_L15
       vmovq     xmm0,rax
M02_L03:
       mov       rcx,[rsp+38]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       near ptr M02_L17
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
       mov       rdx,8001000A00
       cmp       rax,[rdx]
       jne       short M02_L08
       mov       eax,1000
       jmp       near ptr M02_L01
M02_L08:
       mov       rdx,[rsp+40]
       mov       rcx,8001000A08
       movsx     rax,word ptr [rsp+48]
       mov       r8d,2000
       cmp       rdx,[rcx]
       cmove     eax,r8d
       jmp       near ptr M02_L01
M02_L09:
       xor       eax,eax
       jmp       near ptr M02_L01
M02_L10:
       mov       r8,[rsp+30]
       mov       rdx,8001000A00
       cmp       r8,[rdx]
       jne       short M02_L11
       mov       r8d,1000
       jmp       near ptr M02_L02
M02_L11:
       mov       rdx,[rsp+30]
       mov       rcx,8001000A08
       movsx     r8,word ptr [rsp+38]
       mov       r10d,2000
       cmp       rdx,[rcx]
       cmove     r8d,r10d
       jmp       near ptr M02_L02
M02_L12:
       xor       r8d,r8d
       jmp       near ptr M02_L02
M02_L13:
       cmp       r8d,4
       ja        short M02_L14
       mov       edx,r8d
       lea       rcx,[7FF9778B1738]
       mov       ecx,[rcx+rdx*4]
       lea       rax,[M02_L00]
       add       rcx,rax
       jmp       rcx
M02_L14:
       cmp       r8d,1000
       je        near ptr M02_L19
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
M02_L15:
       cmp       rcx,2
       jbe       short M02_L16
       vmovsd    xmm0,qword ptr [7FF9778B1750]
       jmp       near ptr M02_L03
M02_L16:
       vmovq     xmm0,rcx
       jmp       near ptr M02_L03
M02_L17:
       cmp       rdx,2
       jbe       short M02_L18
       vmovsd    xmm1,qword ptr [7FF9778B1750]
       jmp       near ptr M02_L04
M02_L18:
       vmovq     xmm1,rdx
       jmp       near ptr M02_L04
M02_L19:
       mov       rax,[rsp+48]
       cmp       rax,[rsp+38]
       sete      al
       movzx     eax,al
       jmp       near ptr M02_L06
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
       vmovsd    xmm0,qword ptr [7FF9778B1750]
       jmp       short M02_L26
M02_L25:
       vmovq     xmm0,rdx
M02_L26:
       vmovsd    qword ptr [rsp+28],xmm0
       jmp       short M02_L28
M02_L27:
       lea       rcx,[rsp+40]
       lea       rdx,[rsp+28]
       call      qword ptr [7FF977C37D20]
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
       vmovsd    xmm0,qword ptr [7FF9778B1750]
       jmp       short M02_L31
M02_L30:
       vmovq     xmm0,rdx
M02_L31:
       vmovsd    qword ptr [rsp+20],xmm0
       jmp       short M02_L33
M02_L32:
       lea       rcx,[rsp+30]
       lea       rdx,[rsp+20]
       call      qword ptr [7FF977C37D20]
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
; Total bytes of code 1015
```
```assembly
; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rdx,rdx
       je        short M03_L01
       mov       rax,[rdx]
       cmp       rax,rcx
       je        short M03_L01
       mov       rax,[rax+10]
M03_L00:
       cmp       rax,rcx
       jne       short M03_L02
M03_L01:
       mov       rax,rdx
       ret
M03_L02:
       test      rax,rax
       je        short M03_L03
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M03_L01
       test      rax,rax
       je        short M03_L03
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M03_L01
       test      rax,rax
       je        short M03_L03
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M03_L01
       test      rax,rax
       je        short M03_L03
       mov       rax,[rax+10]
       jmp       short M03_L00
M03_L03:
       xor       edx,edx
       jmp       short M03_L01
; Total bytes of code 83
```
```assembly
; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rax,[rcx+20]
       mov       rax,[rax-18]
       mov       rdx,rax
       test      dl,1
       jne       short M04_L00
       ret
M04_L00:
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
       jne       short M06_L00
       mov       rcx,7FF977CA5A64
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rax,rsi
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
M06_L00:
       mov       rcx,7FF977CA5A60
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
       je        short M07_L02
       test      rcx,rcx
       je        short M07_L00
       test      rdx,rdx
       je        short M07_L00
       mov       r8d,[rcx+8]
       cmp       r8d,[rdx+8]
       je        short M07_L01
M07_L00:
       xor       eax,eax
       add       rsp,28
       ret
M07_L01:
       lea       rax,[rcx+0C]
       add       rdx,0C
       mov       r8d,[rcx+8]
       add       r8d,r8d
       mov       rcx,rax
       call      qword ptr [7FF96E84D900]; Precode of System.SpanHelpers.SequenceEqual(Byte ByRef, Byte ByRef, UIntPtr)
       nop
       add       rsp,28
       ret
M07_L02:
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
       push      r14
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,70
       xor       eax,eax
       mov       [rsp+28],rax
       vxorps    xmm4,xmm4,xmm4
       vmovdqu   ymmword ptr [rsp+30],ymm4
       vmovdqu   ymmword ptr [rsp+50],ymm4
M00_L00:
       mov       rdx,[rcx+20]
       mov       rbx,[rcx+28]
       mov       rsi,[rcx+70]
       mov       rdi,[rcx+78]
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rsp+50],xmm0
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rsp+60],xmm0
       test      rdx,rdx
       jne       near ptr M00_L11
       test      rbx,rbx
       je        near ptr M00_L12
       lea       rcx,[rbx-1]
       mov       eax,1
       mov       r8d,2
       cmp       rcx,1
       cmova     eax,r8d
M00_L01:
       cmp       eax,4
       je        near ptr M00_L13
M00_L02:
       mov       [rsp+40],rdx
       mov       [rsp+48],rbx
       cmp       qword ptr [rsp+40],0
       jne       near ptr M00_L24
       mov       rcx,[rsp+48]
       test      rcx,rcx
       je        near ptr M00_L27
       dec       rcx
       mov       eax,1
       mov       edx,2
       cmp       rcx,1
       cmova     eax,edx
M00_L03:
       cmp       eax,4
       jbe       near ptr M00_L26
       cmp       eax,1000
       je        near ptr M00_L48
       cmp       eax,2000
       je        near ptr M00_L49
       mov       rdx,[rsp+40]
       mov       rbx,rdx
       test      rbx,rbx
       je        short M00_L04
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt32Array
       cmp       [rbx],rcx
       jne       near ptr M00_L28
M00_L04:
       test      rbx,rbx
       je        near ptr M00_L50
M00_L05:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt32Array
       cmp       [rbx],rcx
       jne       near ptr M00_L54
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptEnumerator
       call      CORINFO_HELP_NEWSFAST
       mov       rbp,rax
       mov       rcx,0BF54256C60
       mov       [rbp+10],rcx
       mov       rcx,9F21800A28
       mov       rdx,[rcx]
       lea       rcx,[rbp+20]
       call      CORINFO_HELP_ASSIGN_REF
       mov       rcx,9F21800A30
       mov       rdx,[rcx]
       lea       rcx,[rbp+8]
       call      CORINFO_HELP_ASSIGN_REF
       mov       dword ptr [rbp+34],2
       lea       rcx,[rbp+48]
       mov       rdx,rbx
       call      CORINFO_HELP_ASSIGN_REF
       mov       rcx,[rbx+38]
       mov       ecx,[rcx+8]
       mov       [rbp+58],ecx
       xor       ecx,ecx
       mov       [rbp+5C],ecx
M00_L06:
       mov       edx,[rbp+5C]
       cmp       edx,[rbp+58]
       jge       short M00_L08
M00_L07:
       lea       rdx,[rsp+60]
       mov       rcx,rbp
       call      qword ptr [7FF977C45DD0]; AuroraScript.Runtime.Types.ScriptEnumerator.Value()
       inc       dword ptr [rbp+5C]
       mov       [rsp+28],rsi
       mov       [rsp+30],rdi
       lea       rdx,[rsp+28]
       lea       rcx,[rsp+60]
       call      qword ptr [7FF977C45C50]; AuroraScript.Runtime.ScriptDatum.Equals(AuroraScript.Runtime.ScriptDatum)
       test      eax,eax
       jne       near ptr M00_L55
       mov       eax,[rbp+5C]
       cmp       eax,[rbp+58]
       jl        short M00_L07
M00_L08:
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rsp+60],xmm0
M00_L09:
       xor       eax,eax
M00_L10:
       add       rsp,70
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       ret
M00_L11:
       mov       rax,9F21800A00
       cmp       rdx,[rax]
       je        near ptr M00_L02
       mov       rcx,9F21800A08
       cmp       rdx,[rcx]
       je        near ptr M00_L02
       movsx     rax,bx
       jmp       near ptr M00_L01
M00_L12:
       xor       eax,eax
       jmp       near ptr M00_L01
M00_L13:
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbx,rax
       mov       rdx,0BF54250008
       test      rbx,rbx
       cmove     rbx,rdx
       mov       rbp,rbx
       test      rsi,rsi
       je        short M00_L14
       mov       rcx,9F21800A00
       cmp       rsi,[rcx]
       je        near ptr M00_L18
       mov       rdx,9F21800A08
       cmp       rsi,[rdx]
       je        short M00_L18
       movsx     rcx,di
       jmp       short M00_L17
M00_L14:
       test      rdi,rdi
       je        short M00_L16
       lea       rdx,[rdi-1]
       mov       ecx,1
       mov       eax,2
       cmp       rdx,1
       cmova     ecx,eax
M00_L15:
       jmp       short M00_L17
M00_L16:
       xor       ecx,ecx
       jmp       short M00_L15
M00_L17:
       cmp       ecx,4
       jne       short M00_L18
       mov       rdx,rsi
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdx,rax
       mov       r9,0BF54250008
       test      rdx,rdx
       cmove     rdx,r9
       cmp       dword ptr [rdx+8],1
       jle       short M00_L18
       mov       dword ptr [rsp+20],4
       mov       r9d,[rbx+8]
       mov       rcx,rbx
       xor       r8d,r8d
       call      qword ptr [7FF977C47D68]
       not       eax
       shr       eax,1F
       jmp       near ptr M00_L10
M00_L18:
       xor       ebx,ebx
       jmp       near ptr M00_L23
M00_L19:
       movzx     r14d,word ptr [rbp+rbx*2+0C]
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,9F21801498
       mov       rcx,[rcx]
       cmp       [rcx+8],r14d
       jg        short M00_L20
       mov       ecx,1
       call      qword ptr [7FF977846670]; System.String.FastAllocateString(IntPtr)
       mov       [rax+0C],r14w
       jmp       short M00_L21
M00_L20:
       mov       ecx,r14d
       call      qword ptr [7FF977C47D38]
       mov       rax,[rax+38]
M00_L21:
       test      rax,rax
       jne       short M00_L22
       mov       rax,0BF54250008
M00_L22:
       mov       [rsp+50],rax
       mov       qword ptr [rsp+58],4
       mov       [rsp+28],rsi
       mov       [rsp+30],rdi
       lea       rdx,[rsp+28]
       lea       rcx,[rsp+50]
       call      qword ptr [7FF977C45C50]; AuroraScript.Runtime.ScriptDatum.Equals(AuroraScript.Runtime.ScriptDatum)
       test      eax,eax
       jne       near ptr M00_L55
       inc       ebx
M00_L23:
       cmp       [rbp+8],ebx
       jg        near ptr M00_L19
       jmp       near ptr M00_L09
M00_L24:
       mov       rax,[rsp+40]
       mov       rcx,9F21800A00
       cmp       rax,[rcx]
       jne       short M00_L25
       mov       eax,1000
       jmp       near ptr M00_L03
M00_L25:
       mov       rcx,[rsp+40]
       mov       rax,9F21800A08
       movsx     rdx,word ptr [rsp+48]
       mov       r8d,2000
       cmp       rcx,[rax]
       mov       eax,r8d
       cmovne    eax,edx
       jmp       near ptr M00_L03
M00_L26:
       mov       ecx,eax
       lea       rax,[7FF9778C24B8]
       mov       eax,[rax+rcx*4]
       lea       rdx,[M00_L00]
       add       rax,rdx
       jmp       rax
M00_L27:
       xor       eax,eax
       jmp       near ptr M00_L03
M00_L28:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbx,rax
       jmp       near ptr M00_L04
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,9F21800A50
       mov       rbx,[rcx]
       jmp       near ptr M00_L05
       cmp       qword ptr [rsp+48],2
       je        short M00_L29
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BooleanValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,9F21801520
       mov       rbx,[rcx]
       jmp       short M00_L30
M00_L29:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BooleanValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,9F21801518
       mov       rbx,[rcx]
M00_L30:
       jmp       near ptr M00_L05
       mov       rcx,[rsp+48]
       mov       rax,8007FFFFFFFFFFFF
       add       rax,rcx
       cmp       rax,3
       jbe       short M00_L31
       vmovq     xmm1,rcx
       jmp       short M00_L33
M00_L31:
       cmp       rax,2
       jbe       short M00_L32
       vmovsd    xmm1,qword ptr [7FF9778C24D0]
       jmp       short M00_L33
M00_L32:
       vmovq     xmm1,rax
M00_L33:
       vucomisd  xmm1,xmm1
       jp        short M00_L34
       je        short M00_L35
M00_L34:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,9F21801508
       mov       rbx,[rcx]
       jmp       near ptr M00_L47
M00_L35:
       vxorps    xmm0,xmm0,xmm0
       vucomisd  xmm1,xmm0
       jp        short M00_L36
       jne       short M00_L36
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,9F218014B8
       mov       rbx,[rcx]
       jmp       near ptr M00_L47
M00_L36:
       vucomisd  xmm1,qword ptr [7FF9778C24D8]
       jp        short M00_L37
       jne       short M00_L37
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,9F218014C0
       mov       rbx,[rcx]
       jmp       near ptr M00_L47
M00_L37:
       vucomisd  xmm1,qword ptr [7FF9778C24E0]
       jp        short M00_L38
       jne       short M00_L38
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,9F218014C8
       mov       rbx,[rcx]
       jmp       near ptr M00_L47
M00_L38:
       vucomisd  xmm1,qword ptr [7FF9778C24E8]
       jp        short M00_L39
       jne       short M00_L39
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,9F218014D0
       mov       rbx,[rcx]
       jmp       near ptr M00_L47
M00_L39:
       vucomisd  xmm1,qword ptr [7FF9778C24F0]
       jp        short M00_L40
       jne       short M00_L40
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,9F218014D8
       mov       rbx,[rcx]
       jmp       near ptr M00_L47
M00_L40:
       vucomisd  xmm1,qword ptr [7FF9778C24F8]
       jp        short M00_L41
       jne       short M00_L41
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,9F218014E0
       mov       rbx,[rcx]
       jmp       near ptr M00_L47
M00_L41:
       vucomisd  xmm1,qword ptr [7FF9778C2500]
       jp        short M00_L42
       jne       short M00_L42
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,9F218014E8
       mov       rbx,[rcx]
       jmp       near ptr M00_L47
M00_L42:
       vucomisd  xmm1,qword ptr [7FF9778C2508]
       jp        short M00_L43
       jne       short M00_L43
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,9F218014F0
       mov       rbx,[rcx]
       jmp       near ptr M00_L47
M00_L43:
       vucomisd  xmm1,qword ptr [7FF9778C2510]
       jp        short M00_L44
       jne       short M00_L44
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,9F218014F8
       mov       rbx,[rcx]
       jmp       near ptr M00_L47
M00_L44:
       vucomisd  xmm1,qword ptr [7FF9778C2518]
       jp        short M00_L45
       jne       short M00_L45
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,9F21801500
       mov       rbx,[rcx]
       jmp       short M00_L47
M00_L45:
       vmovsd    qword ptr [rsp+38],xmm1
       vucomisd  xmm1,qword ptr [7FF9778C2520]
       jp        short M00_L46
       jne       short M00_L46
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,9F218014B0
       mov       rbx,[rcx]
       jmp       short M00_L47
M00_L46:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       rcx,rbx
       vmovsd    xmm1,qword ptr [rsp+38]
       call      qword ptr [7FF977C47D80]
M00_L47:
       jmp       near ptr M00_L05
M00_L48:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.Int64Value
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       rcx,rbx
       mov       rdx,[rsp+48]
       call      qword ptr [7FF977C45D10]
       jmp       near ptr M00_L05
M00_L49:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.UInt64Value
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       rcx,rbx
       mov       rdx,[rsp+48]
       call      qword ptr [7FF977C45D40]
       jmp       near ptr M00_L05
       lea       rcx,[rsp+40]
       call      qword ptr [7FF977C45D58]
       mov       rbx,rax
       jmp       near ptr M00_L05
M00_L50:
       mov       rdx,[rsp+40]
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       short M00_L51
       xor       ebx,ebx
       jmp       near ptr M00_L05
M00_L51:
       cmp       dword ptr [rbp+8],0
       jne       short M00_L52
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,9F218014A0
       mov       rbx,[rcx]
       jmp       short M00_L53
M00_L52:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       rcx,rbx
       mov       rdx,rbp
       call      qword ptr [7FF977C47D98]
M00_L53:
       jmp       near ptr M00_L05
M00_L54:
       mov       rcx,rbx
       mov       rax,[rbx]
       mov       rax,[rax+58]
       call      qword ptr [rax]
       mov       rbp,rax
       jmp       near ptr M00_L06
M00_L55:
       mov       eax,1
       jmp       near ptr M00_L10
; Total bytes of code 1973
```
```assembly
; AuroraScript.Runtime.Types.ScriptEnumerator.Value()
       push      rsi
       push      rbx
       sub       rsp,38
       xor       eax,eax
       mov       [rsp+28],rax
       mov       [rsp+30],rax
       mov       rbx,rdx
M01_L00:
       mov       eax,[rcx+34]
       cmp       eax,2
       jne       short M01_L03
       mov       rax,[rcx+48]
       mov       r8d,[rcx+5C]
       mov       rdx,offset MT_AuroraScript.Runtime.Types.ScriptInt32Array
       cmp       [rax],rdx
       jne       near ptr M01_L06
       mov       rdx,[rax+38]
       cmp       r8d,[rdx+8]
       jae       near ptr M01_L09
       mov       eax,r8d
       mov       edx,[rdx+rax*4+10]
       test      edx,edx
       je        near ptr M01_L05
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2sd xmm0,xmm0,edx
       vmovq     rdx,xmm0
M01_L01:
       xor       ecx,ecx
       mov       [rsp+28],rcx
       mov       [rsp+30],rdx
M01_L02:
       mov       rax,[rsp+28]
       mov       [rbx],rax
       mov       rax,[rsp+30]
       mov       [rbx+8],rax
       mov       rax,rbx
       add       rsp,38
       pop       rbx
       pop       rsi
       ret
M01_L03:
       cmp       eax,3
       ja        short M01_L04
       mov       eax,eax
       lea       rdx,[7FF9778C16A0]
       mov       edx,[rdx+rax*4]
       lea       r8,[M01_L00]
       add       rdx,r8
       jmp       rdx
M01_L04:
       mov       rcx,offset MT_System.InvalidOperationException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       ecx,1AD3
       mov       rdx,7FF977A88948
       call      qword ptr [7FF97784F210]
       mov       rdx,rax
       mov       rcx,rbx
       call      qword ptr [7FF977B47BE8]
       mov       rcx,rbx
       call      CORINFO_HELP_THROW
       int       3
       mov       rax,[rcx+40]
       mov       rax,[rax+38]
       mov       ecx,[rcx+5C]
       cmp       ecx,[rax+8]
       jae       near ptr M01_L09
       shl       rcx,4
       vmovdqu   xmm0,xmmword ptr [rax+rcx+10]
       vmovdqu   xmmword ptr [rsp+28],xmm0
       jmp       near ptr M01_L02
M01_L05:
       mov       rdx,7FF8000000000001
       jmp       near ptr M01_L01
M01_L06:
       lea       rdx,[rsp+28]
       mov       rcx,rax
       mov       rax,[rax]
       mov       rax,[rax+58]
       call      qword ptr [rax+10]
       jmp       near ptr M01_L02
       mov       rax,[rcx+38]
       mov       ecx,[rcx+5C]
       cmp       ecx,[rax+8]
       jae       near ptr M01_L09
       shl       rcx,4
       vmovdqu   xmm0,xmmword ptr [rax+rcx+10]
       vmovdqu   xmmword ptr [rsp+28],xmm0
       jmp       near ptr M01_L02
       mov       rax,[rcx+50]
       mov       ecx,[rcx+5C]
       cmp       ecx,[rax+8]
       jae       short M01_L09
       movzx     esi,word ptr [rax+rcx*2+0C]
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,9F21801498
       mov       rcx,[rcx]
       cmp       [rcx+8],esi
       jg        short M01_L07
       mov       ecx,1
       call      qword ptr [7FF977846670]; System.String.FastAllocateString(IntPtr)
       mov       [rax+0C],si
       jmp       short M01_L08
M01_L07:
       mov       ecx,esi
       call      qword ptr [7FF977C47D38]
       mov       rax,[rax+38]
M01_L08:
       mov       rdx,0BF54250008
       test      rax,rax
       cmove     rax,rdx
       mov       [rsp+28],rax
       mov       qword ptr [rsp+30],4
       jmp       near ptr M01_L02
M01_L09:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 463
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
       je        near ptr M02_L09
       dec       rdx
       mov       eax,1
       mov       ecx,2
       cmp       rdx,1
       cmova     eax,ecx
M02_L01:
       cmp       qword ptr [rsp+30],0
       jne       near ptr M02_L10
       mov       rdx,[rsp+38]
       test      rdx,rdx
       je        near ptr M02_L12
       dec       rdx
       mov       r8d,1
       mov       ecx,2
       cmp       rdx,1
       cmova     r8d,ecx
M02_L02:
       cmp       eax,r8d
       jne       near ptr M02_L21
       cmp       r8d,2
       jne       near ptr M02_L13
       mov       rax,[rsp+48]
       mov       rcx,8007FFFFFFFFFFFF
       add       rcx,rax
       cmp       rcx,3
       jbe       near ptr M02_L15
       vmovq     xmm0,rax
M02_L03:
       mov       rcx,[rsp+38]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       near ptr M02_L17
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
       mov       rdx,9F21800A00
       cmp       rax,[rdx]
       jne       short M02_L08
       mov       eax,1000
       jmp       near ptr M02_L01
M02_L08:
       mov       rdx,[rsp+40]
       mov       rcx,9F21800A08
       movsx     rax,word ptr [rsp+48]
       mov       r8d,2000
       cmp       rdx,[rcx]
       cmove     eax,r8d
       jmp       near ptr M02_L01
M02_L09:
       xor       eax,eax
       jmp       near ptr M02_L01
M02_L10:
       mov       r8,[rsp+30]
       mov       rdx,9F21800A00
       cmp       r8,[rdx]
       jne       short M02_L11
       mov       r8d,1000
       jmp       near ptr M02_L02
M02_L11:
       mov       rdx,[rsp+30]
       mov       rcx,9F21800A08
       movsx     r8,word ptr [rsp+38]
       mov       r10d,2000
       cmp       rdx,[rcx]
       cmove     r8d,r10d
       jmp       near ptr M02_L02
M02_L12:
       xor       r8d,r8d
       jmp       near ptr M02_L02
M02_L13:
       cmp       r8d,4
       ja        short M02_L14
       mov       edx,r8d
       lea       rcx,[7FF9778C1C08]
       mov       ecx,[rcx+rdx*4]
       lea       rax,[M02_L00]
       add       rcx,rax
       jmp       rcx
M02_L14:
       cmp       r8d,1000
       je        near ptr M02_L19
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
M02_L15:
       cmp       rcx,2
       jbe       short M02_L16
       vmovsd    xmm0,qword ptr [7FF9778C1C20]
       jmp       near ptr M02_L03
M02_L16:
       vmovq     xmm0,rcx
       jmp       near ptr M02_L03
M02_L17:
       cmp       rdx,2
       jbe       short M02_L18
       vmovsd    xmm1,qword ptr [7FF9778C1C20]
       jmp       near ptr M02_L04
M02_L18:
       vmovq     xmm1,rdx
       jmp       near ptr M02_L04
M02_L19:
       mov       rax,[rsp+48]
       cmp       rax,[rsp+38]
       sete      al
       movzx     eax,al
       jmp       near ptr M02_L06
       mov       rdx,[rsp+40]
       mov       rcx,offset MT_System.String
       call      qword ptr [7FF977846850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbx,rax
       mov       rdx,[rsp+30]
       mov       rcx,offset MT_System.String
       call      qword ptr [7FF977846850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdx,rax
       mov       rcx,rbx
       call      qword ptr [7FF977846E08]; System.String.Equals(System.String, System.String)
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
       vmovsd    xmm0,qword ptr [7FF9778C1C20]
       jmp       short M02_L26
M02_L25:
       vmovq     xmm0,rdx
M02_L26:
       vmovsd    qword ptr [rsp+28],xmm0
       jmp       short M02_L28
M02_L27:
       lea       rcx,[rsp+40]
       lea       rdx,[rsp+28]
       call      qword ptr [7FF977C47D50]
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
       vmovsd    xmm0,qword ptr [7FF9778C1C20]
       jmp       short M02_L31
M02_L30:
       vmovq     xmm0,rdx
M02_L31:
       vmovsd    qword ptr [rsp+20],xmm0
       jmp       short M02_L33
M02_L32:
       lea       rcx,[rsp+30]
       lea       rdx,[rsp+20]
       call      qword ptr [7FF977C47D50]
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
; Total bytes of code 1015
```
```assembly
; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rdx,rdx
       je        short M03_L01
       mov       rax,[rdx]
       cmp       rax,rcx
       je        short M03_L01
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M03_L01
M03_L00:
       test      rax,rax
       je        short M03_L03
       mov       rax,[rax+10]
       cmp       rax,rcx
       jne       short M03_L02
M03_L01:
       mov       rax,rdx
       ret
M03_L02:
       test      rax,rax
       je        short M03_L03
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M03_L01
       test      rax,rax
       je        short M03_L03
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M03_L01
       test      rax,rax
       je        short M03_L03
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M03_L01
       jmp       short M03_L00
M03_L03:
       xor       edx,edx
       jmp       short M03_L01
; Total bytes of code 88
```
```assembly
; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rax,[rcx+20]
       mov       rax,[rax-18]
       mov       rdx,rax
       test      dl,1
       jne       short M04_L00
       ret
M04_L00:
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
       jne       short M06_L00
       ret
M06_L00:
       jmp       qword ptr [7FF977A1E838]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBaseSlow(System.Runtime.CompilerServices.MethodTable*)
; Total bytes of code 23
```
```assembly
; System.String.Equals(System.String, System.String)
       sub       rsp,28
       cmp       rcx,rdx
       je        short M07_L02
       test      rcx,rcx
       je        short M07_L00
       test      rdx,rdx
       je        short M07_L00
       mov       r8d,[rcx+8]
       cmp       r8d,[rdx+8]
       je        short M07_L01
M07_L00:
       xor       eax,eax
       add       rsp,28
       ret
M07_L01:
       lea       rax,[rcx+0C]
       add       rdx,0C
       mov       r8d,[rcx+8]
       add       r8d,r8d
       mov       rcx,rax
       call      qword ptr [7FF96E84D900]; Precode of System.SpanHelpers.SequenceEqual(Byte ByRef, Byte ByRef, UIntPtr)
       nop
       add       rsp,28
       ret
M07_L02:
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
       sub       rsp,70
       xor       eax,eax
       mov       [rsp+28],rax
       vxorps    xmm4,xmm4,xmm4
       vmovdqu   ymmword ptr [rsp+30],ymm4
       vmovdqu   ymmword ptr [rsp+50],ymm4
M00_L00:
       mov       rbx,[rcx+30]
       mov       rsi,[rcx+38]
       mov       rdi,[rcx+40]
       mov       rbp,[rcx+48]
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rsp+50],xmm0
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rsp+60],xmm0
       test      rbx,rbx
       je        near ptr M00_L13
       mov       r9,8001400A00
       cmp       rbx,[r9]
       je        near ptr M00_L22
       mov       r9,8001400A08
       cmp       rbx,[r9]
       je        near ptr M00_L22
       movsx     r9,si
M00_L01:
       cmp       r9d,4
       jne       near ptr M00_L22
       test      rbx,rbx
       je        short M00_L02
       mov       r9,offset MT_System.String
       cmp       [rbx],r9
       jne       short M00_L02
       mov       rcx,rbx
       jmp       short M00_L03
M00_L02:
       xor       ecx,ecx
M00_L03:
       mov       r9,0A033EB0008
       test      rcx,rcx
       cmove     rcx,r9
       mov       rbx,rcx
       test      rdi,rdi
       je        near ptr M00_L16
       mov       r9,8001400A00
       cmp       rdi,[r9]
       je        short M00_L07
       mov       r9,8001400A08
       cmp       rdi,[r9]
       je        short M00_L07
       movsx     r9,bp
M00_L04:
       cmp       r9d,4
       jne       short M00_L07
       test      rdi,rdi
       je        short M00_L05
       mov       r9,offset MT_System.String
       cmp       [rdi],r9
       jne       short M00_L05
       mov       rdx,rdi
       jmp       short M00_L06
M00_L05:
       xor       edx,edx
M00_L06:
       mov       r9,0A033EB0008
       test      rdx,rdx
       cmove     rdx,r9
       cmp       dword ptr [rdx+8],1
       jg        near ptr M00_L19
M00_L07:
       mov       esi,[rbx+8]
       test      esi,esi
       jle       short M00_L11
       add       rbx,0C
M00_L08:
       movzx     r14d,word ptr [rbx]
       cmp       r14d,100
       jl        near ptr M00_L20
       mov       rcx,offset MT_System.String
       mov       edx,1
       call      00007FF9D74C4E80
       mov       [rax+0C],r14w
M00_L09:
       test      rax,rax
       je        near ptr M00_L21
M00_L10:
       mov       [rsp+50],rax
       mov       qword ptr [rsp+58],4
       mov       [rsp+28],rdi
       mov       [rsp+30],rbp
       lea       rdx,[rsp+28]
       lea       rcx,[rsp+50]
       call      qword ptr [7FF977C45C50]; AuroraScript.Runtime.ScriptDatum.Equals(AuroraScript.Runtime.ScriptDatum)
       test      eax,eax
       jne       near ptr M00_L52
       add       rbx,2
       dec       esi
       jne       short M00_L08
M00_L11:
       xor       eax,eax
M00_L12:
       add       rsp,70
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       ret
M00_L13:
       test      rsi,rsi
       je        short M00_L15
       lea       r9,[rsi-1]
       mov       ecx,1
       mov       edx,2
       cmp       r9,1
       cmova     ecx,edx
       mov       r9d,ecx
M00_L14:
       jmp       near ptr M00_L01
M00_L15:
       xor       r9d,r9d
       jmp       short M00_L14
M00_L16:
       test      rbp,rbp
       je        short M00_L18
       lea       r9,[rbp-1]
       mov       edx,1
       mov       r8d,2
       cmp       r9,1
       cmova     edx,r8d
       mov       r9d,edx
M00_L17:
       jmp       near ptr M00_L04
M00_L18:
       xor       r9d,r9d
       jmp       short M00_L17
M00_L19:
       mov       dword ptr [rsp+20],4
       mov       r9d,[rcx+8]
       mov       esi,r9d
       mov       r9d,esi
       xor       r8d,r8d
       call      qword ptr [7FF977C4C048]
       not       eax
       shr       eax,1F
       jmp       short M00_L12
M00_L20:
       mov       ecx,r14d
       call      qword ptr [7FF977C45CB0]
       mov       rax,[rax+38]
       jmp       near ptr M00_L09
M00_L21:
       mov       rax,0A033EB0008
       jmp       near ptr M00_L10
M00_L22:
       mov       [rsp+40],rbx
       mov       [rsp+48],rsi
       cmp       qword ptr [rsp+40],0
       je        short M00_L25
       mov       rcx,[rsp+40]
       mov       rax,8001400A00
       cmp       rcx,[rax]
       jne       short M00_L23
       mov       edx,1000
       jmp       short M00_L24
M00_L23:
       mov       rcx,[rsp+40]
       mov       rax,8001400A08
       movsx     rdx,word ptr [rsp+48]
       mov       r8d,2000
       cmp       rcx,[rax]
       cmove     edx,r8d
M00_L24:
       cmp       edx,4
       ja        short M00_L28
       mov       ecx,edx
       lea       rax,[7FF9778C33B8]
       mov       eax,[rax+rcx*4]
       lea       rdx,[M00_L00]
       add       rax,rdx
       jmp       rax
M00_L25:
       mov       rdx,[rsp+48]
       test      rdx,rdx
       je        short M00_L27
       dec       rdx
       mov       ecx,1
       mov       eax,2
       cmp       rdx,1
       cmova     ecx,eax
       mov       edx,ecx
M00_L26:
       jmp       short M00_L24
M00_L27:
       xor       edx,edx
       jmp       short M00_L26
M00_L28:
       cmp       edx,1000
       je        near ptr M00_L48
       cmp       edx,2000
       jne       near ptr M00_L49
       mov       rcx,offset MT_AuroraScript.Runtime.Types.UInt64Value
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       rcx,rbx
       mov       rdx,[rsp+48]
       call      qword ptr [7FF977C4C060]
       mov       rax,rbx
       jmp       near ptr M00_L50
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001400A50
       mov       rax,[rcx]
       jmp       near ptr M00_L50
       cmp       qword ptr [rsp+48],2
       je        short M00_L29
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BooleanValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80014015E0
       mov       rax,[rcx]
       jmp       short M00_L30
M00_L29:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BooleanValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80014015D8
       mov       rax,[rcx]
M00_L30:
       jmp       near ptr M00_L50
       mov       rcx,[rsp+48]
       mov       rax,8007FFFFFFFFFFFF
       add       rax,rcx
       cmp       rax,3
       jbe       short M00_L31
       vmovq     xmm1,rcx
       jmp       short M00_L33
M00_L31:
       cmp       rax,2
       jbe       short M00_L32
       vmovsd    xmm1,qword ptr [7FF9778C33D0]
       jmp       short M00_L33
M00_L32:
       vmovq     xmm1,rax
M00_L33:
       vucomisd  xmm1,xmm1
       jp        short M00_L34
       je        short M00_L35
M00_L34:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80014015C8
       mov       rax,[rcx]
       jmp       near ptr M00_L47
M00_L35:
       vxorps    xmm0,xmm0,xmm0
       vucomisd  xmm1,xmm0
       jp        short M00_L36
       jne       short M00_L36
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001401578
       mov       rax,[rcx]
       jmp       near ptr M00_L47
M00_L36:
       vucomisd  xmm1,qword ptr [7FF9778C33D8]
       jp        short M00_L37
       jne       short M00_L37
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001401580
       mov       rax,[rcx]
       jmp       near ptr M00_L47
M00_L37:
       vucomisd  xmm1,qword ptr [7FF9778C33E0]
       jp        short M00_L38
       jne       short M00_L38
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001401588
       mov       rax,[rcx]
       jmp       near ptr M00_L47
M00_L38:
       vucomisd  xmm1,qword ptr [7FF9778C33E8]
       jp        short M00_L39
       jne       short M00_L39
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001401590
       mov       rax,[rcx]
       jmp       near ptr M00_L47
M00_L39:
       vucomisd  xmm1,qword ptr [7FF9778C33F0]
       jp        short M00_L40
       jne       short M00_L40
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001401598
       mov       rax,[rcx]
       jmp       near ptr M00_L47
M00_L40:
       vucomisd  xmm1,qword ptr [7FF9778C33F8]
       jp        short M00_L41
       jne       short M00_L41
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80014015A0
       mov       rax,[rcx]
       jmp       near ptr M00_L47
M00_L41:
       vucomisd  xmm1,qword ptr [7FF9778C3400]
       jp        short M00_L42
       jne       short M00_L42
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80014015A8
       mov       rax,[rcx]
       jmp       near ptr M00_L47
M00_L42:
       vucomisd  xmm1,qword ptr [7FF9778C3408]
       jp        short M00_L43
       jne       short M00_L43
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80014015B0
       mov       rax,[rcx]
       jmp       near ptr M00_L47
M00_L43:
       vucomisd  xmm1,qword ptr [7FF9778C3410]
       jp        short M00_L44
       jne       short M00_L44
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80014015B8
       mov       rax,[rcx]
       jmp       near ptr M00_L47
M00_L44:
       vucomisd  xmm1,qword ptr [7FF9778C3418]
       jp        short M00_L45
       jne       short M00_L45
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80014015C0
       mov       rax,[rcx]
       jmp       short M00_L47
M00_L45:
       vmovsd    qword ptr [rsp+38],xmm1
       vucomisd  xmm1,qword ptr [7FF9778C3420]
       jp        short M00_L46
       jne       short M00_L46
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001401570
       mov       rax,[rcx]
       jmp       short M00_L47
M00_L46:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       rcx,rbx
       vmovsd    xmm1,qword ptr [rsp+38]
       call      qword ptr [7FF977C4C078]
       mov       rax,rbx
M00_L47:
       jmp       short M00_L50
M00_L48:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.Int64Value
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       rcx,rbx
       mov       rdx,[rsp+48]
       call      qword ptr [7FF977C4C090]
       mov       rax,rbx
       jmp       short M00_L50
       lea       rcx,[rsp+40]
       call      qword ptr [7FF977C4C0A8]
       jmp       short M00_L50
M00_L49:
       lea       rcx,[rsp+40]
       call      qword ptr [7FF977C4C0C0]
M00_L50:
       mov       rcx,rax
       mov       rax,[rax]
       mov       rax,[rax+58]
       call      qword ptr [rax]
       mov       rbx,rax
M00_L51:
       lea       rdx,[rsp+60]
       mov       rcx,rbx
       cmp       [rcx],ecx
       call      qword ptr [7FF977C45C80]
       test      eax,eax
       je        near ptr M00_L11
       mov       [rsp+28],rdi
       mov       [rsp+30],rbp
       lea       rdx,[rsp+28]
       lea       rcx,[rsp+60]
       call      qword ptr [7FF977C45C50]; AuroraScript.Runtime.ScriptDatum.Equals(AuroraScript.Runtime.ScriptDatum)
       test      eax,eax
       je        short M00_L51
M00_L52:
       mov       eax,1
       jmp       near ptr M00_L12
; Total bytes of code 1652
```
```assembly
; AuroraScript.Runtime.ScriptDatum.Equals(AuroraScript.Runtime.ScriptDatum)
       sub       rsp,58
       xor       eax,eax
       mov       [rsp+28],rax
       vxorps    xmm4,xmm4,xmm4
       vmovdqu   ymmword ptr [rsp+30],ymm4
       mov       [rsp+50],rax
M01_L00:
       vmovdqu   xmm0,xmmword ptr [rdx]
       vmovdqu   xmmword ptr [rsp+48],xmm0
       vmovdqu   xmm0,xmmword ptr [rcx]
       vmovdqu   xmmword ptr [rsp+38],xmm0
       cmp       qword ptr [rsp+48],0
       je        near ptr M01_L09
       mov       r8,[rsp+48]
       mov       rdx,8001400A00
       cmp       r8,[rdx]
       je        near ptr M01_L08
       mov       r8,[rsp+48]
       mov       rdx,8001400A08
       movsx     rax,word ptr [rsp+50]
       mov       ecx,2000
       cmp       r8,[rdx]
       cmove     eax,ecx
M01_L01:
       cmp       qword ptr [rsp+38],0
       je        near ptr M01_L13
       mov       r8,[rsp+38]
       mov       rdx,8001400A00
       cmp       r8,[rdx]
       je        near ptr M01_L12
       mov       r8,[rsp+38]
       mov       rdx,8001400A08
       movsx     r10,word ptr [rsp+40]
       mov       ecx,2000
       cmp       r8,[rdx]
       cmove     r10d,ecx
M01_L02:
       cmp       eax,r10d
       jne       near ptr M01_L29
       cmp       r10d,4
       jne       near ptr M01_L16
       mov       rcx,[rsp+48]
       test      rcx,rcx
       je        short M01_L03
       mov       r8,offset MT_System.String
       cmp       [rcx],r8
       jne       short M01_L03
       jmp       short M01_L04
M01_L03:
       xor       ecx,ecx
M01_L04:
       mov       rdx,[rsp+38]
       test      rdx,rdx
       je        short M01_L05
       mov       r8,offset MT_System.String
       cmp       [rdx],r8
       jne       short M01_L05
       jmp       short M01_L06
M01_L05:
       xor       edx,edx
M01_L06:
       cmp       rcx,rdx
       je        near ptr M01_L26
       test      rcx,rcx
       je        near ptr M01_L27
       test      rdx,rdx
       je        near ptr M01_L27
       mov       r8d,[rcx+8]
       cmp       r8d,[rdx+8]
       jne       near ptr M01_L27
       add       rcx,0C
       add       r8d,r8d
       add       rdx,0C
       call      qword ptr [7FF97784C330]; System.SpanHelpers.SequenceEqual(Byte ByRef, Byte ByRef, UIntPtr)
M01_L07:
       movzx     eax,al
       add       rsp,58
       ret
M01_L08:
       mov       eax,1000
       jmp       near ptr M01_L01
M01_L09:
       mov       rax,[rsp+50]
       test      rax,rax
       je        short M01_L11
       dec       rax
       mov       r8d,1
       mov       edx,2
       cmp       rax,1
       cmova     r8d,edx
       mov       eax,r8d
M01_L10:
       jmp       near ptr M01_L01
M01_L11:
       xor       eax,eax
       jmp       short M01_L10
M01_L12:
       mov       r10d,1000
       jmp       near ptr M01_L02
M01_L13:
       mov       r10,[rsp+40]
       test      r10,r10
       je        short M01_L15
       dec       r10
       mov       r8d,1
       mov       edx,2
       cmp       r10,1
       cmova     r8d,edx
       mov       r10d,r8d
M01_L14:
       jmp       near ptr M01_L02
M01_L15:
       xor       r10d,r10d
       jmp       short M01_L14
M01_L16:
       cmp       r10d,4
       ja        short M01_L17
       mov       r8d,r10d
       lea       rdx,[7FF9778C2CF8]
       mov       edx,[rdx+r8*4]
       lea       rcx,[M01_L00]
       add       rdx,rcx
       jmp       rdx
M01_L17:
       cmp       r10d,1000
       je        near ptr M01_L25
       cmp       r10d,2000
       jne       near ptr M01_L28
       mov       rax,[rsp+50]
       cmp       rax,[rsp+40]
       sete      al
       movzx     eax,al
       jmp       near ptr M01_L07
       mov       eax,1
       jmp       near ptr M01_L07
       cmp       qword ptr [rsp+50],2
       sete      al
       movzx     eax,al
       cmp       qword ptr [rsp+40],2
       sete      cl
       movzx     ecx,cl
       cmp       eax,ecx
       sete      al
       movzx     eax,al
       jmp       near ptr M01_L07
       mov       rax,[rsp+50]
       mov       rcx,8007FFFFFFFFFFFF
       add       rcx,rax
       cmp       rcx,3
       jbe       short M01_L18
       vmovq     xmm0,rax
       jmp       short M01_L20
M01_L18:
       cmp       rcx,2
       jbe       short M01_L19
       vmovsd    xmm0,qword ptr [7FF9778C2D10]
       jmp       short M01_L20
M01_L19:
       vmovq     xmm0,rcx
M01_L20:
       mov       rcx,[rsp+40]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M01_L21
       vmovq     xmm1,rcx
       jmp       short M01_L23
M01_L21:
       cmp       rdx,2
       jbe       short M01_L22
       vmovsd    xmm1,qword ptr [7FF9778C2D10]
       jmp       short M01_L23
M01_L22:
       vmovq     xmm1,rdx
M01_L23:
       vucomisd  xmm0,xmm1
       setnp     al
       jp        short M01_L24
       sete      al
M01_L24:
       movzx     eax,al
       jmp       near ptr M01_L07
M01_L25:
       mov       rax,[rsp+50]
       cmp       rax,[rsp+40]
       sete      al
       movzx     eax,al
       jmp       near ptr M01_L07
M01_L26:
       mov       eax,1
       jmp       near ptr M01_L07
M01_L27:
       xor       eax,eax
       jmp       near ptr M01_L07
M01_L28:
       mov       rax,[rsp+48]
       cmp       rax,[rsp+38]
       sete      al
       movzx     eax,al
       jmp       near ptr M01_L07
M01_L29:
       cmp       eax,1000
       jne       short M01_L30
       cmp       r10d,2000
       jne       short M01_L30
       cmp       qword ptr [rsp+50],0
       jl        near ptr M01_L43
       mov       rax,[rsp+50]
       cmp       rax,[rsp+40]
       sete      al
       movzx     eax,al
       jmp       near ptr M01_L07
M01_L30:
       cmp       eax,2000
       jne       short M01_L31
       cmp       r10d,1000
       jne       short M01_L31
       cmp       qword ptr [rsp+40],0
       jl        near ptr M01_L43
       mov       rcx,[rsp+50]
       cmp       rcx,[rsp+40]
       sete      al
       movzx     eax,al
       jmp       near ptr M01_L07
M01_L31:
       cmp       qword ptr [rsp+48],0
       jne       short M01_L35
       cmp       qword ptr [rsp+50],2
       jbe       short M01_L35
       mov       rcx,[rsp+50]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M01_L32
       vmovq     xmm0,rcx
       jmp       short M01_L34
M01_L32:
       cmp       rdx,2
       jbe       short M01_L33
       vmovsd    xmm0,qword ptr [7FF9778C2D10]
       jmp       short M01_L34
M01_L33:
       vmovq     xmm0,rdx
M01_L34:
       vmovsd    qword ptr [rsp+30],xmm0
       jmp       short M01_L36
M01_L35:
       lea       rcx,[rsp+48]
       lea       rdx,[rsp+30]
       call      qword ptr [7FF977C4C030]
       test      eax,eax
       je        near ptr M01_L43
M01_L36:
       cmp       qword ptr [rsp+38],0
       jne       short M01_L40
       cmp       qword ptr [rsp+40],2
       jbe       short M01_L40
       mov       rcx,[rsp+40]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M01_L37
       vmovq     xmm0,rcx
       jmp       short M01_L39
M01_L37:
       cmp       rdx,2
       jbe       short M01_L38
       vmovsd    xmm0,qword ptr [7FF9778C2D10]
       jmp       short M01_L39
M01_L38:
       vmovq     xmm0,rdx
M01_L39:
       vmovsd    qword ptr [rsp+28],xmm0
       jmp       short M01_L41
M01_L40:
       lea       rcx,[rsp+38]
       lea       rdx,[rsp+28]
       call      qword ptr [7FF977C4C030]
       test      eax,eax
       je        short M01_L43
M01_L41:
       vmovsd    xmm0,qword ptr [rsp+30]
       vucomisd  xmm0,qword ptr [rsp+28]
       setnp     al
       jp        short M01_L42
       sete      al
M01_L42:
       movzx     eax,al
       jmp       near ptr M01_L07
M01_L43:
       xor       eax,eax
       add       rsp,58
       ret
; Total bytes of code 1074
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
; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rax,[rcx+20]
       mov       rax,[rax-10]
       mov       rdx,rax
       test      dl,1
       jne       short M03_L00
       ret
M03_L00:
       jmp       qword ptr [7FF977A1E838]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBaseSlow(System.Runtime.CompilerServices.MethodTable*)
; Total bytes of code 23
```
```assembly
; System.SpanHelpers.SequenceEqual(Byte ByRef, Byte ByRef, UIntPtr)
       cmp       r8,8
       jae       short M04_L05
       cmp       r8,4
       jae       short M04_L04
       xor       eax,eax
       mov       r10,r8
       and       r10,2
       je        short M04_L00
       movzx     eax,word ptr [rcx]
       movzx     r9d,word ptr [rdx]
       sub       eax,r9d
M04_L00:
       test      r8b,1
       jne       short M04_L03
M04_L01:
       test      eax,eax
       sete      al
       movzx     eax,al
M04_L02:
       vzeroupper
       ret
M04_L03:
       movzx     r8d,byte ptr [rcx+r10]
       movzx     ecx,byte ptr [rdx+r10]
       sub       r8d,ecx
       or        eax,r8d
       jmp       short M04_L01
M04_L04:
       add       r8,0FFFFFFFFFFFFFFFC
       mov       eax,[rcx]
       sub       eax,[rdx]
       mov       ecx,[rcx+r8]
       sub       ecx,[rdx+r8]
       or        eax,ecx
       sete      al
       movzx     eax,al
       jmp       short M04_L02
M04_L05:
       cmp       rcx,rdx
       je        near ptr M04_L11
       cmp       r8,20
       jb        short M04_L08
       xor       eax,eax
       add       r8,0FFFFFFFFFFFFFFE0
       je        short M04_L07
M04_L06:
       vmovups   ymm0,[rcx+rax]
       vpcmpeqb  ymm0,ymm0,[rdx+rax]
       vpmovmskb r10d,ymm0
       cmp       r10d,0FFFFFFFF
       jne       near ptr M04_L13
       add       rax,20
       cmp       r8,rax
       ja        short M04_L06
M04_L07:
       vmovups   ymm0,[rcx+r8]
       vpcmpeqb  ymm0,ymm0,[rdx+r8]
       vpmovmskb ecx,ymm0
       cmp       ecx,0FFFFFFFF
       jne       short M04_L13
       jmp       short M04_L11
M04_L08:
       cmp       r8,10
       jb        short M04_L12
       xor       eax,eax
       add       r8,0FFFFFFFFFFFFFFF0
       je        short M04_L10
M04_L09:
       vmovups   xmm0,[rcx+rax]
       vpcmpeqb  xmm0,xmm0,[rdx+rax]
       vpmovmskb r10d,xmm0
       cmp       r10d,0FFFF
       jne       short M04_L13
       add       rax,10
       cmp       r8,rax
       ja        short M04_L09
M04_L10:
       vmovups   xmm0,[rcx+r8]
       vpcmpeqb  xmm0,xmm0,[rdx+r8]
       vpmovmskb eax,xmm0
       cmp       eax,0FFFF
       jne       short M04_L13
M04_L11:
       mov       eax,1
       vzeroupper
       ret
M04_L12:
       lea       rax,[r8-8]
       mov       r8,[rcx]
       sub       r8,[rdx]
       mov       rcx,[rcx+rax]
       sub       rcx,[rdx+rax]
       or        rcx,r8
       sete      al
       movzx     eax,al
       jmp       near ptr M04_L02
M04_L13:
       xor       eax,eax
       vzeroupper
       ret
; Total bytes of code 290
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
       push      rsi
       push      rbx
       sub       rsp,48
       xor       eax,eax
       mov       [rsp+28],rax
       vxorps    xmm4,xmm4,xmm4
       vmovdqa   xmmword ptr [rsp+30],xmm4
       mov       [rsp+40],rax
       mov       rbx,[rcx+50]
       mov       r8,[rcx+58]
       test      rbx,rbx
       je        near ptr M00_L10
       mov       rcx,0A0A5400A00
       cmp       rbx,[rcx]
       je        short M00_L01
       mov       rcx,0A0A5400A08
       cmp       rbx,[rcx]
       je        short M00_L01
       movsx     rcx,r8w
M00_L00:
       test      ecx,ecx
       je        near ptr M00_L22
M00_L01:
       test      rbx,rbx
       je        near ptr M00_L13
       mov       rcx,0A0A5400A00
       cmp       rbx,[rcx]
       je        short M00_L03
       mov       rcx,0A0A5400A08
       cmp       rbx,[rcx]
       je        short M00_L03
       movsx     rcx,r8w
M00_L02:
       test      ecx,ecx
       je        short M00_L09
M00_L03:
       test      rbx,rbx
       je        short M00_L04
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt32Array
       cmp       [rbx],rcx
       je        short M00_L09
M00_L04:
       mov       [rsp+28],rbx
       mov       [rsp+30],r8
       lea       r8,[rsp+28]
       lea       rcx,[rsp+38]
       mov       edx,6
       call      qword ptr [7FF977C25BA8]
       mov       rbx,[rsp+38]
M00_L05:
       mov       rdx,rbx
       test      rdx,rdx
       je        short M00_L06
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt32Array
       cmp       [rdx],rcx
       jne       short M00_L16
M00_L06:
       test      rdx,rdx
       je        near ptr M00_L17
M00_L07:
       mov       rax,rdx
       test      rax,rax
       je        short M00_L08
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt32Array
       cmp       [rax],rcx
       jne       near ptr M00_L21
M00_L08:
       add       rsp,48
       pop       rbx
       pop       rsi
       ret
M00_L09:
       jmp       short M00_L05
M00_L10:
       test      r8,r8
       je        short M00_L12
       lea       rcx,[r8-1]
       mov       edx,1
       mov       eax,2
       cmp       rcx,1
       cmova     edx,eax
       mov       ecx,edx
M00_L11:
       jmp       near ptr M00_L00
M00_L12:
       xor       ecx,ecx
       jmp       short M00_L11
M00_L13:
       test      r8,r8
       je        short M00_L15
       lea       rcx,[r8-1]
       mov       edx,1
       mov       eax,2
       cmp       rcx,1
       cmova     edx,eax
       mov       ecx,edx
M00_L14:
       jmp       near ptr M00_L02
M00_L15:
       xor       ecx,ecx
       jmp       short M00_L14
M00_L16:
       mov       rdx,rbx
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdx,rax
       jmp       near ptr M00_L06
M00_L17:
       mov       rdx,rbx
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbx,rax
       test      rbx,rbx
       jne       short M00_L18
       xor       edx,edx
       jmp       near ptr M00_L07
M00_L18:
       cmp       dword ptr [rbx+8],0
       jne       short M00_L19
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0A0A54014A8
       mov       rdx,[rcx]
       jmp       short M00_L20
M00_L19:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      CORINFO_HELP_NEWSFAST
       mov       rsi,rax
       mov       rcx,rsi
       mov       rdx,rbx
       call      qword ptr [7FF977C27A38]
       mov       rdx,rsi
M00_L20:
       jmp       near ptr M00_L07
M00_L21:
       call      System.Runtime.CompilerServices.CastHelpers.ChkCastClass(Void*, System.Object)
       int       3
M00_L22:
       xor       eax,eax
       jmp       near ptr M00_L08
; Total bytes of code 479
```
```assembly
; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rdx,rdx
       je        short M01_L01
       mov       rax,[rdx]
       cmp       rax,rcx
       je        short M01_L01
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M01_L01
M01_L00:
       test      rax,rax
       je        short M01_L03
       mov       rax,[rax+10]
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
       jmp       short M01_L00
M01_L03:
       xor       edx,edx
       jmp       short M01_L01
; Total bytes of code 88
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
       jmp       qword ptr [7FF977825C38]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBaseSlow(System.Runtime.CompilerServices.MethodTable*)
; Total bytes of code 23
```
```assembly
; System.Runtime.CompilerServices.CastHelpers.ChkCastClass(Void*, System.Object)
       test      rdx,rdx
       je        short M03_L00
       cmp       [rdx],rcx
       jne       short M03_L01
M03_L00:
       mov       rax,rdx
       ret
M03_L01:
       jmp       qword ptr [7FF9779F4D20]; System.Runtime.CompilerServices.CastHelpers.ChkCastClassSpecial(Void*, System.Object)
; Total bytes of code 20
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
; System.Runtime.CompilerServices.CastHelpers.ChkCastClassSpecial(Void*, System.Object)
       mov       rax,[rdx]
       mov       rax,[rax+10]
       cmp       rax,rcx
       jne       short M05_L01
M05_L00:
       mov       rax,rdx
       ret
M05_L01:
       test      rax,rax
       je        short M05_L04
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M05_L00
       jmp       short M05_L03
M05_L02:
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M05_L00
       jmp       short M05_L01
M05_L03:
       test      rax,rax
       je        short M05_L04
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M05_L00
       test      rax,rax
       je        short M05_L04
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M05_L00
       test      rax,rax
       jne       short M05_L02
M05_L04:
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
       je        short M06_L00
       mov       rcx,rsi
       cmp       [r8],rcx
       je        short M06_L00
       mov       rdx,rax
       call      qword ptr [7FF96E839090]; Precode of System.Runtime.CompilerServices.CastHelpers.ChkCastAny(Void*, System.Object)
       mov       r8,rax
M06_L00:
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
       sub       rsp,58
       xor       eax,eax
       mov       [rsp+28],rax
       vxorps    xmm4,xmm4,xmm4
       vmovdqu   ymmword ptr [rsp+30],ymm4
       mov       [rsp+50],rax
       mov       rbx,[rcx+60]
       mov       rsi,[rcx+68]
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rsp+48],xmm0
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
       add       rsp,58
       pop       rbx
       pop       rsi
       ret
M00_L02:
       mov       rdx,0CB4BC00A00
       cmp       rbx,[rdx]
       je        short M00_L04
       mov       rdx,0CB4BC00A08
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
       test      rbx,rbx
       je        short M00_L05
       mov       rdx,0CB4BC00A00
       cmp       rbx,[rdx]
       je        short M00_L09
       mov       rdx,0CB4BC00A08
       cmp       rbx,[rdx]
       je        short M00_L09
       movsx     rdx,si
       jmp       short M00_L08
M00_L05:
       test      rsi,rsi
       je        short M00_L07
       lea       rdx,[rsi-1]
       mov       ecx,1
       mov       eax,2
       cmp       rdx,1
       mov       edx,eax
       cmovbe    edx,ecx
M00_L06:
       jmp       short M00_L08
M00_L07:
       xor       edx,edx
       jmp       short M00_L06
M00_L08:
       test      edx,edx
       je        short M00_L10
M00_L09:
       mov       rdx,rbx
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt32Array
       call      qword ptr [7FF977846850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rax,rax
       jne       short M00_L10
       mov       [rsp+28],rbx
       mov       [rsp+30],rsi
       lea       r8,[rsp+28]
       lea       rcx,[rsp+38]
       mov       edx,6
       call      qword ptr [7FF977C479F0]
       jmp       short M00_L11
M00_L10:
       mov       [rsp+38],rbx
       mov       [rsp+40],rsi
M00_L11:
       vmovdqu   xmm0,xmmword ptr [rsp+38]
       vmovdqu   xmmword ptr [rsp+48],xmm0
       lea       rcx,[rsp+48]
       call      qword ptr [7FF977C45B78]
       mov       rdx,rax
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt32Array
       call      qword ptr [7FF977846328]; System.Runtime.CompilerServices.CastHelpers.ChkCastClass(Void*, System.Object)
       jmp       near ptr M00_L01
; Total bytes of code 325
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
       jmp       qword ptr [7FF977A14D20]; System.Runtime.CompilerServices.CastHelpers.ChkCastClassSpecial(Void*, System.Object)
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

