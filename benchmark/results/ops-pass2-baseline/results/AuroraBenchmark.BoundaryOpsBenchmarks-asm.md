## .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3 (Job: Job-BZNPUE(IterationCount=5, IterationTime=200ms, LaunchCount=1, WarmupCount=3))

```assembly
; AuroraBenchmark.BoundaryOpsBenchmarks.ReadDynamicArray()
       push      r15
       push      r14
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,0A8
       vmovaps   [rsp+90],xmm6
       vxorps    xmm4,xmm4,xmm4
       vmovdqu   ymmword ptr [rsp+20],ymm4
       vmovdqu   ymmword ptr [rsp+40],ymm4
       vmovdqu   ymmword ptr [rsp+60],ymm4
       vmovdqa   xmmword ptr [rsp+80],xmm4
       mov       rbx,rcx
M00_L00:
       vxorps    xmm6,xmm6,xmm6
       xor       esi,esi
       jmp       near ptr M00_L07
M00_L01:
       test      r14,r14
       je        near ptr M00_L16
       cmp       qword ptr [rsp+70],0
       jne       near ptr M00_L13
       cmp       qword ptr [rsp+78],2
       jbe       near ptr M00_L13
       mov       rcx,[rsp+78]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       near ptr M00_L09
       vmovq     xmm0,rcx
M00_L02:
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       rcx,7FFFFFFFFFFFFFFF
       vcvttsd2si rdx,xmm1
       vucomisd  xmm0,qword ptr [7FF977892048]
       cmovb     rcx,rdx
       mov       [rsp+68],rcx
M00_L03:
       mov       r8,offset MT_AuroraScript.Runtime.Types.ScriptArray
       cmp       [r14],r8
       jne       near ptr M00_L15
       mov       edx,[rsp+68]
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rsp+58],xmm0
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rsp+58],xmm0
       cmp       edx,[r14+34]
       jae       near ptr M00_L14
       mov       r8,[r14+38]
       cmp       edx,[r8+8]
       jae       near ptr M00_L107
       mov       ecx,edx
       shl       rcx,4
       vmovdqu   xmm0,xmmword ptr [r8+rcx+10]
       vmovdqu   xmmword ptr [rsp+58],xmm0
M00_L04:
       mov       r15,[rsp+60]
M00_L05:
       mov       rax,8007FFFFFFFFFFFF
       add       rax,r15
       cmp       rax,3
       jbe       near ptr M00_L10
       vmovq     xmm0,r15
M00_L06:
       vaddsd    xmm6,xmm6,xmm0
       inc       esi
       cmp       esi,200
       jge       short M00_L08
M00_L07:
       mov       rdi,[rbx+48]
       mov       rbp,[rbx+50]
       mov       rdx,[rbx+8]
       mov       ecx,esi
       and       ecx,1F
       cmp       ecx,[rdx+8]
       jae       near ptr M00_L107
       shl       rcx,4
       vmovdqu   xmm0,xmmword ptr [rdx+rcx+10]
       vmovdqu   xmmword ptr [rsp+70],xmm0
       xor       edx,edx
       mov       [rsp+68],rdx
       mov       r14,rdi
       test      r14,r14
       je        near ptr M00_L01
       mov       rdx,offset MT_AuroraScript.Runtime.Types.ScriptArray
       cmp       [r14],rdx
       je        near ptr M00_L01
       jmp       short M00_L11
M00_L08:
       vmovaps   xmm0,xmm6
       vmovaps   xmm6,[rsp+90]
       add       rsp,0A8
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       pop       r15
       ret
M00_L09:
       cmp       rdx,2
       ja        short M00_L12
       vmovq     xmm0,rdx
       jmp       near ptr M00_L02
M00_L10:
       cmp       rax,2
       ja        near ptr M00_L106
       vmovq     xmm0,rax
       jmp       near ptr M00_L06
M00_L11:
       mov       rdx,rdi
       mov       rcx,offset MT_AuroraScript.Runtime.Types.IAuroraNativeIndexer
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfInterface(Void*, System.Object)
       mov       r14,rax
       jmp       near ptr M00_L01
M00_L12:
       vmovsd    xmm0,qword ptr [7FF977892050]
       jmp       near ptr M00_L02
M00_L13:
       lea       rcx,[rsp+70]
       lea       rdx,[rsp+68]
       call      qword ptr [7FF977C15DB8]
       test      eax,eax
       je        short M00_L16
       jmp       near ptr M00_L03
M00_L14:
       lea       r8,[rsp+58]
       mov       rcx,r14
       call      qword ptr [7FF977C15E18]
       jmp       near ptr M00_L04
M00_L15:
       lea       rdx,[rsp+80]
       mov       rcx,r14
       mov       r8d,[rsp+68]
       mov       r11,7FF977760500
       call      qword ptr [r11]
       jmp       near ptr M00_L105
M00_L16:
       mov       rdx,rdi
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptPackedArray
       call      qword ptr [7FF977816850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r15,rax
       test      r15,r15
       je        near ptr M00_L69
       cmp       qword ptr [rsp+70],0
       jne       short M00_L20
       cmp       qword ptr [rsp+78],2
       jbe       short M00_L20
       mov       rcx,[rsp+78]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L17
       vmovq     xmm0,rcx
       jmp       short M00_L19
M00_L17:
       cmp       rdx,2
       jbe       short M00_L18
       vmovsd    xmm0,qword ptr [7FF977892050]
       jmp       short M00_L19
M00_L18:
       vmovq     xmm0,rdx
M00_L19:
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       rcx,7FFFFFFFFFFFFFFF
       vcvttsd2si rdx,xmm1
       vucomisd  xmm0,qword ptr [7FF977892048]
       cmovb     rcx,rdx
       mov       [rsp+68],rcx
       jmp       short M00_L21
M00_L20:
       lea       rcx,[rsp+70]
       lea       rdx,[rsp+68]
       call      qword ptr [7FF977C15DB8]
       test      eax,eax
       je        near ptr M00_L69
M00_L21:
       mov       edi,[rsp+68]
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt32Array
       call      qword ptr [7FF977816850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       near ptr M00_L22
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt8Array
       call      qword ptr [7FF977816850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       near ptr M00_L26
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptFloat32Array
       call      qword ptr [7FF977816850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       near ptr M00_L30
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptFloat64Array
       call      qword ptr [7FF977816850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       near ptr M00_L36
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptBooleanArray
       call      qword ptr [7FF977816850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       near ptr M00_L42
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptUInt8Array
       call      qword ptr [7FF977816850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       near ptr M00_L46
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt16Array
       call      qword ptr [7FF977816850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       near ptr M00_L50
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptUInt16Array
       call      qword ptr [7FF977816850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       near ptr M00_L54
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptUInt32Array
       call      qword ptr [7FF977816850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       near ptr M00_L58
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt64Array
       call      qword ptr [7FF977816850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       near ptr M00_L62
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptUInt64Array
       call      qword ptr [7FF977816850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       near ptr M00_L64
       mov       rcx,r15
       mov       rax,[r15]
       mov       rax,[rax+58]
       call      qword ptr [rax+8]
       cmp       eax,edi
       ja        near ptr M00_L67
       jmp       near ptr M00_L66
M00_L22:
       mov       rdx,[rbp+38]
       cmp       [rdx+8],edi
       ja        short M00_L23
       mov       rdx,[rbp+38]
       mov       edx,[rdx+8]
       mov       ecx,edi
       call      qword ptr [7FF977C1C5A0]
M00_L23:
       mov       rcx,[rbp+38]
       cmp       edi,[rcx+8]
       jae       near ptr M00_L107
       mov       eax,edi
       mov       ecx,[rcx+rax*4+10]
       test      ecx,ecx
       je        short M00_L24
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2sd xmm0,xmm0,ecx
       vmovq     r15,xmm0
       jmp       short M00_L25
M00_L24:
       mov       r15,7FF8000000000001
M00_L25:
       jmp       near ptr M00_L68
M00_L26:
       mov       rdx,[rbp+38]
       cmp       [rdx+8],edi
       ja        short M00_L27
       mov       rdx,[rbp+38]
       mov       edx,[rdx+8]
       mov       ecx,edi
       call      qword ptr [7FF977C1C5A0]
M00_L27:
       mov       rdx,[rbp+38]
       cmp       edi,[rdx+8]
       jae       near ptr M00_L107
       mov       ecx,edi
       movsx     rdx,byte ptr [rdx+rcx+10]
       test      edx,edx
       je        short M00_L28
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2sd xmm0,xmm0,edx
       vmovq     r15,xmm0
       jmp       short M00_L29
M00_L28:
       mov       r15,7FF8000000000001
M00_L29:
       jmp       near ptr M00_L68
M00_L30:
       mov       rdx,[rbp+38]
       cmp       [rdx+8],edi
       ja        short M00_L31
       mov       rdx,[rbp+38]
       mov       edx,[rdx+8]
       mov       ecx,edi
       call      qword ptr [7FF977C1C5A0]
M00_L31:
       mov       rdx,[rbp+38]
       cmp       edi,[rdx+8]
       jae       near ptr M00_L107
       mov       ecx,edi
       vcvtss2sd xmm0,xmm0,dword ptr [rdx+rcx*4+10]
       vucomisd  xmm0,xmm0
       jp        short M00_L32
       je        short M00_L33
M00_L32:
       mov       r15,7FF8000000000004
       jmp       short M00_L35
M00_L33:
       vmovq     r15,xmm0
       cmp       r15,2
       jbe       short M00_L34
       jmp       short M00_L35
M00_L34:
       mov       rdx,7FF8000000000001
       add       r15,rdx
M00_L35:
       jmp       near ptr M00_L68
M00_L36:
       mov       rdx,[rbp+38]
       cmp       [rdx+8],edi
       ja        short M00_L37
       mov       rdx,[rbp+38]
       mov       edx,[rdx+8]
       mov       ecx,edi
       call      qword ptr [7FF977C1C5A0]
M00_L37:
       mov       rdx,[rbp+38]
       cmp       edi,[rdx+8]
       jae       near ptr M00_L107
       mov       ecx,edi
       vmovsd    xmm0,qword ptr [rdx+rcx*8+10]
       vucomisd  xmm0,xmm0
       jp        short M00_L38
       je        short M00_L39
M00_L38:
       mov       r15,7FF8000000000004
       jmp       short M00_L41
M00_L39:
       vmovq     r15,xmm0
       cmp       r15,2
       jbe       short M00_L40
       jmp       short M00_L41
M00_L40:
       mov       rdx,7FF8000000000001
       add       r15,rdx
M00_L41:
       jmp       near ptr M00_L68
M00_L42:
       mov       rdx,[rbp+38]
       cmp       [rdx+8],edi
       ja        short M00_L43
       mov       rdx,[rbp+38]
       mov       edx,[rdx+8]
       mov       ecx,edi
       call      qword ptr [7FF977C1C5A0]
M00_L43:
       mov       rdx,[rbp+38]
       cmp       edi,[rdx+8]
       jae       near ptr M00_L107
       mov       ecx,edi
       movzx     edx,byte ptr [rdx+rcx+10]
       test      edx,edx
       jne       short M00_L44
       mov       r15d,1
       jmp       short M00_L45
M00_L44:
       mov       r15d,2
M00_L45:
       jmp       near ptr M00_L68
M00_L46:
       mov       rdx,[rbp+38]
       cmp       [rdx+8],edi
       ja        short M00_L47
       mov       rdx,[rbp+38]
       mov       edx,[rdx+8]
       mov       ecx,edi
       call      qword ptr [7FF977C1C5A0]
M00_L47:
       mov       rdx,[rbp+38]
       cmp       edi,[rdx+8]
       jae       near ptr M00_L107
       mov       ecx,edi
       movzx     edx,byte ptr [rdx+rcx+10]
       test      edx,edx
       je        short M00_L48
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2sd xmm0,xmm0,edx
       vmovq     r15,xmm0
       jmp       short M00_L49
M00_L48:
       mov       r15,7FF8000000000001
M00_L49:
       jmp       near ptr M00_L68
M00_L50:
       mov       rdx,[rbp+38]
       cmp       [rdx+8],edi
       ja        short M00_L51
       mov       rdx,[rbp+38]
       mov       edx,[rdx+8]
       mov       ecx,edi
       call      qword ptr [7FF977C1C5A0]
M00_L51:
       mov       rdx,[rbp+38]
       cmp       edi,[rdx+8]
       jae       near ptr M00_L107
       mov       ecx,edi
       movsx     rdx,word ptr [rdx+rcx*2+10]
       test      edx,edx
       je        short M00_L52
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2sd xmm0,xmm0,edx
       vmovq     r15,xmm0
       jmp       short M00_L53
M00_L52:
       mov       r15,7FF8000000000001
M00_L53:
       jmp       near ptr M00_L68
M00_L54:
       mov       rdx,[rbp+38]
       cmp       [rdx+8],edi
       ja        short M00_L55
       mov       rdx,[rbp+38]
       mov       edx,[rdx+8]
       mov       ecx,edi
       call      qword ptr [7FF977C1C5A0]
M00_L55:
       mov       rdx,[rbp+38]
       cmp       edi,[rdx+8]
       jae       near ptr M00_L107
       mov       ecx,edi
       movzx     edx,word ptr [rdx+rcx*2+10]
       test      edx,edx
       je        short M00_L56
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2sd xmm0,xmm0,edx
       vmovq     r15,xmm0
       jmp       short M00_L57
M00_L56:
       mov       r15,7FF8000000000001
M00_L57:
       jmp       near ptr M00_L68
M00_L58:
       mov       rdx,[rbp+38]
       cmp       [rdx+8],edi
       ja        short M00_L59
       mov       rdx,[rbp+38]
       mov       edx,[rdx+8]
       mov       ecx,edi
       call      qword ptr [7FF977C1C5A0]
M00_L59:
       mov       rdx,[rbp+38]
       cmp       edi,[rdx+8]
       jae       near ptr M00_L107
       mov       ecx,edi
       mov       edx,[rdx+rcx*4+10]
       test      edx,edx
       je        short M00_L60
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2sd xmm0,xmm0,rdx
       vmovq     r15,xmm0
       jmp       short M00_L61
M00_L60:
       mov       r15,7FF8000000000001
M00_L61:
       jmp       near ptr M00_L68
M00_L62:
       mov       rdx,[rbp+38]
       cmp       [rdx+8],edi
       ja        short M00_L63
       mov       rdx,[rbp+38]
       mov       edx,[rdx+8]
       mov       ecx,edi
       call      qword ptr [7FF977C1C5A0]
M00_L63:
       mov       rcx,[rbp+38]
       cmp       edi,[rcx+8]
       jae       near ptr M00_L107
       mov       eax,edi
       mov       r15,[rcx+rax*8+10]
       mov       rcx,offset MT_AuroraScript.Runtime.ScriptDatum
       call      qword ptr [7FF977815728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       jmp       short M00_L68
M00_L64:
       mov       rdx,[rbp+38]
       cmp       [rdx+8],edi
       ja        short M00_L65
       mov       rdx,[rbp+38]
       mov       edx,[rdx+8]
       mov       ecx,edi
       call      qword ptr [7FF977C1C5A0]
M00_L65:
       mov       rcx,[rbp+38]
       cmp       edi,[rcx+8]
       jae       near ptr M00_L107
       mov       eax,edi
       mov       r15,[rcx+rax*8+10]
       mov       rcx,offset MT_AuroraScript.Runtime.ScriptDatum
       call      qword ptr [7FF977815728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       jmp       short M00_L68
M00_L66:
       mov       rcx,r15
       mov       rax,[r15]
       mov       rax,[rax+58]
       call      qword ptr [rax+8]
       mov       edx,eax
       mov       ecx,edi
       call      qword ptr [7FF977C1C5A0]
M00_L67:
       lea       rdx,[rsp+48]
       mov       rcx,r15
       mov       r8d,edi
       mov       rax,[r15]
       mov       rax,[rax+58]
       call      qword ptr [rax+10]
       mov       r15,[rsp+50]
M00_L68:
       jmp       near ptr M00_L05
M00_L69:
       mov       [rsp+38],rdi
       mov       [rsp+40],rbp
       cmp       qword ptr [rsp+38],0
       je        short M00_L72
       mov       r15,[rsp+38]
       mov       rcx,offset MT_AuroraScript.Runtime.ScriptDatum
       call      qword ptr [7FF977815728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0C10F000450
       cmp       r15,[rcx]
       jne       short M00_L70
       mov       eax,1000
       jmp       short M00_L75
M00_L70:
       mov       rdi,[rsp+38]
       mov       rcx,offset MT_AuroraScript.Runtime.ScriptDatum
       call      qword ptr [7FF977815728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0C10F000458
       cmp       rdi,[rcx]
       jne       short M00_L71
       mov       eax,2000
       jmp       short M00_L75
M00_L71:
       movsx     rax,word ptr [rsp+40]
       jmp       short M00_L75
M00_L72:
       mov       rax,[rsp+40]
       test      rax,rax
       je        short M00_L73
       dec       rax
       cmp       rax,1
       jbe       short M00_L74
       mov       eax,2
       jmp       short M00_L75
M00_L73:
       xor       eax,eax
       jmp       short M00_L75
M00_L74:
       mov       eax,1
M00_L75:
       cmp       eax,4
       ja        short M00_L76
       mov       ecx,eax
       lea       rax,[7FF977892058]
       mov       eax,[rax+rcx*4]
       lea       rdx,[M00_L00]
       add       rax,rdx
       jmp       rax
M00_L76:
       cmp       eax,1000
       je        near ptr M00_L96
       cmp       eax,2000
       jne       near ptr M00_L97
       mov       rcx,offset MT_AuroraScript.Runtime.Types.UInt64Value
       call      CORINFO_HELP_NEWSFAST
       mov       rdi,rax
       mov       rcx,rdi
       mov       rdx,[rsp+40]
       call      qword ptr [7FF977C1C5B8]
       jmp       near ptr M00_L98
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       call      qword ptr [7FF977815728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0C10F000410
       mov       rdi,[rcx]
       jmp       near ptr M00_L98
       cmp       qword ptr [rsp+40],2
       je        short M00_L77
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BooleanValue
       call      qword ptr [7FF977815740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0C10F001500
       mov       rdi,[rcx]
       jmp       short M00_L78
M00_L77:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BooleanValue
       call      qword ptr [7FF977815740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0C10F0014F8
       mov       rdi,[rcx]
M00_L78:
       jmp       near ptr M00_L98
       mov       rcx,[rsp+40]
       mov       rax,8007FFFFFFFFFFFF
       add       rax,rcx
       cmp       rax,3
       jbe       short M00_L79
       vmovq     xmm1,rcx
       jmp       short M00_L81
M00_L79:
       cmp       rax,2
       jbe       short M00_L80
       vmovsd    xmm1,qword ptr [7FF977892050]
       jmp       short M00_L81
M00_L80:
       vmovq     xmm1,rax
M00_L81:
       vucomisd  xmm1,xmm1
       jp        short M00_L82
       je        short M00_L83
M00_L82:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977815740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0C10F0014E8
       mov       rdi,[rcx]
       jmp       near ptr M00_L95
M00_L83:
       vxorps    xmm0,xmm0,xmm0
       vucomisd  xmm1,xmm0
       jp        short M00_L84
       jne       short M00_L84
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977815740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0C10F001498
       mov       rdi,[rcx]
       jmp       near ptr M00_L95
M00_L84:
       vucomisd  xmm1,qword ptr [7FF977892070]
       jp        short M00_L85
       jne       short M00_L85
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977815740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0C10F0014A0
       mov       rdi,[rcx]
       jmp       near ptr M00_L95
M00_L85:
       vucomisd  xmm1,qword ptr [7FF977892078]
       jp        short M00_L86
       jne       short M00_L86
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977815740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0C10F0014A8
       mov       rdi,[rcx]
       jmp       near ptr M00_L95
M00_L86:
       vucomisd  xmm1,qword ptr [7FF977892080]
       jp        short M00_L87
       jne       short M00_L87
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977815740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0C10F0014B0
       mov       rdi,[rcx]
       jmp       near ptr M00_L95
M00_L87:
       vucomisd  xmm1,qword ptr [7FF977892088]
       jp        short M00_L88
       jne       short M00_L88
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977815740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0C10F0014B8
       mov       rdi,[rcx]
       jmp       near ptr M00_L95
M00_L88:
       vucomisd  xmm1,qword ptr [7FF977892090]
       jp        short M00_L89
       jne       short M00_L89
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977815740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0C10F0014C0
       mov       rdi,[rcx]
       jmp       near ptr M00_L95
M00_L89:
       vucomisd  xmm1,qword ptr [7FF977892098]
       jp        short M00_L90
       jne       short M00_L90
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977815740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0C10F0014C8
       mov       rdi,[rcx]
       jmp       near ptr M00_L95
M00_L90:
       vucomisd  xmm1,qword ptr [7FF9778920A0]
       jp        short M00_L91
       jne       short M00_L91
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977815740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0C10F0014D0
       mov       rdi,[rcx]
       jmp       near ptr M00_L95
M00_L91:
       vucomisd  xmm1,qword ptr [7FF9778920A8]
       jp        short M00_L92
       jne       short M00_L92
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977815740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0C10F0014D8
       mov       rdi,[rcx]
       jmp       near ptr M00_L95
M00_L92:
       vucomisd  xmm1,qword ptr [7FF9778920B0]
       jp        short M00_L93
       jne       short M00_L93
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977815740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0C10F0014E0
       mov       rdi,[rcx]
       jmp       short M00_L95
M00_L93:
       vmovsd    qword ptr [rsp+30],xmm1
       vucomisd  xmm1,qword ptr [7FF9778920B8]
       jp        short M00_L94
       jne       short M00_L94
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977815740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0C10F001490
       mov       rdi,[rcx]
       jmp       short M00_L95
M00_L94:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      CORINFO_HELP_NEWSFAST
       mov       rdi,rax
       mov       rcx,rdi
       vmovsd    xmm1,qword ptr [rsp+30]
       call      qword ptr [7FF977C1C5D0]
M00_L95:
       jmp       short M00_L98
M00_L96:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.Int64Value
       call      CORINFO_HELP_NEWSFAST
       mov       rdi,rax
       mov       rcx,rdi
       mov       rdx,[rsp+40]
       call      qword ptr [7FF977C1C5E8]
       jmp       short M00_L98
       lea       rcx,[rsp+38]
       call      qword ptr [7FF977C1C600]
       mov       rdi,rax
       jmp       short M00_L98
M00_L97:
       lea       rcx,[rsp+38]
       call      qword ptr [7FF977C1C618]
       mov       rdi,rax
M00_L98:
       mov       rdx,rdi
       mov       rcx,offset MT_AuroraScript.Runtime.Types.IAuroraNativeIndexer
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfInterface(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       je        near ptr M00_L104
       cmp       qword ptr [rsp+70],0
       jne       short M00_L102
       cmp       qword ptr [rsp+78],2
       jbe       short M00_L102
       mov       rcx,[rsp+78]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L99
       vmovq     xmm0,rcx
       jmp       short M00_L101
M00_L99:
       cmp       rdx,2
       jbe       short M00_L100
       vmovsd    xmm0,qword ptr [7FF977892050]
       jmp       short M00_L101
M00_L100:
       vmovq     xmm0,rdx
M00_L101:
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       rcx,7FFFFFFFFFFFFFFF
       vcvttsd2si rdx,xmm1
       vucomisd  xmm0,qword ptr [7FF977892048]
       cmovb     rcx,rdx
       mov       [rsp+68],rcx
       jmp       short M00_L103
M00_L102:
       lea       rcx,[rsp+70]
       lea       rdx,[rsp+68]
       call      qword ptr [7FF977C15DB8]
       test      eax,eax
       je        short M00_L104
M00_L103:
       lea       rdx,[rsp+80]
       mov       rcx,rbp
       mov       r8d,[rsp+68]
       mov       r11,7FF9777604F8
       call      qword ptr [r11]
       jmp       short M00_L105
M00_L104:
       vmovdqu   xmm0,xmmword ptr [rsp+70]
       vmovdqu   xmmword ptr [rsp+20],xmm0
       lea       rcx,[rsp+20]
       call      qword ptr [7FF977C15DA0]
       mov       r9,rax
       lea       rdx,[rsp+80]
       mov       rcx,rdi
       xor       r8d,r8d
       mov       rax,[rdi]
       mov       rax,[rax+50]
       call      qword ptr [rax+10]
M00_L105:
       mov       r15,[rsp+88]
       jmp       near ptr M00_L05
M00_L106:
       vmovsd    xmm0,qword ptr [7FF977892050]
       jmp       near ptr M00_L06
M00_L107:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 3331
```
```assembly
; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfInterface(Void*, System.Object)
       test      rdx,rdx
       je        short M01_L01
       mov       rax,[rdx]
       movzx     r8d,word ptr [rax+0E]
       test      r8,r8
       je        short M01_L04
       mov       r10,[rax+38]
       cmp       r8,4
       jge       short M01_L02
M01_L00:
       cmp       [r10],rcx
       jne       short M01_L03
M01_L01:
       mov       rax,rdx
       ret
M01_L02:
       cmp       [r10],rcx
       je        short M01_L01
       cmp       [r10+8],rcx
       je        short M01_L01
       cmp       [r10+10],rcx
       je        short M01_L01
       cmp       [r10+18],rcx
       je        short M01_L01
       add       r10,20
       add       r8,0FFFFFFFFFFFFFFFC
       cmp       r8,4
       jge       short M01_L02
       test      r8,r8
       je        short M01_L04
       jmp       short M01_L00
M01_L03:
       add       r10,8
       dec       r8
       test      r8,r8
       jg        short M01_L00
M01_L04:
       test      dword ptr [rax],500C0000
       jne       short M01_L05
       xor       edx,edx
       jmp       short M01_L01
M01_L05:
       jmp       qword ptr [7FF977B95488]; System.Runtime.CompilerServices.CastHelpers.IsInstance_Helper(Void*, System.Object)
; Total bytes of code 111
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
; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rax,[rcx+20]
       mov       rax,[rax-10]
       mov       rdx,rax
       test      dl,1
       jne       short M04_L00
       ret
M04_L00:
       jmp       qword ptr [7FF9779EFD68]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBaseSlow(System.Runtime.CompilerServices.MethodTable*)
; Total bytes of code 23
```
```assembly
; System.Runtime.CompilerServices.CastHelpers.IsInstance_Helper(Void*, System.Object)
       push      rsi
       push      rbx
       sub       rsp,28
       mov       rbx,rcx
       mov       rsi,rdx
       call      qword ptr [7FF96E839C18]
       mov       rax,[rax]
       mov       rdx,[rsi]
       add       rax,10
       mov       r8,rdx
       rol       r8,20
       xor       r8,rbx
       mov       r10,9E3779B97F4A7C15
       imul      r8,r10
       mov       ecx,[rax]
       shr       r8,cl
       xor       ecx,ecx
M05_L00:
       lea       r10d,[r8+1]
       movsxd    r10,r10d
       lea       r10,[r10+r10*2]
       lea       r10,[rax+r10*8]
       mov       r9d,[r10]
       mov       r11,[r10+8]
       and       r9d,0FFFFFFFE
       cmp       r11,rdx
       jne       short M05_L02
       mov       r11,rbx
       xor       r11,[r10+10]
       cmp       r11,1
       ja        short M05_L02
       cmp       r9d,[r10]
       jne       short M05_L04
M05_L01:
       cmp       r11d,1
       jne       short M05_L03
       mov       rax,rsi
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
M05_L02:
       test      r9d,r9d
       je        short M05_L04
       inc       ecx
       add       r8d,ecx
       and       r8d,[rax+4]
       cmp       ecx,8
       jl        short M05_L00
       jmp       short M05_L04
M05_L03:
       test      r11d,r11d
       jne       short M05_L05
       xor       eax,eax
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
M05_L04:
       mov       r11d,2
       jmp       short M05_L01
M05_L05:
       mov       rcx,rbx
       mov       rdx,rsi
       call      qword ptr [7FF96E854CA8]; Precode of System.Runtime.CompilerServices.CastHelpers.IsInstanceOfAny_NoCacheLookup(Void*, System.Object)
       nop
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
; Total bytes of code 187
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
; AuroraBenchmark.BoundaryOpsBenchmarks.WriteDynamicArray()
       push      r15
       push      r14
       push      r13
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,170
       vmovaps   [rsp+160],xmm6
       xor       eax,eax
       mov       [rsp+28],rax
       vxorps    xmm4,xmm4,xmm4
       vmovdqa   xmmword ptr [rsp+30],xmm4
       mov       rax,0FFFFFFFFFFFFFEE0
M00_L00:
       vmovdqa   xmmword ptr [rsp+rax+160],xmm4
       vmovdqa   xmmword ptr [rsp+rax+170],xmm4
       vmovdqa   xmmword ptr [rsp+rax+180],xmm4
       add       rax,30
       jne       short M00_L00
       mov       rbx,rcx
M00_L01:
       xor       esi,esi
       jmp       near ptr M00_L08
M00_L02:
       test      r15,r15
       je        near ptr M00_L43
M00_L03:
       mov       r14,r15
       test      r14,r14
       je        short M00_L04
       mov       rdx,offset MT_AuroraScript.Runtime.Types.ScriptArray
       cmp       [r14],rdx
       jne       near ptr M00_L47
M00_L04:
       test      r14,r14
       je        near ptr M00_L13
       cmp       qword ptr [rsp+150],0
       jne       near ptr M00_L49
       cmp       qword ptr [rsp+158],2
       jbe       near ptr M00_L49
       mov       rcx,[rsp+158]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       near ptr M00_L12
       vmovq     xmm0,rcx
M00_L05:
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       rcx,7FFFFFFFFFFFFFFF
       vcvttsd2si rdx,xmm1
       vucomisd  xmm0,qword ptr [7FF9778A49D8]
       cmovb     rcx,rdx
       mov       [rsp+148],rcx
M00_L06:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptArray
       cmp       [r14],rcx
       jne       near ptr M00_L51
       mov       rcx,r14
       mov       edx,[rsp+148]
       mov       [rsp+128],rdi
       mov       [rsp+130],rbp
       cmp       edx,[rcx+34]
       jae       near ptr M00_L50
       mov       rcx,[rcx+38]
       cmp       edx,[rcx+8]
       jae       near ptr M00_L227
       shl       rdx,4
       lea       rdi,[rcx+rdx+10]
       mov       rdx,[rsp+128]
       mov       rcx,rdi
       call      CORINFO_HELP_ASSIGN_REF
       mov       r8,[rsp+130]
       mov       [rdi+8],r8
M00_L07:
       inc       esi
       cmp       esi,200
       jge       near ptr M00_L10
M00_L08:
       mov       rcx,[rbx+48]
       mov       rax,[rbx+50]
       mov       rdx,[rbx+8]
       mov       r8,rdx
       mov       r10d,esi
       and       r10d,1F
       mov       r9d,[r8+8]
       cmp       r10d,r9d
       jae       near ptr M00_L227
       shl       r10,4
       vmovdqu   xmm0,xmmword ptr [r8+r10+10]
       vmovdqu   xmmword ptr [rsp+150],xmm0
       lea       r8d,[rsi+1]
       and       r8d,1F
       cmp       r8d,r9d
       jae       near ptr M00_L227
       shl       r8,4
       lea       rdx,[rdx+r8+10]
       mov       rdi,[rdx]
       mov       rbp,[rdx+8]
       xor       edx,edx
       mov       [rsp+148],rdx
       mov       [rsp+138],rcx
       mov       [rsp+140],rax
       cmp       qword ptr [rsp+138],0
       je        near ptr M00_L17
       mov       rcx,[rsp+138]
       mov       rax,0C53AC00448
       cmp       rcx,[rax]
       je        near ptr M00_L15
       mov       rcx,[rsp+138]
       mov       rax,0C53AC00450
       cmp       rcx,[rax]
       je        near ptr M00_L16
       movsx     r14,word ptr [rsp+140]
M00_L09:
       cmp       r14d,4
       jbe       short M00_L11
       cmp       r14d,1000
       je        near ptr M00_L41
       cmp       r14d,2000
       je        near ptr M00_L42
       mov       rdx,[rsp+138]
       mov       r15,rdx
       test      r15,r15
       je        near ptr M00_L02
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptArray
       cmp       [r15],rcx
       je        near ptr M00_L02
       jmp       near ptr M00_L21
M00_L10:
       vmovaps   xmm6,[rsp+160]
       add       rsp,170
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r13
       pop       r14
       pop       r15
       ret
M00_L11:
       mov       ecx,r14d
       lea       rax,[7FF9778A49E0]
       mov       eax,[rax+rcx*4]
       lea       rdx,[M00_L01]
       add       rax,rdx
       jmp       rax
M00_L12:
       cmp       rdx,2
       ja        near ptr M00_L48
       vmovq     xmm0,rdx
       jmp       near ptr M00_L05
M00_L13:
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptPackedArray
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r14,rax
       test      r14,r14
       jne       near ptr M00_L52
M00_L14:
       vmovdqu   xmm0,xmmword ptr [rsp+150]
       vmovdqu   xmmword ptr [rsp+28],xmm0
       lea       rcx,[rsp+28]
       call      qword ptr [7FF977C25D40]
       mov       r8,rax
       mov       [rsp+28],rdi
       mov       [rsp+30],rbp
       lea       r9,[rsp+28]
       mov       rcx,r15
       xor       edx,edx
       mov       rax,[r15]
       mov       rax,[rax+50]
       call      qword ptr [rax+20]
       jmp       near ptr M00_L07
M00_L15:
       mov       r14d,1000
       jmp       near ptr M00_L09
M00_L16:
       mov       r14d,2000
       jmp       near ptr M00_L09
M00_L17:
       mov       rcx,[rsp+140]
       test      rcx,rcx
       je        short M00_L18
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L19
       mov       r14d,2
       jmp       short M00_L20
M00_L18:
       xor       r14d,r14d
       jmp       short M00_L20
M00_L19:
       mov       r14d,1
M00_L20:
       jmp       near ptr M00_L09
M00_L21:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r15,rax
       jmp       near ptr M00_L02
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rdx,0C53AC00408
       mov       r15,[rdx]
       jmp       near ptr M00_L03
       cmp       qword ptr [rsp+140],2
       je        short M00_L22
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BooleanValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0C53AC01510
       mov       r15,[rcx]
       jmp       short M00_L23
M00_L22:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BooleanValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0C53AC01508
       mov       r15,[rcx]
M00_L23:
       jmp       near ptr M00_L03
       mov       rcx,[rsp+140]
       mov       rax,8007FFFFFFFFFFFF
       add       rax,rcx
       cmp       rax,3
       jbe       short M00_L24
       vmovq     xmm6,rcx
       jmp       short M00_L26
M00_L24:
       cmp       rax,2
       jbe       short M00_L25
       vmovsd    xmm6,qword ptr [7FF9778A49F8]
       jmp       short M00_L26
M00_L25:
       vmovq     xmm6,rax
M00_L26:
       vucomisd  xmm6,xmm6
       jp        short M00_L27
       je        short M00_L28
M00_L27:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0C53AC014F8
       mov       r15,[rcx]
       jmp       near ptr M00_L40
M00_L28:
       vxorps    xmm0,xmm0,xmm0
       vucomisd  xmm6,xmm0
       jp        short M00_L29
       jne       short M00_L29
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0C53AC014A8
       mov       r15,[rcx]
       jmp       near ptr M00_L40
M00_L29:
       vucomisd  xmm6,qword ptr [7FF9778A4A00]
       jp        short M00_L30
       jne       short M00_L30
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0C53AC014B0
       mov       r15,[rcx]
       jmp       near ptr M00_L40
M00_L30:
       vucomisd  xmm6,qword ptr [7FF9778A4A08]
       jp        short M00_L31
       jne       short M00_L31
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0C53AC014B8
       mov       r15,[rcx]
       jmp       near ptr M00_L40
M00_L31:
       vucomisd  xmm6,qword ptr [7FF9778A4A10]
       jp        short M00_L32
       jne       short M00_L32
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0C53AC014C0
       mov       r15,[rcx]
       jmp       near ptr M00_L40
M00_L32:
       vucomisd  xmm6,qword ptr [7FF9778A4A18]
       jp        short M00_L33
       jne       short M00_L33
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0C53AC014C8
       mov       r15,[rcx]
       jmp       near ptr M00_L40
M00_L33:
       vucomisd  xmm6,qword ptr [7FF9778A4A20]
       jp        short M00_L34
       jne       short M00_L34
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0C53AC014D0
       mov       r15,[rcx]
       jmp       near ptr M00_L40
M00_L34:
       vucomisd  xmm6,qword ptr [7FF9778A4A28]
       jp        short M00_L35
       jne       short M00_L35
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0C53AC014D8
       mov       r15,[rcx]
       jmp       near ptr M00_L40
M00_L35:
       vucomisd  xmm6,qword ptr [7FF9778A4A30]
       jp        short M00_L36
       jne       short M00_L36
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0C53AC014E0
       mov       r15,[rcx]
       jmp       near ptr M00_L40
M00_L36:
       vucomisd  xmm6,qword ptr [7FF9778A4A38]
       jp        short M00_L37
       jne       short M00_L37
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0C53AC014E8
       mov       r15,[rcx]
       jmp       near ptr M00_L40
M00_L37:
       vucomisd  xmm6,qword ptr [7FF9778A4A40]
       jp        short M00_L38
       jne       short M00_L38
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0C53AC014F0
       mov       r15,[rcx]
       jmp       short M00_L40
M00_L38:
       vucomisd  xmm6,qword ptr [7FF9778A4A48]
       jp        short M00_L39
       jne       short M00_L39
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0C53AC014A0
       mov       r15,[rcx]
       jmp       short M00_L40
M00_L39:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      CORINFO_HELP_NEWSFAST
       mov       r15,rax
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue+NativePrototypeHolder
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rdx,0C53AC01520
       mov       rdx,[rdx]
       mov       rcx,r15
       mov       r8d,1
       call      qword ptr [7FF977C2C720]
       or        dword ptr [r15+30],1
       vmovsd    qword ptr [r15+38],xmm6
M00_L40:
       jmp       near ptr M00_L03
M00_L41:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.Int64Value
       call      CORINFO_HELP_NEWSFAST
       mov       r15,rax
       mov       r14,[rsp+140]
       mov       rcx,offset MT_AuroraScript.Runtime.Types.Int64Value+NativePrototypeHolder
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rdx,0C53AC01528
       mov       rdx,[rdx]
       mov       rcx,r15
       mov       r8d,1
       call      qword ptr [7FF977C2C720]
       or        dword ptr [r15+30],1
       mov       [r15+38],r14
       jmp       near ptr M00_L03
M00_L42:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.UInt64Value
       call      CORINFO_HELP_NEWSFAST
       mov       r15,rax
       mov       r14,[rsp+140]
       mov       rcx,offset MT_AuroraScript.Runtime.Types.UInt64Value+NativePrototypeHolder
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rdx,0C53AC01530
       mov       rdx,[rdx]
       mov       rcx,r15
       mov       r8d,1
       call      qword ptr [7FF977C2C720]
       or        dword ptr [r15+30],1
       mov       [r15+38],r14
       jmp       near ptr M00_L03
       lea       rcx,[rsp+138]
       call      qword ptr [7FF977C25E48]
       mov       r15,rax
       jmp       near ptr M00_L03
M00_L43:
       mov       rdx,[rsp+138]
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r14,rax
       test      r14,r14
       jne       short M00_L44
       xor       r15d,r15d
       jmp       near ptr M00_L03
M00_L44:
       cmp       dword ptr [r14+8],0
       jne       short M00_L45
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0C53AC01490
       mov       r15,[rcx]
       jmp       short M00_L46
M00_L45:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      CORINFO_HELP_NEWSFAST
       mov       r15,rax
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue+NativePrototypeHolder
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rdx,0C53AC01538
       mov       rdx,[rdx]
       mov       rcx,r15
       mov       r8d,1
       call      qword ptr [7FF977C2C720]
       or        dword ptr [r15+30],1
       lea       rcx,[r15+38]
       mov       rdx,r14
       call      CORINFO_HELP_ASSIGN_REF
M00_L46:
       jmp       near ptr M00_L03
M00_L47:
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.IAuroraNativeIndexer
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfInterface(Void*, System.Object)
       mov       r14,rax
       jmp       near ptr M00_L04
M00_L48:
       vmovsd    xmm0,qword ptr [7FF9778A49F8]
       jmp       near ptr M00_L05
M00_L49:
       lea       rcx,[rsp+150]
       lea       rdx,[rsp+148]
       call      qword ptr [7FF977C25EC0]
       test      eax,eax
       je        near ptr M00_L13
       jmp       near ptr M00_L06
M00_L50:
       lea       r8,[rsp+128]
       call      qword ptr [7FF977BAC108]
       jmp       near ptr M00_L07
M00_L51:
       mov       edx,[rsp+148]
       mov       [rsp+28],rdi
       mov       [rsp+30],rbp
       lea       r8,[rsp+28]
       mov       rcx,r14
       mov       r11,7FF9777704E0
       call      qword ptr [r11]
       jmp       near ptr M00_L07
M00_L52:
       cmp       qword ptr [rsp+150],0
       jne       short M00_L56
       cmp       qword ptr [rsp+158],2
       jbe       short M00_L56
       mov       rcx,[rsp+158]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L53
       vmovq     xmm0,rcx
       jmp       short M00_L55
M00_L53:
       cmp       rdx,2
       jbe       short M00_L54
       vmovsd    xmm0,qword ptr [7FF9778A49F8]
       jmp       short M00_L55
M00_L54:
       vmovq     xmm0,rdx
M00_L55:
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       rcx,7FFFFFFFFFFFFFFF
       vcvttsd2si rdx,xmm1
       vucomisd  xmm0,qword ptr [7FF9778A49D8]
       cmovb     rcx,rdx
       mov       [rsp+148],rcx
       jmp       short M00_L57
M00_L56:
       lea       rcx,[rsp+150]
       lea       rdx,[rsp+148]
       call      qword ptr [7FF977C25EC0]
       test      eax,eax
       je        near ptr M00_L14
M00_L57:
       mov       r15d,[rsp+148]
       mov       rdx,r14
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt32Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r13,rax
       test      r13,r13
       jne       near ptr M00_L58
       mov       rdx,r14
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt8Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r13,rax
       test      r13,r13
       jne       near ptr M00_L72
       mov       rdx,r14
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptFloat32Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r13,rax
       test      r13,r13
       jne       near ptr M00_L86
       mov       rdx,r14
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptFloat64Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r13,rax
       test      r13,r13
       jne       near ptr M00_L100
       mov       rdx,r14
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptBooleanArray
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r13,rax
       test      r13,r13
       jne       near ptr M00_L114
       mov       rdx,r14
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptUInt8Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r13,rax
       test      r13,r13
       jne       near ptr M00_L128
       mov       rdx,r14
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt16Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r13,rax
       test      r13,r13
       jne       near ptr M00_L142
       mov       rdx,r14
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptUInt16Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r13,rax
       test      r13,r13
       jne       near ptr M00_L156
       mov       rdx,r14
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptUInt32Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r13,rax
       test      r13,r13
       jne       near ptr M00_L170
       mov       rdx,r14
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt64Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r13,rax
       test      r13,r13
       jne       near ptr M00_L184
       mov       rdx,r14
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptUInt64Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r13,rax
       test      r13,r13
       jne       near ptr M00_L204
       mov       rcx,r14
       mov       rax,[r14]
       mov       rax,[rax+58]
       call      qword ptr [rax+8]
       cmp       eax,r15d
       ja        near ptr M00_L226
       jmp       near ptr M00_L225
M00_L58:
       mov       rdx,[r13+38]
       cmp       [rdx+8],r15d
       ja        short M00_L59
       mov       rdx,[r13+38]
       mov       edx,[rdx+8]
       mov       ecx,r15d
       call      qword ptr [7FF977C2C6D8]
M00_L59:
       mov       r14,[r13+38]
       xor       ecx,ecx
       mov       [rsp+120],rcx
       mov       [rsp+110],rdi
       mov       [rsp+118],rbp
       cmp       qword ptr [rsp+110],0
       je        short M00_L60
       mov       rcx,[rsp+110]
       mov       rdx,0C53AC00448
       cmp       rcx,[rdx]
       je        short M00_L64
       mov       rcx,[rsp+110]
       mov       rdx,0C53AC00450
       cmp       rcx,[rdx]
       je        short M00_L64
       movsx     rcx,word ptr [rsp+118]
       jmp       short M00_L63
M00_L60:
       mov       rcx,[rsp+118]
       test      rcx,rcx
       je        short M00_L61
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L62
       mov       ecx,2
       jmp       short M00_L63
M00_L61:
       xor       ecx,ecx
       jmp       short M00_L63
M00_L62:
       mov       ecx,1
M00_L63:
       test      ecx,ecx
       jne       short M00_L64
       xor       ecx,ecx
       mov       [rsp+120],rcx
       jmp       near ptr M00_L70
M00_L64:
       cmp       qword ptr [rsp+110],0
       jne       short M00_L68
       cmp       qword ptr [rsp+118],2
       jbe       short M00_L68
       mov       rcx,[rsp+118]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L65
       vmovq     xmm0,rcx
       jmp       short M00_L67
M00_L65:
       cmp       rdx,2
       jbe       short M00_L66
       vmovsd    xmm0,qword ptr [7FF9778A49F8]
       jmp       short M00_L67
M00_L66:
       vmovq     xmm0,rdx
M00_L67:
       vmovsd    qword ptr [rsp+120],xmm0
       mov       edi,1
       jmp       short M00_L69
M00_L68:
       lea       rcx,[rsp+110]
       lea       rdx,[rsp+120]
       call      qword ptr [7FF977C2C6F0]
       mov       edi,eax
M00_L69:
       test      edi,edi
       jne       short M00_L70
       vmovsd    xmm0,qword ptr [7FF9778A49F8]
       jmp       short M00_L71
M00_L70:
       vmovsd    xmm0,qword ptr [rsp+120]
M00_L71:
       cmp       r15d,[r14+8]
       jae       near ptr M00_L227
       mov       ecx,r15d
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       eax,7FFFFFFF
       vcvttsd2si edx,xmm1
       vucomisd  xmm0,qword ptr [7FF9778A4A50]
       cmovb     eax,edx
       mov       [r14+rcx*4+10],eax
       jmp       near ptr M00_L07
M00_L72:
       mov       rdx,[r13+38]
       cmp       [rdx+8],r15d
       ja        short M00_L73
       mov       rdx,[r13+38]
       mov       edx,[rdx+8]
       mov       ecx,r15d
       call      qword ptr [7FF977C2C6D8]
M00_L73:
       mov       r14,[r13+38]
       xor       ecx,ecx
       mov       [rsp+108],rcx
       mov       [rsp+0F8],rdi
       mov       [rsp+100],rbp
       cmp       qword ptr [rsp+0F8],0
       je        short M00_L74
       mov       rcx,[rsp+0F8]
       mov       rdx,0C53AC00448
       cmp       rcx,[rdx]
       je        short M00_L78
       mov       rcx,[rsp+0F8]
       mov       rdx,0C53AC00450
       cmp       rcx,[rdx]
       je        short M00_L78
       movsx     rcx,word ptr [rsp+100]
       jmp       short M00_L77
M00_L74:
       mov       rcx,[rsp+100]
       test      rcx,rcx
       je        short M00_L75
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L76
       mov       ecx,2
       jmp       short M00_L77
M00_L75:
       xor       ecx,ecx
       jmp       short M00_L77
M00_L76:
       mov       ecx,1
M00_L77:
       test      ecx,ecx
       jne       short M00_L78
       xor       ecx,ecx
       mov       [rsp+108],rcx
       jmp       near ptr M00_L84
M00_L78:
       cmp       qword ptr [rsp+0F8],0
       jne       short M00_L82
       cmp       qword ptr [rsp+100],2
       jbe       short M00_L82
       mov       rcx,[rsp+100]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L79
       vmovq     xmm0,rcx
       jmp       short M00_L81
M00_L79:
       cmp       rdx,2
       jbe       short M00_L80
       vmovsd    xmm0,qword ptr [7FF9778A49F8]
       jmp       short M00_L81
M00_L80:
       vmovq     xmm0,rdx
M00_L81:
       vmovsd    qword ptr [rsp+108],xmm0
       mov       edi,1
       jmp       short M00_L83
M00_L82:
       lea       rcx,[rsp+0F8]
       lea       rdx,[rsp+108]
       call      qword ptr [7FF977C2C6F0]
       mov       edi,eax
M00_L83:
       test      edi,edi
       jne       short M00_L84
       vmovsd    xmm0,qword ptr [7FF9778A49F8]
       jmp       short M00_L85
M00_L84:
       vmovsd    xmm0,qword ptr [rsp+108]
M00_L85:
       cmp       r15d,[r14+8]
       jae       near ptr M00_L227
       mov       edx,r15d
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       ecx,7FFFFFFF
       vcvttsd2si eax,xmm1
       vucomisd  xmm0,qword ptr [7FF9778A4A50]
       cmovb     ecx,eax
       mov       [r14+rdx+10],cl
       jmp       near ptr M00_L07
M00_L86:
       mov       rdx,[r13+38]
       cmp       [rdx+8],r15d
       ja        short M00_L87
       mov       rdx,[r13+38]
       mov       edx,[rdx+8]
       mov       ecx,r15d
       call      qword ptr [7FF977C2C6D8]
M00_L87:
       mov       r14,[r13+38]
       xor       ecx,ecx
       mov       [rsp+0F0],rcx
       mov       [rsp+0E0],rdi
       mov       [rsp+0E8],rbp
       cmp       qword ptr [rsp+0E0],0
       je        short M00_L88
       mov       rcx,[rsp+0E0]
       mov       rdx,0C53AC00448
       cmp       rcx,[rdx]
       je        short M00_L92
       mov       rcx,[rsp+0E0]
       mov       rdx,0C53AC00450
       cmp       rcx,[rdx]
       je        short M00_L92
       movsx     rcx,word ptr [rsp+0E8]
       jmp       short M00_L91
M00_L88:
       mov       rcx,[rsp+0E8]
       test      rcx,rcx
       je        short M00_L89
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L90
       mov       ecx,2
       jmp       short M00_L91
M00_L89:
       xor       ecx,ecx
       jmp       short M00_L91
M00_L90:
       mov       ecx,1
M00_L91:
       test      ecx,ecx
       jne       short M00_L92
       xor       ecx,ecx
       mov       [rsp+0F0],rcx
       jmp       near ptr M00_L98
M00_L92:
       cmp       qword ptr [rsp+0E0],0
       jne       short M00_L96
       cmp       qword ptr [rsp+0E8],2
       jbe       short M00_L96
       mov       rcx,[rsp+0E8]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L93
       vmovq     xmm0,rcx
       jmp       short M00_L95
M00_L93:
       cmp       rdx,2
       jbe       short M00_L94
       vmovsd    xmm0,qword ptr [7FF9778A49F8]
       jmp       short M00_L95
M00_L94:
       vmovq     xmm0,rdx
M00_L95:
       vmovsd    qword ptr [rsp+0F0],xmm0
       mov       edi,1
       jmp       short M00_L97
M00_L96:
       lea       rcx,[rsp+0E0]
       lea       rdx,[rsp+0F0]
       call      qword ptr [7FF977C2C6F0]
       mov       edi,eax
M00_L97:
       test      edi,edi
       jne       short M00_L98
       vmovsd    xmm0,qword ptr [7FF9778A49F8]
       jmp       short M00_L99
M00_L98:
       vmovsd    xmm0,qword ptr [rsp+0F0]
M00_L99:
       cmp       r15d,[r14+8]
       jae       near ptr M00_L227
       mov       edx,r15d
       vcvtsd2ss xmm0,xmm0,xmm0
       vmovss    dword ptr [r14+rdx*4+10],xmm0
       jmp       near ptr M00_L07
M00_L100:
       mov       rdx,[r13+38]
       cmp       [rdx+8],r15d
       ja        short M00_L101
       mov       rdx,[r13+38]
       mov       edx,[rdx+8]
       mov       ecx,r15d
       call      qword ptr [7FF977C2C6D8]
M00_L101:
       mov       r14,[r13+38]
       xor       ecx,ecx
       mov       [rsp+0D8],rcx
       mov       [rsp+0C8],rdi
       mov       [rsp+0D0],rbp
       cmp       qword ptr [rsp+0C8],0
       je        short M00_L102
       mov       rcx,[rsp+0C8]
       mov       rdx,0C53AC00448
       cmp       rcx,[rdx]
       je        short M00_L106
       mov       rcx,[rsp+0C8]
       mov       rdx,0C53AC00450
       cmp       rcx,[rdx]
       je        short M00_L106
       movsx     rcx,word ptr [rsp+0D0]
       jmp       short M00_L105
M00_L102:
       mov       rcx,[rsp+0D0]
       test      rcx,rcx
       je        short M00_L103
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L104
       mov       ecx,2
       jmp       short M00_L105
M00_L103:
       xor       ecx,ecx
       jmp       short M00_L105
M00_L104:
       mov       ecx,1
M00_L105:
       test      ecx,ecx
       jne       short M00_L106
       xor       ecx,ecx
       mov       [rsp+0D8],rcx
       jmp       near ptr M00_L112
M00_L106:
       cmp       qword ptr [rsp+0C8],0
       jne       short M00_L110
       cmp       qword ptr [rsp+0D0],2
       jbe       short M00_L110
       mov       rcx,[rsp+0D0]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L107
       vmovq     xmm0,rcx
       jmp       short M00_L109
M00_L107:
       cmp       rdx,2
       jbe       short M00_L108
       vmovsd    xmm0,qword ptr [7FF9778A49F8]
       jmp       short M00_L109
M00_L108:
       vmovq     xmm0,rdx
M00_L109:
       vmovsd    qword ptr [rsp+0D8],xmm0
       mov       edi,1
       jmp       short M00_L111
M00_L110:
       lea       rcx,[rsp+0C8]
       lea       rdx,[rsp+0D8]
       call      qword ptr [7FF977C2C6F0]
       mov       edi,eax
M00_L111:
       test      edi,edi
       jne       short M00_L112
       vmovsd    xmm0,qword ptr [7FF9778A49F8]
       jmp       short M00_L113
M00_L112:
       vmovsd    xmm0,qword ptr [rsp+0D8]
M00_L113:
       cmp       r15d,[r14+8]
       jae       near ptr M00_L227
       mov       edx,r15d
       vmovsd    qword ptr [r14+rdx*8+10],xmm0
       jmp       near ptr M00_L07
M00_L114:
       mov       rdx,[r13+38]
       cmp       [rdx+8],r15d
       ja        short M00_L115
       mov       rdx,[r13+38]
       mov       edx,[rdx+8]
       mov       ecx,r15d
       call      qword ptr [7FF977C2C6D8]
M00_L115:
       mov       r14,[r13+38]
       mov       [rsp+0B8],rdi
       mov       [rsp+0C0],rbp
       cmp       qword ptr [rsp+0B8],0
       je        short M00_L118
       mov       rdx,[rsp+0B8]
       mov       rcx,0C53AC00448
       cmp       rdx,[rcx]
       jne       short M00_L116
       mov       edi,1000
       jmp       short M00_L121
M00_L116:
       mov       rdx,[rsp+0B8]
       mov       rcx,0C53AC00450
       cmp       rdx,[rcx]
       jne       short M00_L117
       mov       edi,2000
       jmp       short M00_L121
M00_L117:
       movsx     rdi,word ptr [rsp+0C0]
       jmp       short M00_L121
M00_L118:
       mov       rdx,[rsp+0C0]
       test      rdx,rdx
       je        short M00_L119
       dec       rdx
       cmp       rdx,1
       jbe       short M00_L120
       mov       edi,2
       jmp       short M00_L121
M00_L119:
       xor       edi,edi
       jmp       short M00_L121
M00_L120:
       mov       edi,1
M00_L121:
       cmp       edi,4
       ja        short M00_L122
       mov       edx,edi
       lea       rcx,[7FF9778A4A58]
       mov       ecx,[rcx+rdx*4]
       lea       rax,[M00_L01]
       add       rcx,rax
       jmp       rcx
M00_L122:
       cmp       edi,1000
       je        near ptr M00_L124
       cmp       edi,2000
       je        near ptr M00_L124
       lea       rcx,[rsp+0B8]
       call      qword ptr [7FF977C25E60]; AuroraScript.Runtime.ScriptDatum.get_Object()
       mov       rbp,rax
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rdx,0C53AC00408
       cmp       rbp,[rdx]
       setne     bpl
       movzx     ebp,bpl
       jmp       near ptr M00_L127
       cmp       qword ptr [rsp+0C0],2
       sete      bpl
       movzx     ebp,bpl
       jmp       near ptr M00_L127
       mov       rcx,7FF8000000000001
       cmp       [rsp+0C0],rcx
       je        short M00_L123
       mov       rcx,8000000000000000
       cmp       [rsp+0C0],rcx
       je        short M00_L123
       mov       rcx,7FF8000000000004
       cmp       [rsp+0C0],rcx
       setne     bpl
       movzx     ebp,bpl
       jmp       short M00_L127
M00_L123:
       xor       ebp,ebp
       jmp       short M00_L127
M00_L124:
       cmp       qword ptr [rsp+0C0],0
       setne     bpl
       movzx     ebp,bpl
       jmp       short M00_L127
       mov       rdx,[rsp+0B8]
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rax,rax
       je        short M00_L125
       cmp       dword ptr [rax+8],0
       sete      cl
       movzx     ecx,cl
       jmp       short M00_L126
M00_L125:
       mov       ecx,1
M00_L126:
       test      ecx,ecx
       sete      bpl
       movzx     ebp,bpl
M00_L127:
       cmp       r15d,[r14+8]
       jae       near ptr M00_L227
       mov       edx,r15d
       mov       [r14+rdx+10],bpl
       jmp       near ptr M00_L07
M00_L128:
       mov       rdx,[r13+38]
       cmp       [rdx+8],r15d
       ja        short M00_L129
       mov       rdx,[r13+38]
       mov       edx,[rdx+8]
       mov       ecx,r15d
       call      qword ptr [7FF977C2C6D8]
M00_L129:
       mov       r14,[r13+38]
       xor       ecx,ecx
       mov       [rsp+0B0],rcx
       mov       [rsp+0A0],rdi
       mov       [rsp+0A8],rbp
       cmp       qword ptr [rsp+0A0],0
       je        short M00_L130
       mov       rcx,[rsp+0A0]
       mov       rdx,0C53AC00448
       cmp       rcx,[rdx]
       je        short M00_L134
       mov       rcx,[rsp+0A0]
       mov       rdx,0C53AC00450
       cmp       rcx,[rdx]
       je        short M00_L134
       movsx     rcx,word ptr [rsp+0A8]
       jmp       short M00_L133
M00_L130:
       mov       rcx,[rsp+0A8]
       test      rcx,rcx
       je        short M00_L131
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L132
       mov       ecx,2
       jmp       short M00_L133
M00_L131:
       xor       ecx,ecx
       jmp       short M00_L133
M00_L132:
       mov       ecx,1
M00_L133:
       test      ecx,ecx
       jne       short M00_L134
       xor       ecx,ecx
       mov       [rsp+0B0],rcx
       jmp       near ptr M00_L140
M00_L134:
       cmp       qword ptr [rsp+0A0],0
       jne       short M00_L138
       cmp       qword ptr [rsp+0A8],2
       jbe       short M00_L138
       mov       rcx,[rsp+0A8]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L135
       vmovq     xmm0,rcx
       jmp       short M00_L137
M00_L135:
       cmp       rdx,2
       jbe       short M00_L136
       vmovsd    xmm0,qword ptr [7FF9778A49F8]
       jmp       short M00_L137
M00_L136:
       vmovq     xmm0,rdx
M00_L137:
       vmovsd    qword ptr [rsp+0B0],xmm0
       mov       edi,1
       jmp       short M00_L139
M00_L138:
       lea       rcx,[rsp+0A0]
       lea       rdx,[rsp+0B0]
       call      qword ptr [7FF977C2C6F0]
       mov       edi,eax
M00_L139:
       test      edi,edi
       jne       short M00_L140
       vmovsd    xmm0,qword ptr [7FF9778A49F8]
       jmp       short M00_L141
M00_L140:
       vmovsd    xmm0,qword ptr [rsp+0B0]
M00_L141:
       cmp       r15d,[r14+8]
       jae       near ptr M00_L227
       mov       edx,r15d
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       ecx,7FFFFFFF
       vcvttsd2si eax,xmm1
       vucomisd  xmm0,qword ptr [7FF9778A4A50]
       cmovb     ecx,eax
       mov       [r14+rdx+10],cl
       jmp       near ptr M00_L07
M00_L142:
       mov       rdx,[r13+38]
       cmp       [rdx+8],r15d
       ja        short M00_L143
       mov       rdx,[r13+38]
       mov       edx,[rdx+8]
       mov       ecx,r15d
       call      qword ptr [7FF977C2C6D8]
M00_L143:
       mov       r14,[r13+38]
       xor       ecx,ecx
       mov       [rsp+98],rcx
       mov       [rsp+88],rdi
       mov       [rsp+90],rbp
       cmp       qword ptr [rsp+88],0
       je        short M00_L144
       mov       rcx,[rsp+88]
       mov       rdx,0C53AC00448
       cmp       rcx,[rdx]
       je        short M00_L148
       mov       rcx,[rsp+88]
       mov       rdx,0C53AC00450
       cmp       rcx,[rdx]
       je        short M00_L148
       movsx     rcx,word ptr [rsp+90]
       jmp       short M00_L147
M00_L144:
       mov       rcx,[rsp+90]
       test      rcx,rcx
       je        short M00_L145
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L146
       mov       ecx,2
       jmp       short M00_L147
M00_L145:
       xor       ecx,ecx
       jmp       short M00_L147
M00_L146:
       mov       ecx,1
M00_L147:
       test      ecx,ecx
       jne       short M00_L148
       xor       ecx,ecx
       mov       [rsp+98],rcx
       jmp       near ptr M00_L154
M00_L148:
       cmp       qword ptr [rsp+88],0
       jne       short M00_L152
       cmp       qword ptr [rsp+90],2
       jbe       short M00_L152
       mov       rcx,[rsp+90]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L149
       vmovq     xmm0,rcx
       jmp       short M00_L151
M00_L149:
       cmp       rdx,2
       jbe       short M00_L150
       vmovsd    xmm0,qword ptr [7FF9778A49F8]
       jmp       short M00_L151
M00_L150:
       vmovq     xmm0,rdx
M00_L151:
       vmovsd    qword ptr [rsp+98],xmm0
       mov       edi,1
       jmp       short M00_L153
M00_L152:
       lea       rcx,[rsp+88]
       lea       rdx,[rsp+98]
       call      qword ptr [7FF977C2C6F0]
       mov       edi,eax
M00_L153:
       test      edi,edi
       jne       short M00_L154
       vmovsd    xmm0,qword ptr [7FF9778A49F8]
       jmp       short M00_L155
M00_L154:
       vmovsd    xmm0,qword ptr [rsp+98]
M00_L155:
       cmp       r15d,[r14+8]
       jae       near ptr M00_L227
       mov       edx,r15d
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       ecx,7FFFFFFF
       vcvttsd2si eax,xmm1
       vucomisd  xmm0,qword ptr [7FF9778A4A50]
       cmovb     ecx,eax
       mov       [r14+rdx*2+10],cx
       jmp       near ptr M00_L07
M00_L156:
       mov       rdx,[r13+38]
       cmp       [rdx+8],r15d
       ja        short M00_L157
       mov       rdx,[r13+38]
       mov       edx,[rdx+8]
       mov       ecx,r15d
       call      qword ptr [7FF977C2C6D8]
M00_L157:
       mov       r14,[r13+38]
       xor       ecx,ecx
       mov       [rsp+80],rcx
       mov       [rsp+70],rdi
       mov       [rsp+78],rbp
       cmp       qword ptr [rsp+70],0
       je        short M00_L158
       mov       rcx,[rsp+70]
       mov       rdx,0C53AC00448
       cmp       rcx,[rdx]
       je        short M00_L162
       mov       rcx,[rsp+70]
       mov       rdx,0C53AC00450
       cmp       rcx,[rdx]
       je        short M00_L162
       movsx     rcx,word ptr [rsp+78]
       jmp       short M00_L161
M00_L158:
       mov       rcx,[rsp+78]
       test      rcx,rcx
       je        short M00_L159
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L160
       mov       ecx,2
       jmp       short M00_L161
M00_L159:
       xor       ecx,ecx
       jmp       short M00_L161
M00_L160:
       mov       ecx,1
M00_L161:
       test      ecx,ecx
       jne       short M00_L162
       xor       ecx,ecx
       mov       [rsp+80],rcx
       jmp       short M00_L168
M00_L162:
       cmp       qword ptr [rsp+70],0
       jne       short M00_L166
       cmp       qword ptr [rsp+78],2
       jbe       short M00_L166
       mov       rcx,[rsp+78]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L163
       vmovq     xmm0,rcx
       jmp       short M00_L165
M00_L163:
       cmp       rdx,2
       jbe       short M00_L164
       vmovsd    xmm0,qword ptr [7FF9778A49F8]
       jmp       short M00_L165
M00_L164:
       vmovq     xmm0,rdx
M00_L165:
       vmovsd    qword ptr [rsp+80],xmm0
       mov       edi,1
       jmp       short M00_L167
M00_L166:
       lea       rcx,[rsp+70]
       lea       rdx,[rsp+80]
       call      qword ptr [7FF977C2C6F0]
       mov       edi,eax
M00_L167:
       test      edi,edi
       jne       short M00_L168
       vmovsd    xmm0,qword ptr [7FF9778A49F8]
       jmp       short M00_L169
M00_L168:
       vmovsd    xmm0,qword ptr [rsp+80]
M00_L169:
       cmp       r15d,[r14+8]
       jae       near ptr M00_L227
       mov       edx,r15d
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       ecx,7FFFFFFF
       vcvttsd2si eax,xmm1
       vucomisd  xmm0,qword ptr [7FF9778A4A50]
       cmovb     ecx,eax
       mov       [r14+rdx*2+10],cx
       jmp       near ptr M00_L07
M00_L170:
       mov       rdx,[r13+38]
       cmp       [rdx+8],r15d
       ja        short M00_L171
       mov       rdx,[r13+38]
       mov       edx,[rdx+8]
       mov       ecx,r15d
       call      qword ptr [7FF977C2C6D8]
M00_L171:
       mov       r14,[r13+38]
       xor       ecx,ecx
       mov       [rsp+68],rcx
       mov       [rsp+58],rdi
       mov       [rsp+60],rbp
       cmp       qword ptr [rsp+58],0
       je        short M00_L172
       mov       rcx,[rsp+58]
       mov       rdx,0C53AC00448
       cmp       rcx,[rdx]
       je        short M00_L176
       mov       rcx,[rsp+58]
       mov       rdx,0C53AC00450
       cmp       rcx,[rdx]
       je        short M00_L176
       movsx     rcx,word ptr [rsp+60]
       jmp       short M00_L175
M00_L172:
       mov       rcx,[rsp+60]
       test      rcx,rcx
       je        short M00_L173
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L174
       mov       ecx,2
       jmp       short M00_L175
M00_L173:
       xor       ecx,ecx
       jmp       short M00_L175
M00_L174:
       mov       ecx,1
M00_L175:
       test      ecx,ecx
       jne       short M00_L176
       xor       ecx,ecx
       mov       [rsp+68],rcx
       jmp       short M00_L182
M00_L176:
       cmp       qword ptr [rsp+58],0
       jne       short M00_L180
       cmp       qword ptr [rsp+60],2
       jbe       short M00_L180
       mov       rcx,[rsp+60]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L177
       vmovq     xmm0,rcx
       jmp       short M00_L179
M00_L177:
       cmp       rdx,2
       jbe       short M00_L178
       vmovsd    xmm0,qword ptr [7FF9778A49F8]
       jmp       short M00_L179
M00_L178:
       vmovq     xmm0,rdx
M00_L179:
       vmovsd    qword ptr [rsp+68],xmm0
       mov       edi,1
       jmp       short M00_L181
M00_L180:
       lea       rcx,[rsp+58]
       lea       rdx,[rsp+68]
       call      qword ptr [7FF977C2C6F0]
       mov       edi,eax
M00_L181:
       test      edi,edi
       jne       short M00_L182
       vmovsd    xmm0,qword ptr [7FF9778A49F8]
       jmp       short M00_L183
M00_L182:
       vmovsd    xmm0,qword ptr [rsp+68]
M00_L183:
       cmp       r15d,[r14+8]
       jae       near ptr M00_L227
       mov       edx,r15d
       vxorps    xmm1,xmm1,xmm1
       vmaxsd    xmm1,xmm0,xmm1
       mov       ecx,0FFFFFFFF
       vcvttsd2si rax,xmm1
       vucomisd  xmm0,qword ptr [7FF9778A4A70]
       cmovb     ecx,eax
       mov       [r14+rdx*4+10],ecx
       jmp       near ptr M00_L07
M00_L184:
       mov       rdx,[r13+38]
       cmp       [rdx+8],r15d
       ja        short M00_L185
       mov       rdx,[r13+38]
       mov       edx,[rdx+8]
       mov       ecx,r15d
       call      qword ptr [7FF977C2C6D8]
M00_L185:
       mov       r14,[r13+38]
       test      rdi,rdi
       je        short M00_L188
       mov       r8,0C53AC00448
       cmp       rdi,[r8]
       jne       short M00_L186
       mov       r8d,1000
       jmp       short M00_L191
M00_L186:
       mov       r8,0C53AC00450
       cmp       rdi,[r8]
       jne       short M00_L187
       mov       r8d,2000
       jmp       short M00_L191
M00_L187:
       movsx     r8,bp
       jmp       short M00_L191
M00_L188:
       test      rbp,rbp
       je        short M00_L189
       lea       r8,[rbp-1]
       cmp       r8,1
       jbe       short M00_L190
       mov       r8d,2
       jmp       short M00_L191
M00_L189:
       xor       r8d,r8d
       jmp       short M00_L191
M00_L190:
       mov       r8d,1
M00_L191:
       cmp       r8d,2
       je        short M00_L193
       cmp       r8d,1000
       je        short M00_L192
       cmp       r8d,2000
       jne       near ptr M00_L198
       test      rbp,rbp
       jl        near ptr M00_L198
M00_L192:
       mov       rax,rbp
       jmp       near ptr M00_L203
M00_L193:
       mov       rax,8007FFFFFFFFFFFF
       add       rax,rbp
       cmp       rax,3
       jbe       short M00_L194
       vmovq     xmm0,rbp
       jmp       short M00_L196
M00_L194:
       cmp       rax,2
       jbe       short M00_L195
       vmovsd    xmm0,qword ptr [7FF9778A49F8]
       jmp       short M00_L196
M00_L195:
       vmovq     xmm0,rax
M00_L196:
       vucomisd  xmm0,qword ptr [7FF9778A4A78]
       jb        short M00_L198
       vmovsd    xmm1,qword ptr [7FF9778A49D8]
       vucomisd  xmm1,xmm0
       jbe       short M00_L198
       vroundsd  xmm1,xmm1,xmm0,0B
       vucomisd  xmm1,xmm0
       jp        short M00_L198
       jne       short M00_L198
       vxorps    xmm1,xmm1,xmm1
       vucomisd  xmm0,xmm1
       jp        short M00_L197
       jne       short M00_L197
       vmovq     r8,xmm0
       not       r8
       shr       r8,3F
       test      r8d,r8d
       je        short M00_L198
M00_L197:
       mov       r8,8007FFFFFFFFFFFF
       add       r8,rbp
       cmp       r8,3
       jbe       short M00_L200
       jmp       short M00_L199
M00_L198:
       mov       [rsp+28],rdi
       mov       [rsp+30],rbp
       lea       r8,[rsp+28]
       lea       rcx,[rsp+48]
       mov       edx,13
       call      qword ptr [7FF977C2C708]
       xor       eax,eax
       jmp       short M00_L203
M00_L199:
       vmovq     xmm0,rbp
       jmp       short M00_L202
M00_L200:
       cmp       r8,2
       jbe       short M00_L201
       vmovsd    xmm0,qword ptr [7FF9778A49F8]
       jmp       short M00_L202
M00_L201:
       vmovq     xmm0,r8
M00_L202:
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       r8,7FFFFFFFFFFFFFFF
       vcvttsd2si rcx,xmm1
       vucomisd  xmm0,qword ptr [7FF9778A49D8]
       cmovb     r8,rcx
       mov       rax,r8
M00_L203:
       cmp       r15d,[r14+8]
       jae       near ptr M00_L227
       mov       edx,r15d
       mov       [r14+rdx*8+10],rax
       jmp       near ptr M00_L07
M00_L204:
       mov       rdx,[r13+38]
       cmp       [rdx+8],r15d
       ja        short M00_L205
       mov       rdx,[r13+38]
       mov       edx,[rdx+8]
       mov       ecx,r15d
       call      qword ptr [7FF977C2C6D8]
M00_L205:
       mov       r14,[r13+38]
       test      rdi,rdi
       je        short M00_L208
       mov       r8,0C53AC00448
       cmp       rdi,[r8]
       jne       short M00_L206
       mov       r8d,1000
       jmp       short M00_L211
M00_L206:
       mov       r8,0C53AC00450
       cmp       rdi,[r8]
       jne       short M00_L207
       mov       r8d,2000
       jmp       short M00_L211
M00_L207:
       movsx     r8,bp
       jmp       short M00_L211
M00_L208:
       test      rbp,rbp
       je        short M00_L209
       lea       r8,[rbp-1]
       cmp       r8,1
       jbe       short M00_L210
       mov       r8d,2
       jmp       short M00_L211
M00_L209:
       xor       r8d,r8d
       jmp       short M00_L211
M00_L210:
       mov       r8d,1
M00_L211:
       cmp       r8d,2
       je        short M00_L214
       cmp       r8d,1000
       je        short M00_L212
       cmp       r8d,2000
       jne       near ptr M00_L219
       jmp       short M00_L213
M00_L212:
       test      rbp,rbp
       jl        near ptr M00_L219
M00_L213:
       jmp       near ptr M00_L224
M00_L214:
       mov       r8,8007FFFFFFFFFFFF
       add       r8,rbp
       cmp       r8,3
       jbe       short M00_L215
       vmovq     xmm0,rbp
       jmp       short M00_L217
M00_L215:
       cmp       r8,2
       jbe       short M00_L216
       vmovsd    xmm0,qword ptr [7FF9778A49F8]
       jmp       short M00_L217
M00_L216:
       vmovq     xmm0,r8
M00_L217:
       vxorps    xmm1,xmm1,xmm1
       vucomisd  xmm0,xmm1
       jb        short M00_L219
       vmovsd    xmm1,qword ptr [7FF9778A4A80]
       vucomisd  xmm1,xmm0
       jbe       short M00_L219
       vroundsd  xmm1,xmm1,xmm0,0B
       vucomisd  xmm1,xmm0
       jp        short M00_L219
       jne       short M00_L219
       vxorps    xmm1,xmm1,xmm1
       vucomisd  xmm0,xmm1
       jp        short M00_L218
       jne       short M00_L218
       vmovq     r8,xmm0
       not       r8
       shr       r8,3F
       test      r8d,r8d
       je        short M00_L219
M00_L218:
       mov       r8,rbp
       mov       rcx,8007FFFFFFFFFFFF
       add       rcx,r8
       cmp       rcx,3
       jbe       short M00_L221
       jmp       short M00_L220
M00_L219:
       mov       [rsp+28],rdi
       mov       [rsp+30],rbp
       lea       r8,[rsp+28]
       lea       rcx,[rsp+38]
       mov       edx,14
       call      qword ptr [7FF977C2C708]
       xor       ebp,ebp
       jmp       short M00_L224
M00_L220:
       vmovq     xmm0,r8
       jmp       short M00_L223
M00_L221:
       cmp       rcx,2
       jbe       short M00_L222
       vmovsd    xmm0,qword ptr [7FF9778A49F8]
       jmp       short M00_L223
M00_L222:
       vmovq     xmm0,rcx
M00_L223:
       vxorps    xmm1,xmm1,xmm1
       vmaxsd    xmm1,xmm0,xmm1
       vmovsd    xmm2,qword ptr [7FF9778A4A80]
       vsubsd    xmm3,xmm0,xmm2
       vcvttsd2si r8,xmm1
       vcvttsd2si rcx,xmm3
       mov       rdx,r8
       sar       rdx,3F
       and       rcx,rdx
       mov       rdx,0FFFFFFFFFFFFFFFF
       or        r8,rcx
       vucomisd  xmm0,xmm2
       cmovb     rdx,r8
       mov       rbp,rdx
M00_L224:
       cmp       r15d,[r14+8]
       jae       short M00_L227
       mov       edx,r15d
       mov       [r14+rdx*8+10],rbp
       jmp       near ptr M00_L07
M00_L225:
       mov       rcx,r14
       mov       rax,[r14]
       mov       rax,[rax+58]
       call      qword ptr [rax+8]
       mov       edx,eax
       mov       ecx,r15d
       call      qword ptr [7FF977C2C6D8]
M00_L226:
       mov       [rsp+28],rdi
       mov       [rsp+30],rbp
       lea       r8,[rsp+28]
       mov       edx,r15d
       mov       rcx,r14
       mov       rax,[r14]
       mov       rax,[rax+58]
       call      qword ptr [rax+18]
       jmp       near ptr M00_L07
M00_L227:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 6936
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
; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       push      rsi
       push      rbx
       sub       rsp,28
       mov       rbx,rcx
       mov       rcx,[rbx+20]
       mov       rsi,[rcx-10]
       mov       rcx,rsi
       test      cl,1
       jne       short M03_L00
       mov       rcx,7FF977C891C4
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rax,rsi
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
M03_L00:
       mov       rcx,7FF977C891C0
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rcx,rbx
       add       rsp,28
       pop       rbx
       pop       rsi
       jmp       qword ptr [7FF9779FFD50]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBaseSlow(System.Runtime.CompilerServices.MethodTable*)
; Total bytes of code 80
```
```assembly
; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfInterface(Void*, System.Object)
       test      rdx,rdx
       je        short M04_L01
       mov       rax,[rdx]
       movzx     r8d,word ptr [rax+0E]
       test      r8,r8
       je        short M04_L04
       mov       r10,[rax+38]
       cmp       r8,4
       jge       short M04_L02
M04_L00:
       cmp       [r10],rcx
       jne       short M04_L03
M04_L01:
       mov       rax,rdx
       ret
M04_L02:
       cmp       [r10],rcx
       je        short M04_L01
       cmp       [r10+8],rcx
       je        short M04_L01
       cmp       [r10+10],rcx
       je        short M04_L01
       cmp       [r10+18],rcx
       je        short M04_L01
       add       r10,20
       add       r8,0FFFFFFFFFFFFFFFC
       cmp       r8,4
       jge       short M04_L02
       test      r8,r8
       je        short M04_L04
       jmp       short M04_L00
M04_L03:
       add       r10,8
       dec       r8
       test      r8,r8
       jg        short M04_L00
M04_L04:
       test      dword ptr [rax],500C0000
       jne       short M04_L05
       xor       edx,edx
       jmp       short M04_L01
M04_L05:
       jmp       qword ptr [7FF977BA5470]; System.Runtime.CompilerServices.CastHelpers.IsInstance_Helper(Void*, System.Object)
; Total bytes of code 111
```
```assembly
; AuroraScript.Runtime.ScriptDatum.get_Object()
       push      rsi
       push      rbx
       sub       rsp,28
       mov       rbx,rcx
       mov       rdx,[rbx]
       mov       rax,rdx
       test      rax,rax
       je        short M05_L00
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptArray
       cmp       [rax],rcx
       jne       short M05_L01
M05_L00:
       test      rax,rax
       je        short M05_L02
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
M05_L01:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       jmp       short M05_L00
M05_L02:
       mov       rdx,[rbx]
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbx,rax
       test      rbx,rbx
       jne       short M05_L03
       xor       eax,eax
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
M05_L03:
       cmp       dword ptr [rbx+8],0
       jne       short M05_L04
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0C53AC01490
       mov       rax,[rcx]
       jmp       short M05_L05
M05_L04:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      CORINFO_HELP_NEWSFAST
       mov       rsi,rax
       mov       rcx,rsi
       mov       rdx,rbx
       call      qword ptr [7FF977C27DF8]
       mov       rax,rsi
M05_L05:
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
; Total bytes of code 175
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
```assembly
; System.Runtime.CompilerServices.CastHelpers.IsInstance_Helper(Void*, System.Object)
       push      rsi
       push      rbx
       sub       rsp,28
       mov       rbx,rcx
       mov       rsi,rdx
       call      qword ptr [7FF96E839C18]
       mov       rax,[rax]
       mov       rdx,[rsi]
       add       rax,10
       mov       r8,rdx
       rol       r8,20
       xor       r8,rbx
       mov       r10,9E3779B97F4A7C15
       imul      r8,r10
       mov       ecx,[rax]
       shr       r8,cl
       xor       ecx,ecx
M08_L00:
       lea       r10d,[r8+1]
       movsxd    r10,r10d
       lea       r10,[r10+r10*2]
       lea       r10,[rax+r10*8]
       mov       r9d,[r10]
       mov       r11,[r10+8]
       and       r9d,0FFFFFFFE
       cmp       r11,rdx
       jne       short M08_L02
       mov       r11,rbx
       xor       r11,[r10+10]
       cmp       r11,1
       ja        short M08_L02
       cmp       r9d,[r10]
       jne       short M08_L04
M08_L01:
       cmp       r11d,1
       jne       short M08_L03
       mov       rax,rsi
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
M08_L02:
       test      r9d,r9d
       je        short M08_L04
       inc       ecx
       add       r8d,ecx
       and       r8d,[rax+4]
       cmp       ecx,8
       jl        short M08_L00
       jmp       short M08_L04
M08_L03:
       test      r11d,r11d
       jne       short M08_L05
       xor       eax,eax
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
M08_L04:
       mov       r11d,2
       jmp       short M08_L01
M08_L05:
       mov       rcx,rbx
       mov       rdx,rsi
       call      qword ptr [7FF96E854CA8]; Precode of System.Runtime.CompilerServices.CastHelpers.IsInstanceOfAny_NoCacheLookup(Void*, System.Object)
       nop
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
; Total bytes of code 187
```

## .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3 (Job: Job-BZNPUE(IterationCount=5, IterationTime=200ms, LaunchCount=1, WarmupCount=3))

```assembly
; AuroraBenchmark.BoundaryOpsBenchmarks.ReadNumberProperty()
       push      r14
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,0A0
       vmovaps   [rsp+90],xmm6
       vxorps    xmm4,xmm4,xmm4
       vmovdqu   ymmword ptr [rsp+20],ymm4
       vmovdqu   ymmword ptr [rsp+40],ymm4
       vmovdqu   ymmword ptr [rsp+60],ymm4
       vmovdqa   xmmword ptr [rsp+80],xmm4
       mov       rbx,rcx
M00_L00:
       vxorps    xmm6,xmm6,xmm6
       mov       esi,200
       jmp       near ptr M00_L12
M00_L01:
       mov       rdx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       cmp       [rcx],rdx
       jne       near ptr M00_L16
       xor       ecx,ecx
M00_L02:
       test      rcx,rcx
       jne       near ptr M00_L17
       mov       r14,rdi
       test      r14,r14
       je        short M00_L03
       mov       rdx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       cmp       [r14],rdx
       jne       near ptr M00_L18
       xor       r14d,r14d
M00_L03:
       test      r14,r14
       jne       near ptr M00_L19
       xor       ecx,ecx
       mov       [rsp+50],rcx
       mov       rcx,3FF8000000000000
       mov       [rsp+58],rcx
       xor       ecx,ecx
       mov       [rsp+48],rcx
       mov       [rsp+38],rdi
       mov       [rsp+40],rbp
       cmp       qword ptr [rsp+38],0
       jne       near ptr M00_L14
       mov       rcx,[rsp+40]
       test      rcx,rcx
       je        near ptr M00_L69
       dec       rcx
       cmp       rcx,1
       jbe       near ptr M00_L70
       mov       r14d,2
M00_L04:
       cmp       r14d,4
       jbe       near ptr M00_L15
       cmp       r14d,1000
       je        near ptr M00_L93
       cmp       r14d,2000
       je        near ptr M00_L94
       mov       rdx,[rsp+38]
       mov       rdi,rdx
       test      rdi,rdi
       je        short M00_L05
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       cmp       [rdi],rcx
       jne       near ptr M00_L71
M00_L05:
       test      rdi,rdi
       je        near ptr M00_L95
M00_L06:
       mov       rbp,rdi
       test      rbp,rbp
       je        short M00_L07
       mov       rdx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       cmp       [rbp],rdx
       jne       near ptr M00_L99
       xor       ebp,ebp
M00_L07:
       test      rbp,rbp
       jne       near ptr M00_L100
M00_L08:
       vmovdqu   xmm0,xmmword ptr [rsp+50]
       vmovdqu   xmmword ptr [rsp+20],xmm0
       lea       rcx,[rsp+20]
       call      qword ptr [7FF977C45DA0]; AuroraScript.Runtime.ScriptDatum.ToString(AuroraScript.Runtime.ScriptDatum)
       mov       rdx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       cmp       [rdi],rdx
       jne       near ptr M00_L106
       lea       rdx,[rsp+60]
       mov       rcx,rdi
       mov       r9,rax
       xor       r8d,r8d
       call      qword ptr [7FF977C45FF8]; AuroraScript.Runtime.Types.ScriptObject.InternalGetPropertyDatum(AuroraScript.Runtime.ScriptContext, System.String)
M00_L09:
       mov       rdi,[rsp+68]
M00_L10:
       mov       rax,8007FFFFFFFFFFFF
       add       rax,rdi
       cmp       rax,3
       jbe       near ptr M00_L107
       vmovq     xmm0,rdi
M00_L11:
       vaddsd    xmm6,xmm6,xmm0
       dec       esi
       je        short M00_L13
M00_L12:
       mov       rdi,[rbx+68]
       mov       rbp,[rbx+70]
       mov       rcx,rdi
       test      rcx,rcx
       je        near ptr M00_L02
       jmp       near ptr M00_L01
M00_L13:
       vmovaps   xmm0,xmm6
       vmovaps   xmm6,[rsp+90]
       add       rsp,0A0
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       ret
M00_L14:
       mov       rcx,[rsp+38]
       mov       rax,8A3D400450
       cmp       rcx,[rax]
       je        near ptr M00_L67
       mov       rcx,[rsp+38]
       mov       rax,8A3D400458
       cmp       rcx,[rax]
       je        near ptr M00_L68
       movsx     r14,word ptr [rsp+40]
       jmp       near ptr M00_L04
M00_L15:
       mov       ecx,r14d
       lea       rax,[7FF9778CBBA0]
       mov       eax,[rax+rcx*4]
       lea       rdx,[M00_L00]
       add       rax,rdx
       jmp       rax
       lea       rcx,[rsp+38]
       call      qword ptr [7FF977C45E78]
       mov       rdi,rax
       jmp       near ptr M00_L06
       mov       rcx,[rsp+40]
       mov       rax,8007FFFFFFFFFFFF
       add       rax,rcx
       cmp       rax,3
       jbe       near ptr M00_L76
       jmp       near ptr M00_L75
       cmp       qword ptr [rsp+40],2
       je        near ptr M00_L73
       jmp       near ptr M00_L72
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rdx,8A3D400410
       mov       rdi,[rdx]
       jmp       near ptr M00_L06
M00_L16:
       mov       rdx,rdi
       mov       rcx,offset MT_AuroraScript.Runtime.Types.IAuroraNativeIndexer
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfInterface(Void*, System.Object)
       mov       rcx,rax
       jmp       near ptr M00_L02
M00_L17:
       lea       rdx,[rsp+80]
       mov       r11,7FF977790520
       mov       r8d,1
       call      qword ptr [r11]
       mov       rdi,[rsp+88]
       jmp       near ptr M00_L10
M00_L18:
       mov       rdx,rdi
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptPackedArray
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r14,rax
       jmp       near ptr M00_L03
M00_L19:
       mov       rdx,r14
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt32Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       near ptr M00_L20
       mov       rdx,r14
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt8Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       near ptr M00_L24
       mov       rdx,r14
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptFloat32Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       near ptr M00_L28
       mov       rdx,r14
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptFloat64Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       near ptr M00_L34
       mov       rdx,r14
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptBooleanArray
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       near ptr M00_L40
       mov       rdx,r14
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptUInt8Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       near ptr M00_L44
       mov       rdx,r14
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt16Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       near ptr M00_L48
       mov       rdx,r14
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptUInt16Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       near ptr M00_L52
       mov       rdx,r14
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptUInt32Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       near ptr M00_L56
       mov       rdx,r14
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt64Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       near ptr M00_L60
       mov       rdx,r14
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptUInt64Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       near ptr M00_L62
       mov       rcx,r14
       mov       rax,[r14]
       mov       rax,[rax+58]
       call      qword ptr [rax+8]
       cmp       eax,1
       ja        near ptr M00_L65
       jmp       near ptr M00_L64
M00_L20:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jg        short M00_L21
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,1
       call      qword ptr [7FF977C4D500]
M00_L21:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jbe       near ptr M00_L109
       mov       edx,[rdx+14]
       test      edx,edx
       je        short M00_L22
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2sd xmm0,xmm0,edx
       vmovq     rdi,xmm0
       jmp       short M00_L23
M00_L22:
       mov       rdi,7FF8000000000001
M00_L23:
       jmp       near ptr M00_L66
M00_L24:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jg        short M00_L25
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,1
       call      qword ptr [7FF977C4D500]
M00_L25:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jbe       near ptr M00_L109
       movsx     rdx,byte ptr [rdx+11]
       test      edx,edx
       je        short M00_L26
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2sd xmm0,xmm0,edx
       vmovq     rdi,xmm0
       jmp       short M00_L27
M00_L26:
       mov       rdi,7FF8000000000001
M00_L27:
       jmp       near ptr M00_L66
M00_L28:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jg        short M00_L29
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,1
       call      qword ptr [7FF977C4D500]
M00_L29:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jbe       near ptr M00_L109
       vcvtss2sd xmm0,xmm0,dword ptr [rdx+14]
       vucomisd  xmm0,xmm0
       jp        short M00_L30
       je        short M00_L31
M00_L30:
       mov       rdi,7FF8000000000004
       jmp       short M00_L33
M00_L31:
       vmovq     rdi,xmm0
       cmp       rdi,2
       jbe       short M00_L32
       jmp       short M00_L33
M00_L32:
       mov       rdx,7FF8000000000001
       add       rdi,rdx
M00_L33:
       jmp       near ptr M00_L66
M00_L34:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jg        short M00_L35
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,1
       call      qword ptr [7FF977C4D500]
M00_L35:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jbe       near ptr M00_L109
       vmovsd    xmm0,qword ptr [rdx+18]
       vucomisd  xmm0,xmm0
       jp        short M00_L36
       je        short M00_L37
M00_L36:
       mov       rdi,7FF8000000000004
       jmp       short M00_L39
M00_L37:
       vmovq     rdi,xmm0
       cmp       rdi,2
       jbe       short M00_L38
       jmp       short M00_L39
M00_L38:
       mov       rdx,7FF8000000000001
       add       rdi,rdx
M00_L39:
       jmp       near ptr M00_L66
M00_L40:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jg        short M00_L41
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,1
       call      qword ptr [7FF977C4D500]
M00_L41:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jbe       near ptr M00_L109
       movzx     edx,byte ptr [rdx+11]
       test      edx,edx
       jne       short M00_L42
       mov       edi,1
       jmp       short M00_L43
M00_L42:
       mov       edi,2
M00_L43:
       jmp       near ptr M00_L66
M00_L44:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jg        short M00_L45
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,1
       call      qword ptr [7FF977C4D500]
M00_L45:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jbe       near ptr M00_L109
       movzx     edx,byte ptr [rdx+11]
       test      edx,edx
       je        short M00_L46
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2sd xmm0,xmm0,edx
       vmovq     rdi,xmm0
       jmp       short M00_L47
M00_L46:
       mov       rdi,7FF8000000000001
M00_L47:
       jmp       near ptr M00_L66
M00_L48:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jg        short M00_L49
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,1
       call      qword ptr [7FF977C4D500]
M00_L49:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jbe       near ptr M00_L109
       movsx     rdx,word ptr [rdx+12]
       test      edx,edx
       je        short M00_L50
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2sd xmm0,xmm0,edx
       vmovq     rdi,xmm0
       jmp       short M00_L51
M00_L50:
       mov       rdi,7FF8000000000001
M00_L51:
       jmp       near ptr M00_L66
M00_L52:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jg        short M00_L53
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,1
       call      qword ptr [7FF977C4D500]
M00_L53:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jbe       near ptr M00_L109
       movzx     edx,word ptr [rdx+12]
       test      edx,edx
       je        short M00_L54
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2sd xmm0,xmm0,edx
       vmovq     rdi,xmm0
       jmp       short M00_L55
M00_L54:
       mov       rdi,7FF8000000000001
M00_L55:
       jmp       near ptr M00_L66
M00_L56:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jg        short M00_L57
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,1
       call      qword ptr [7FF977C4D500]
M00_L57:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jbe       near ptr M00_L109
       mov       edx,[rdx+14]
       test      edx,edx
       je        short M00_L58
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2sd xmm0,xmm0,rdx
       vmovq     rdi,xmm0
       jmp       short M00_L59
M00_L58:
       mov       rdi,7FF8000000000001
M00_L59:
       jmp       near ptr M00_L66
M00_L60:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jg        short M00_L61
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,1
       call      qword ptr [7FF977C4D500]
M00_L61:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jbe       near ptr M00_L109
       mov       rdi,[rdx+18]
       jmp       short M00_L66
M00_L62:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jg        short M00_L63
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,1
       call      qword ptr [7FF977C4D500]
M00_L63:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jbe       near ptr M00_L109
       mov       rdi,[rdx+18]
       jmp       short M00_L66
M00_L64:
       mov       rcx,r14
       mov       rax,[r14]
       mov       rax,[rax+58]
       call      qword ptr [rax+8]
       mov       edx,eax
       mov       ecx,1
       call      qword ptr [7FF977C4D500]
M00_L65:
       lea       rdx,[rsp+70]
       mov       rcx,r14
       mov       r8d,1
       mov       rax,[r14]
       mov       rax,[rax+58]
       call      qword ptr [rax+10]
       mov       rdi,[rsp+78]
M00_L66:
       jmp       near ptr M00_L10
M00_L67:
       mov       r14d,1000
       jmp       near ptr M00_L04
M00_L68:
       mov       r14d,2000
       jmp       near ptr M00_L04
M00_L69:
       xor       r14d,r14d
       jmp       near ptr M00_L04
M00_L70:
       mov       r14d,1
       jmp       near ptr M00_L04
M00_L71:
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       jmp       near ptr M00_L05
M00_L72:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BooleanValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8A3D401528
       mov       rdi,[rcx]
       jmp       short M00_L74
M00_L73:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BooleanValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8A3D401520
       mov       rdi,[rcx]
M00_L74:
       jmp       near ptr M00_L06
M00_L75:
       vmovq     xmm1,rcx
       jmp       short M00_L78
M00_L76:
       cmp       rax,2
       jbe       short M00_L77
       vmovsd    xmm1,qword ptr [7FF9778CBBB8]
       jmp       short M00_L78
M00_L77:
       vmovq     xmm1,rax
M00_L78:
       vucomisd  xmm1,xmm1
       jp        short M00_L79
       je        short M00_L80
M00_L79:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8A3D401510
       mov       rdi,[rcx]
       jmp       near ptr M00_L92
M00_L80:
       vxorps    xmm0,xmm0,xmm0
       vucomisd  xmm1,xmm0
       jp        short M00_L81
       jne       short M00_L81
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8A3D4014C0
       mov       rdi,[rcx]
       jmp       near ptr M00_L92
M00_L81:
       vucomisd  xmm1,qword ptr [7FF9778CBBC0]
       jp        short M00_L82
       jne       short M00_L82
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8A3D4014C8
       mov       rdi,[rcx]
       jmp       near ptr M00_L92
M00_L82:
       vucomisd  xmm1,qword ptr [7FF9778CBBC8]
       jp        short M00_L83
       jne       short M00_L83
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8A3D4014D0
       mov       rdi,[rcx]
       jmp       near ptr M00_L92
M00_L83:
       vucomisd  xmm1,qword ptr [7FF9778CBBD0]
       jp        short M00_L84
       jne       short M00_L84
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8A3D4014D8
       mov       rdi,[rcx]
       jmp       near ptr M00_L92
M00_L84:
       vucomisd  xmm1,qword ptr [7FF9778CBBD8]
       jp        short M00_L85
       jne       short M00_L85
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8A3D4014E0
       mov       rdi,[rcx]
       jmp       near ptr M00_L92
M00_L85:
       vucomisd  xmm1,qword ptr [7FF9778CBBE0]
       jp        short M00_L86
       jne       short M00_L86
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8A3D4014E8
       mov       rdi,[rcx]
       jmp       near ptr M00_L92
M00_L86:
       vucomisd  xmm1,qword ptr [7FF9778CBBE8]
       jp        short M00_L87
       jne       short M00_L87
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8A3D4014F0
       mov       rdi,[rcx]
       jmp       near ptr M00_L92
M00_L87:
       vucomisd  xmm1,qword ptr [7FF9778CBBF0]
       jp        short M00_L88
       jne       short M00_L88
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8A3D4014F8
       mov       rdi,[rcx]
       jmp       near ptr M00_L92
M00_L88:
       vucomisd  xmm1,qword ptr [7FF9778CBBF8]
       jp        short M00_L89
       jne       short M00_L89
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8A3D401500
       mov       rdi,[rcx]
       jmp       near ptr M00_L92
M00_L89:
       vucomisd  xmm1,qword ptr [7FF9778CBC00]
       jp        short M00_L90
       jne       short M00_L90
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8A3D401508
       mov       rdi,[rcx]
       jmp       short M00_L92
M00_L90:
       vmovsd    qword ptr [rsp+30],xmm1
       vucomisd  xmm1,qword ptr [7FF9778CBC08]
       jp        short M00_L91
       jne       short M00_L91
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8A3D4014B8
       mov       rdi,[rcx]
       jmp       short M00_L92
M00_L91:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      CORINFO_HELP_NEWSFAST
       mov       rdi,rax
       mov       rcx,rdi
       vmovsd    xmm1,qword ptr [rsp+30]
       call      qword ptr [7FF977C4D518]
M00_L92:
       jmp       near ptr M00_L06
M00_L93:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.Int64Value
       call      CORINFO_HELP_NEWSFAST
       mov       rdi,rax
       mov       rcx,rdi
       mov       rdx,[rsp+40]
       call      qword ptr [7FF977C45E30]
       jmp       near ptr M00_L06
M00_L94:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.UInt64Value
       call      CORINFO_HELP_NEWSFAST
       mov       rdi,rax
       mov       rcx,rdi
       mov       rdx,[rsp+40]
       call      qword ptr [7FF977C45E60]
       jmp       near ptr M00_L06
M00_L95:
       mov       rdx,[rsp+38]
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       short M00_L96
       xor       edi,edi
       jmp       near ptr M00_L06
M00_L96:
       cmp       dword ptr [rbp+8],0
       jne       short M00_L97
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8A3D401498
       mov       rdi,[rcx]
       jmp       short M00_L98
M00_L97:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      CORINFO_HELP_NEWSFAST
       mov       rdi,rax
       mov       rcx,rdi
       mov       rdx,rbp
       call      qword ptr [7FF977C4CAE0]
M00_L98:
       jmp       near ptr M00_L06
M00_L99:
       mov       rdx,rdi
       mov       rcx,offset MT_AuroraScript.Runtime.Types.IAuroraNativeIndexer
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfInterface(Void*, System.Object)
       mov       rbp,rax
       jmp       near ptr M00_L07
M00_L100:
       cmp       qword ptr [rsp+50],0
       jne       short M00_L104
       cmp       qword ptr [rsp+58],2
       jbe       short M00_L104
       mov       rcx,[rsp+58]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L101
       vmovq     xmm0,rcx
       jmp       short M00_L103
M00_L101:
       cmp       rdx,2
       jbe       short M00_L102
       vmovsd    xmm0,qword ptr [7FF9778CBBB8]
       jmp       short M00_L103
M00_L102:
       vmovq     xmm0,rdx
M00_L103:
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       rcx,7FFFFFFFFFFFFFFF
       vcvttsd2si rdx,xmm1
       vucomisd  xmm0,qword ptr [7FF9778CBC10]
       cmovb     rcx,rdx
       mov       [rsp+48],rcx
       jmp       short M00_L105
M00_L104:
       lea       rcx,[rsp+50]
       lea       rdx,[rsp+48]
       call      qword ptr [7FF977C4D530]
       test      eax,eax
       je        near ptr M00_L08
M00_L105:
       lea       rdx,[rsp+60]
       mov       rcx,rbp
       mov       r8d,[rsp+48]
       mov       r11,7FF977790528
       call      qword ptr [r11]
       jmp       near ptr M00_L09
M00_L106:
       lea       rdx,[rsp+60]
       mov       rcx,rdi
       mov       r9,rax
       xor       r8d,r8d
       mov       rax,[rdi]
       mov       rax,[rax+50]
       call      qword ptr [rax+10]
       jmp       near ptr M00_L09
M00_L107:
       cmp       rax,2
       jbe       short M00_L108
       vmovsd    xmm0,qword ptr [7FF9778CBBB8]
       jmp       near ptr M00_L11
M00_L108:
       vmovq     xmm0,rax
       jmp       near ptr M00_L11
M00_L109:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 3198
```
```assembly
; AuroraScript.Runtime.ScriptDatum.ToString(AuroraScript.Runtime.ScriptDatum)
       sub       rsp,28
M01_L00:
       mov       rdx,[rcx]
       test      rdx,rdx
       jne       near ptr M01_L05
       mov       rax,[rcx+8]
       test      rax,rax
       je        near ptr M01_L11
       dec       rax
       mov       r8d,1
       mov       r10d,2
       cmp       rax,1
       cmova     r8d,r10d
M01_L01:
       cmp       r8d,2
       jne       near ptr M01_L12
       mov       rcx,[rcx+8]
       mov       rax,8007FFFFFFFFFFFF
       add       rax,rcx
       cmp       rax,3
       jbe       near ptr M01_L14
       vmovq     xmm0,rcx
M01_L02:
       mov       rcx,gs:[58]
       mov       rcx,[rcx+48]
       cmp       dword ptr [rcx+238],2
       jle       near ptr M01_L18
       mov       rcx,[rcx+240]
       mov       rax,[rcx+10]
       test      rax,rax
       je        near ptr M01_L16
M01_L03:
       mov       rcx,[rax+10]
       test      rcx,rcx
       jne       short M01_L04
       mov       rcx,8A3D400190
       mov       rcx,[rcx]
       test      rcx,rcx
       jne       short M01_L04
       mov       rcx,8A3D400170
       mov       rcx,[rcx]
       test      rcx,rcx
       jne       short M01_L04
       vmovsd    qword ptr [rsp+20],xmm0
       call      qword ptr [7FF97784D4B8]; System.Globalization.CultureInfo.InitializeUserDefaultCulture()
       mov       rcx,rax
       vmovsd    xmm0,qword ptr [rsp+20]
M01_L04:
       cmp       byte ptr [rcx+61],0
       jne       short M01_L06
       mov       r8,[rcx+18]
       test      r8,r8
       je        near ptr M01_L09
       vmovsd    qword ptr [rsp+20],xmm0
       jmp       short M01_L08
M01_L05:
       mov       r8,8A3D400450
       cmp       rdx,[r8]
       je        short M01_L10
       mov       rax,8A3D400458
       movsx     r8,word ptr [rcx+8]
       mov       r10d,2000
       cmp       rdx,[rax]
       cmove     r8d,r10d
       jmp       near ptr M01_L01
M01_L06:
       vmovsd    qword ptr [rsp+20],xmm0
M01_L07:
       mov       rdx,0AA6FF61518
       mov       rax,[rcx]
       mov       rax,[rax+50]
       call      qword ptr [rax]
       mov       r8,rax
       test      r8,r8
       je        short M01_L08
       mov       rcx,offset MT_System.Globalization.NumberFormatInfo
       cmp       [r8],rcx
       jne       near ptr M01_L19
M01_L08:
       vmovsd    xmm0,qword ptr [rsp+20]
       xor       edx,edx
       add       rsp,28
       jmp       qword ptr [7FF977BC7120]; System.Number.FormatFloat[[System.Double, System.Private.CoreLib]](Double, System.String, System.Globalization.NumberFormatInfo)
M01_L09:
       vmovsd    qword ptr [rsp+20],xmm0
       jmp       short M01_L07
M01_L10:
       mov       r8d,1000
       jmp       near ptr M01_L01
M01_L11:
       xor       r8d,r8d
       jmp       near ptr M01_L01
M01_L12:
       cmp       r8d,4
       ja        short M01_L13
       mov       eax,r8d
       lea       r8,[7FF9778C59F8]
       mov       r8d,[r8+rax*4]
       lea       r10,[M01_L00]
       add       r8,r10
       jmp       r8
M01_L13:
       cmp       r8d,1000
       je        near ptr M01_L20
       cmp       r8d,2000
       jne       near ptr M01_L21
       mov       rcx,[rcx+8]
       add       rsp,28
       jmp       qword ptr [7FF977C45B90]; System.Number.UInt64ToDecStr(UInt64)
       mov       rax,0AA6FF69130
       jmp       near ptr M01_L22
       mov       rax,0AA6FF601E0
       mov       rdx,0AA6FF601C0
       cmp       qword ptr [rcx+8],2
       cmovne    rax,rdx
       jmp       near ptr M01_L22
M01_L14:
       cmp       rax,2
       jbe       short M01_L15
       vmovsd    xmm0,qword ptr [7FF9778C5A10]
       jmp       near ptr M01_L02
M01_L15:
       vmovq     xmm0,rax
       vmovsd    qword ptr [rsp+20],xmm0
       vmovsd    xmm0,qword ptr [rsp+20]
       jmp       near ptr M01_L02
M01_L16:
       vmovsd    qword ptr [rsp+20],xmm0
M01_L17:
       mov       ecx,2
       call      qword ptr [7FF977BCCB40]; System.Runtime.CompilerServices.StaticsHelpers.GetOptimizedGCThreadStaticBase(Int32)
       vmovsd    xmm0,qword ptr [rsp+20]
       jmp       near ptr M01_L03
M01_L18:
       vmovsd    qword ptr [rsp+20],xmm0
       jmp       short M01_L17
M01_L19:
       mov       rdx,rax
       call      qword ptr [7FF977846328]; System.Runtime.CompilerServices.CastHelpers.ChkCastClass(Void*, System.Object)
       int       3
M01_L20:
       mov       rcx,[rcx+8]
       mov       r8,8A3D400180
       mov       r8,[r8]
       xor       edx,edx
       add       rsp,28
       jmp       qword ptr [7FF977C4CB10]
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       jmp       short M01_L22
M01_L21:
       call      qword ptr [7FF977C45E90]; AuroraScript.Runtime.ScriptDatum.get_Object()
       mov       rcx,rax
       mov       rax,[rax]
       mov       rax,[rax+40]
       add       rsp,28
       jmp       qword ptr [rax+8]
M01_L22:
       add       rsp,28
       ret
; Total bytes of code 662
```
```assembly
; AuroraScript.Runtime.Types.ScriptObject.InternalGetPropertyDatum(AuroraScript.Runtime.ScriptContext, System.String)
       push      r15
       push      r14
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,78
       vxorps    xmm4,xmm4,xmm4
       vmovdqu   ymmword ptr [rsp+20],ymm4
       vmovdqu   ymmword ptr [rsp+40],ymm4
       vmovdqa   xmmword ptr [rsp+60],xmm4
       xor       eax,eax
       mov       [rsp+70],rax
       mov       rsi,rcx
       mov       rbx,rdx
       mov       rbp,r8
       mov       rdi,r9
       mov       r14,[rsi+20]
       mov       r8,[r14+8]
       mov       r15d,[r8+8]
       dec       r15d
       js        near ptr M02_L10
M02_L00:
       mov       r8,[r14+8]
       cmp       r15d,[r8+8]
       jae       near ptr M02_L43
       mov       edx,r15d
       shl       rdx,5
       mov       r8,[r8+rdx+10]
       cmp       r8,rdi
       je        short M02_L01
       test      r8,r8
       je        near ptr M02_L09
       test      rdi,rdi
       je        near ptr M02_L09
       mov       edx,[r8+8]
       cmp       edx,[rdi+8]
       jne       near ptr M02_L09
       lea       rcx,[r8+0C]
       mov       r8d,[r8+8]
       add       r8d,r8d
       lea       rdx,[rdi+0C]
       call      qword ptr [7FF97784C330]; System.SpanHelpers.SequenceEqual(Byte ByRef, Byte ByRef, UIntPtr)
       test      eax,eax
       je        near ptr M02_L09
M02_L01:
       mov       r8d,r15d
M02_L02:
       test      r8d,r8d
       jl        near ptr M02_L11
       mov       rcx,[r14+8]
       cmp       r8d,[rcx+8]
       jae       near ptr M02_L43
       mov       r8d,r8d
       shl       r8,5
       movzx     r8d,word ptr [rcx+r8+18]
       cmp       qword ptr [rsi+18],0
       jne       near ptr M02_L12
M02_L03:
       xor       ecx,ecx
M02_L04:
       mov       rdx,[rsi+10]
       cmp       r8d,[rdx+8]
       jae       near ptr M02_L43
       mov       r8d,r8d
       shl       r8,4
       vmovdqu   xmm0,xmmword ptr [rdx+r8+10]
       vmovdqu   xmmword ptr [rsp+28],xmm0
       vmovdqu   xmm0,xmmword ptr [rsp+20]
       vmovdqu   xmmword ptr [rsp+60],xmm0
       mov       r8,[rsp+30]
       mov       [rsp+70],r8
       mov       [rsp+60],rcx
M02_L05:
       mov       rdx,[rsp+60]
       test      rdx,rdx
       jne       near ptr M02_L15
M02_L06:
       mov       rbp,[rsp+68]
       mov       rdi,[rsp+70]
       mov       rdx,rbp
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r14,rax
       mov       rdx,r14
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BondingAccessor
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rax,rax
       jne       near ptr M02_L16
M02_L07:
       mov       rdx,r14
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BondingFunction
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rax,rax
       jne       near ptr M02_L17
       mov       [rbx],rbp
       mov       [rbx+8],rdi
M02_L08:
       mov       rax,rbx
       add       rsp,78
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       pop       r15
       ret
M02_L09:
       dec       r15d
       jns       near ptr M02_L00
M02_L10:
       mov       r8d,0FFFFFFFF
       jmp       near ptr M02_L02
M02_L11:
       mov       rcx,[rsi+8]
       test      rcx,rcx
       je        short M02_L14
       jmp       short M02_L13
M02_L12:
       mov       rcx,[rsi+18]
       cmp       [rcx+8],r8d
       jle       near ptr M02_L03
       mov       rcx,[rsi+18]
       cmp       r8d,[rcx+8]
       jae       near ptr M02_L43
       mov       edx,r8d
       mov       rcx,[rcx+rdx*8+10]
       jmp       near ptr M02_L04
M02_L13:
       lea       r8,[rsp+60]
       mov       rdx,rdi
       call      qword ptr [7FF977C46088]; AuroraScript.Runtime.Types.ScriptObject.TryResolveProperty(System.String, AuroraScript.Runtime.Property.PropertyDescriptor ByRef)
       test      eax,eax
       je        near ptr M02_L36
       jmp       near ptr M02_L05
M02_L14:
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rsp+60],xmm0
       vmovdqu   xmmword ptr [rsp+68],xmm0
       jmp       near ptr M02_L36
M02_L15:
       mov       rdx,[rdx+8]
       test      rdx,rdx
       je        near ptr M02_L06
       lea       rcx,[rsp+60]
       call      qword ptr [7FF977C460A0]; AuroraScript.Runtime.Property.PropertyDescriptor.get_Getter()
       cmp       [rax],al
       mov       rcx,rax
       mov       rdx,rbx
       mov       r8,rbp
       call      qword ptr [7FF977C4CC48]
       jmp       near ptr M02_L08
M02_L16:
       mov       r15,[rax+38]
       test      r15,r15
       je        near ptr M02_L07
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rsp+48],xmm0
       lea       r8,[rsp+48]
       mov       rdx,rsi
       mov       rcx,[r15+8]
       call      qword ptr [r15+18]
       mov       rcx,[rsp+48]
       mov       [rbx],rcx
       mov       rcx,[rsp+50]
       mov       [rbx+8],rcx
       jmp       near ptr M02_L08
M02_L17:
       mov       rcx,rax
       mov       rdx,rsi
       call      qword ptr [7FF977C460D0]
       mov       r15,rax
       test      r15,r15
       je        near ptr M02_L18
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NullValue
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rax,rax
       jne       near ptr M02_L18
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rax,rax
       jne       near ptr M02_L19
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.Int64Value
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rax,rax
       jne       near ptr M02_L24
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.UInt64Value
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rax,rax
       jne       near ptr M02_L25
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BooleanValue
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rax,rax
       jne       near ptr M02_L26
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rax,rax
       jne       near ptr M02_L27
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptArray
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdx,rax
       test      rdx,rdx
       jne       near ptr M02_L28
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptDate
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdx,rax
       test      rdx,rdx
       jne       near ptr M02_L29
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptRegex
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdx,rax
       test      rdx,rdx
       jne       near ptr M02_L30
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ClosureFunction
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdx,rax
       test      rdx,rdx
       jne       near ptr M02_L31
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptError
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdx,rax
       test      rdx,rdx
       jne       near ptr M02_L32
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Interop.ClrMethodBinding
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdx,rax
       test      rdx,rdx
       jne       near ptr M02_L33
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptType
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdx,rax
       test      rdx,rdx
       jne       near ptr M02_L34
       mov       esi,408
       jmp       near ptr M02_L35
M02_L18:
       xor       r15d,r15d
       xor       esi,esi
       jmp       near ptr M02_L35
M02_L19:
       vmovsd    xmm0,qword ptr [rax+38]
       vucomisd  xmm0,xmm0
       jp        short M02_L20
       je        short M02_L21
M02_L20:
       mov       rsi,7FF8000000000004
       jmp       short M02_L23
M02_L21:
       vmovq     rsi,xmm0
       cmp       rsi,2
       jbe       short M02_L22
       jmp       short M02_L23
M02_L22:
       mov       rcx,7FF8000000000001
       add       rsi,rcx
M02_L23:
       xor       r15d,r15d
       jmp       near ptr M02_L35
M02_L24:
       mov       rsi,[rax+38]
       mov       rcx,8A3D400450
       mov       r15,[rcx]
       jmp       near ptr M02_L35
M02_L25:
       mov       rsi,[rax+38]
       mov       rcx,8A3D400458
       mov       r15,[rcx]
       jmp       short M02_L35
M02_L26:
       movzx     ecx,byte ptr [rax+40]
       mov       esi,2
       mov       edx,1
       test      ecx,ecx
       cmove     rsi,rdx
       xor       r15d,r15d
       jmp       short M02_L35
M02_L27:
       mov       rcx,[rax+38]
       mov       rdx,0AA6FF60008
       test      rcx,rcx
       cmove     rcx,rdx
       mov       r15,rcx
       mov       esi,4
       jmp       short M02_L35
M02_L28:
       mov       r15,rdx
       mov       esi,18
       jmp       short M02_L35
M02_L29:
       mov       r15,rdx
       mov       esi,28
       jmp       short M02_L35
M02_L30:
       mov       r15,rdx
       mov       esi,48
       jmp       short M02_L35
M02_L31:
       mov       r15,rdx
       mov       esi,88
       jmp       short M02_L35
M02_L32:
       mov       r15,rdx
       mov       esi,808
       jmp       short M02_L35
M02_L33:
       mov       r15,rdx
       mov       esi,208
       jmp       short M02_L35
M02_L34:
       mov       r15,rdx
       mov       esi,108
M02_L35:
       mov       rcx,rbx
       mov       rdx,r15
       call      CORINFO_HELP_CHECKED_ASSIGN_REF
       mov       [rbx+8],rsi
       jmp       near ptr M02_L08
M02_L36:
       lea       rdx,[rsp+58]
       mov       rcx,rsi
       call      qword ptr [7FF977C460E8]
       test      eax,eax
       je        near ptr M02_L42
       lea       rdx,[rsp+38]
       mov       rcx,[rsp+58]
       mov       r8,rbp
       mov       r9,rdi
       cmp       [rcx],ecx
       call      qword ptr [7FF977C5B8B0]
       cmp       qword ptr [rsp+38],0
       je        short M02_L37
       mov       rax,[rsp+38]
       mov       rcx,8A3D400450
       cmp       rax,[rcx]
       je        short M02_L41
       mov       rcx,[rsp+38]
       mov       rax,8A3D400458
       cmp       rcx,[rax]
       je        short M02_L41
       movsx     rax,word ptr [rsp+40]
       jmp       short M02_L40
M02_L37:
       mov       rcx,[rsp+40]
       test      rcx,rcx
       je        short M02_L39
       dec       rcx
       mov       eax,1
       mov       edx,2
       cmp       rcx,1
       cmova     eax,edx
M02_L38:
       jmp       short M02_L40
M02_L39:
       xor       eax,eax
       jmp       short M02_L38
M02_L40:
       test      eax,eax
       jne       short M02_L41
       lea       rcx,[rsp+38]
       call      qword ptr [7FF977C45E90]; AuroraScript.Runtime.ScriptDatum.get_Object()
       test      rax,rax
       je        short M02_L42
M02_L41:
       vmovdqu   xmm0,xmmword ptr [rsp+38]
       vmovdqu   xmmword ptr [rbx],xmm0
       jmp       near ptr M02_L08
M02_L42:
       mov       rax,8A3D400428
       mov       rax,[rax]
       vmovdqu   xmm0,xmmword ptr [rax+8]
       vmovdqu   xmmword ptr [rbx],xmm0
       jmp       near ptr M02_L08
M02_L43:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 1493
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
; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfInterface(Void*, System.Object)
       test      rdx,rdx
       je        short M04_L01
       mov       rax,[rdx]
       movzx     r8d,word ptr [rax+0E]
       test      r8,r8
       jne       short M04_L02
M04_L00:
       test      dword ptr [rax],500C0000
       jne       short M04_L05
       xor       edx,edx
M04_L01:
       mov       rax,rdx
       ret
M04_L02:
       mov       r10,[rax+38]
       cmp       r8,4
       jl        short M04_L04
M04_L03:
       cmp       [r10],rcx
       je        short M04_L01
       cmp       [r10+8],rcx
       je        short M04_L01
       cmp       [r10+10],rcx
       je        short M04_L01
       cmp       [r10+18],rcx
       je        short M04_L01
       add       r10,20
       add       r8,0FFFFFFFFFFFFFFFC
       cmp       r8,4
       jge       short M04_L03
       test      r8,r8
       je        short M04_L00
M04_L04:
       cmp       [r10],rcx
       je        short M04_L01
       add       r10,8
       dec       r8
       test      r8,r8
       jg        short M04_L04
       jmp       short M04_L00
M04_L05:
       jmp       qword ptr [7FF977BC5488]; System.Runtime.CompilerServices.CastHelpers.IsInstance_Helper(Void*, System.Object)
; Total bytes of code 109
```
```assembly
; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rdx,rdx
       je        short M05_L02
       cmp       [rdx],rcx
       je        short M05_L02
       mov       rax,[rdx]
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M05_L02
M05_L00:
       test      rax,rax
       je        short M05_L01
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M05_L02
       test      rax,rax
       jne       short M05_L03
M05_L01:
       xor       edx,edx
M05_L02:
       mov       rax,rdx
       ret
M05_L03:
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M05_L02
       test      rax,rax
       je        short M05_L01
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M05_L02
       test      rax,rax
       je        short M05_L01
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M05_L02
       jmp       short M05_L00
; Total bytes of code 86
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
       mov       rcx,7FF977CC9A1C
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rax,rsi
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
M06_L00:
       mov       rcx,7FF977CC9A18
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rcx,rbx
       add       rsp,28
       pop       rbx
       pop       rsi
       jmp       qword ptr [7FF977A1FD68]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBaseSlow(System.Runtime.CompilerServices.MethodTable*)
; Total bytes of code 80
```
```assembly
; System.Globalization.CultureInfo.InitializeUserDefaultCulture()
       push      rsi
       push      rbx
       sub       rsp,28
       call      qword ptr [7FF96E8397E8]
       mov       rbx,rax
       mov       rsi,rbx
       call      qword ptr [7FF96E850A28]
       mov       rdx,rax
       test      rsi,rsi
       je        short M07_L00
       mov       rcx,rsi
       xor       r8d,r8d
       call      qword ptr [7FF96E851BC8]
       mov       rax,[rbx]
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
M07_L00:
       call      qword ptr [7FF96E84F410]
       int       3
; Total bytes of code 61
```
```assembly
; System.Number.FormatFloat[[System.Double, System.Private.CoreLib]](Double, System.String, System.Globalization.NumberFormatInfo)
       push      rbp
       push      rbx
       sub       rsp,58
       lea       rbp,[rsp+20]
       xor       eax,eax
       mov       [rbp+8],rax
       vxorps    xmm4,xmm4,xmm4
       vmovdqu   ymmword ptr [rbp+10],ymm4
       mov       [rbp+30],rax
       mov       rax,383870C868D3
       mov       [rbp],rax
       mov       r9,r8
       test      [rsp],esp
       sub       rsp,40
       lea       r8,[rsp+20]
       mov       [rbp+28],r8
       mov       dword ptr [rbp+30],20
       test      rdx,rdx
       jne       near ptr M08_L04
       xor       r8d,r8d
       xor       ecx,ecx
M08_L00:
       mov       [rbp+8],r8
       mov       [rbp+10],ecx
       lea       r8,[rbp+8]
       lea       rcx,[rbp+18]
       vmovaps   xmm1,xmm0
       call      qword ptr [7FF977BC7138]; System.Number.FormatFloat[[System.Double, System.Private.CoreLib],[System.Char, System.Private.CoreLib]](System.Collections.Generic.ValueListBuilder`1<Char> ByRef, Double, System.ReadOnlySpan`1<Char>, System.Globalization.NumberFormatInfo)
       mov       rbx,rax
       test      rbx,rbx
       jne       short M08_L01
       mov       ecx,[rbp+20]
       cmp       ecx,[rbp+30]
       ja        short M08_L05
       mov       rax,[rbp+28]
       mov       [rbp+8],rax
       mov       [rbp+10],ecx
       lea       rcx,[rbp+8]
       call      System.String.Ctor(System.ReadOnlySpan`1<Char>)
       mov       rbx,rax
M08_L01:
       mov       rdx,[rbp+18]
       test      rdx,rdx
       je        short M08_L02
       xor       ecx,ecx
       mov       [rbp+18],rcx
       mov       rcx,8A3D4002B0
       mov       rcx,[rcx]
       xor       r8d,r8d
       call      qword ptr [7FF977A39840]; Precode of System.Buffers.SharedArrayPool`1[[System.Char, System.Private.CoreLib]].Return(Char[], Boolean)
M08_L02:
       mov       rax,rbx
       mov       r8,383870C868D3
       cmp       [rbp],r8
       je        short M08_L03
       call      CORINFO_HELP_FAIL_FAST
M08_L03:
       nop
       lea       rsp,[rbp+38]
       pop       rbx
       pop       rbp
       ret
M08_L04:
       lea       r8,[rdx+0C]
       mov       ecx,[rdx+8]
       jmp       near ptr M08_L00
M08_L05:
       call      qword ptr [7FF977A17D50]
       int       3
; Total bytes of code 236
```
```assembly
; System.Number.UInt64ToDecStr(UInt64)
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,28
       xor       eax,eax
       mov       [rsp+20],rax
       mov       rbx,rcx
       cmp       rbx,12C
       jb        near ptr M09_L08
       mov       rcx,rbx
       or        rcx,1
       lzcnt     rcx,rcx
       xor       ecx,3F
       mov       rax,7FF96DBC7050
       movzx     ecx,byte ptr [rax+rcx]
       mov       rax,7FF96DBCCE38
       cmp       rbx,[rax+rcx*8]
       setb      al
       movzx     eax,al
       sub       ecx,eax
       movsxd    rsi,ecx
       mov       rcx,rsi
       call      qword ptr [7FF977846670]; System.String.FastAllocateString(IntPtr)
       mov       rdi,rax
       test      rdi,rdi
       je        short M09_L00
       mov       rcx,7FF977D15C44
       call      CORINFO_HELP_COUNTPROFILE32
       lea       rcx,[rdi+0C]
       mov       [rsp+20],rcx
       mov       rbp,[rsp+20]
       jmp       short M09_L01
M09_L00:
       xor       edi,edi
       xor       ebp,ebp
M09_L01:
       mov       rcx,7FF977D15C48
       call      CORINFO_HELP_COUNTPROFILE32
       lea       rax,[rbp+rsi*2]
       cmp       rbx,0A
       jb        short M09_L06
       cmp       rbx,64
       jb        short M09_L03
       mov       rcx,0AA6FF61B94
M09_L02:
       add       rax,0FFFFFFFFFFFFFFFC
       mov       r8,28F5C28F5C28F5C3
       mov       rdx,rbx
       shr       rdx,2
       mulx      rdx,rdx,r8
       shr       rdx,2
       imul      r8,rdx,64
       sub       rbx,r8
       mov       r8,rcx
       shl       ebx,2
       mov       r10d,ebx
       mov       r8d,[r8+r10]
       mov       [rax],r8d
       cmp       rdx,64
       jb        short M09_L04
       mov       rbx,rdx
       jmp       short M09_L02
M09_L03:
       mov       rdx,rbx
M09_L04:
       cmp       rdx,0A
       jb        short M09_L05
       add       rax,0FFFFFFFFFFFFFFFC
       mov       rcx,0AA6FF61B94
       shl       edx,2
       mov       ecx,[rcx+rdx]
       mov       [rax],ecx
       jmp       short M09_L07
M09_L05:
       mov       rbx,rdx
M09_L06:
       lea       ecx,[rbx+30]
       mov       [rax-2],cx
M09_L07:
       xor       eax,eax
       mov       [rsp+20],rax
       mov       rax,rdi
       add       rsp,28
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       ret
M09_L08:
       mov       rcx,7FF977D15C40
       call      CORINFO_HELP_COUNTPROFILE32
       mov       ecx,ebx
       call      qword ptr [7FF977BC5590]; System.Number.UInt32ToDecStrForKnownSmallNumber(UInt32)
       nop
       add       rsp,28
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       ret
; Total bytes of code 338
```
```assembly
; System.Runtime.CompilerServices.StaticsHelpers.GetOptimizedGCThreadStaticBase(Int32)
       push      rbx
       sub       rsp,20
       mov       ebx,ecx
       call      qword ptr [7FF96E851D18]; Precode of System.Threading.Thread.GetThreadStaticsBase()
       mov       ecx,ebx
       and       ecx,0FFFFFF
       mov       edx,ecx
       mov       r8d,ebx
       sar       r8d,18
       jne       short M10_L01
       cmp       [rax],ecx
       jle       short M10_L03
       mov       rax,[rax+8]
       cmp       [rax],al
       add       edx,0FFFFFFFE
       movsxd    rcx,edx
       mov       rax,[rax+rcx*8+10]
       test      rax,rax
       je        short M10_L03
M10_L00:
       add       rsp,20
       pop       rbx
       ret
M10_L01:
       mov       ecx,ebx
       sar       ecx,18
       cmp       ecx,2
       jne       short M10_L02
       movsxd    rcx,edx
       add       rax,rcx
       jmp       short M10_L00
M10_L02:
       cmp       [rax+4],edx
       jle       short M10_L03
       mov       rcx,[rax+10]
       movsxd    rax,edx
       mov       rcx,[rcx+rax*8]
       test      rcx,rcx
       je        short M10_L03
       mov       rax,[rcx]
       test      rax,rax
       je        short M10_L03
       jmp       short M10_L00
M10_L03:
       mov       ecx,ebx
       lea       rax,[System.Reflection.CustomAttributeExtensions.GetCustomAttribute[[System.__Canon, System.Private.CoreLib]](System.Reflection.Assembly)]
       add       rsp,20
       pop       rbx
       jmp       qword ptr [rax]
; Total bytes of code 130
```
```assembly
; System.Runtime.CompilerServices.CastHelpers.ChkCastClass(Void*, System.Object)
       test      rdx,rdx
       je        short M11_L00
       cmp       [rdx],rcx
       jne       short M11_L01
M11_L00:
       mov       rax,rdx
       ret
M11_L01:
       jmp       qword ptr [7FF977A14D20]; System.Runtime.CompilerServices.CastHelpers.ChkCastClassSpecial(Void*, System.Object)
; Total bytes of code 20
```
```assembly
; AuroraScript.Runtime.ScriptDatum.get_Object()
       push      rsi
       push      rbx
       sub       rsp,28
       mov       rbx,rcx
       mov       rdx,[rbx]
       mov       rax,rdx
       test      rax,rax
       je        short M12_L00
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       cmp       [rax],rcx
       jne       short M12_L01
M12_L00:
       test      rax,rax
       je        short M12_L02
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
M12_L01:
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       jmp       short M12_L00
M12_L02:
       mov       rdx,[rbx]
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbx,rax
       test      rbx,rbx
       jne       short M12_L03
       xor       eax,eax
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
M12_L03:
       cmp       dword ptr [rbx+8],0
       jne       short M12_L04
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8A3D401498
       mov       rax,[rcx]
       jmp       short M12_L05
M12_L04:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      CORINFO_HELP_NEWSFAST
       mov       rsi,rax
       mov       rcx,rsi
       mov       rdx,rbx
       call      qword ptr [7FF977C4CAE0]
       mov       rax,rsi
M12_L05:
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
; Total bytes of code 165
```
```assembly
; System.SpanHelpers.SequenceEqual(Byte ByRef, Byte ByRef, UIntPtr)
       cmp       r8,8
       jae       short M13_L04
       cmp       r8,4
       jb        short M13_L01
       add       r8,0FFFFFFFFFFFFFFFC
       mov       eax,[rcx]
       sub       eax,[rdx]
       mov       ecx,[rcx+r8]
       sub       ecx,[rdx+r8]
       or        eax,ecx
       sete      al
       movzx     eax,al
M13_L00:
       vzeroupper
       ret
M13_L01:
       xor       eax,eax
       mov       r10,r8
       and       r10,2
       je        short M13_L02
       movzx     eax,word ptr [rcx]
       movzx     r9d,word ptr [rdx]
       sub       eax,r9d
M13_L02:
       test      r8b,1
       je        short M13_L03
       movzx     ecx,byte ptr [rcx+r10]
       movzx     edx,byte ptr [rdx+r10]
       sub       ecx,edx
       or        eax,ecx
M13_L03:
       test      eax,eax
       sete      al
       movzx     eax,al
       jmp       short M13_L00
M13_L04:
       cmp       rcx,rdx
       je        near ptr M13_L10
       cmp       r8,20
       jb        short M13_L07
       xor       eax,eax
       add       r8,0FFFFFFFFFFFFFFE0
       je        short M13_L06
M13_L05:
       vmovups   ymm0,[rcx+rax]
       vpcmpeqb  ymm0,ymm0,[rdx+rax]
       vpmovmskb r10d,ymm0
       cmp       r10d,0FFFFFFFF
       jne       near ptr M13_L12
       add       rax,20
       cmp       r8,rax
       ja        short M13_L05
M13_L06:
       vmovups   ymm0,[rcx+r8]
       vpcmpeqb  ymm0,ymm0,[rdx+r8]
       vpmovmskb ecx,ymm0
       cmp       ecx,0FFFFFFFF
       jne       short M13_L12
       jmp       short M13_L10
M13_L07:
       cmp       r8,10
       jb        short M13_L11
       xor       eax,eax
       add       r8,0FFFFFFFFFFFFFFF0
       je        short M13_L09
M13_L08:
       vmovups   xmm0,[rcx+rax]
       vpcmpeqb  xmm0,xmm0,[rdx+rax]
       vpmovmskb r10d,xmm0
       cmp       r10d,0FFFF
       jne       short M13_L12
       add       rax,10
       cmp       r8,rax
       ja        short M13_L08
M13_L09:
       vmovups   xmm0,[rcx+r8]
       vpcmpeqb  xmm0,xmm0,[rdx+r8]
       vpmovmskb eax,xmm0
       cmp       eax,0FFFF
       jne       short M13_L12
M13_L10:
       mov       eax,1
       vzeroupper
       ret
M13_L11:
       lea       rax,[r8-8]
       mov       r8,[rcx]
       sub       r8,[rdx]
       mov       rcx,[rcx+rax]
       sub       rcx,[rdx+rax]
       or        rcx,r8
       sete      al
       movzx     eax,al
       jmp       near ptr M13_L00
M13_L12:
       xor       eax,eax
       vzeroupper
       ret
; Total bytes of code 286
```
```assembly
; AuroraScript.Runtime.Types.ScriptObject.TryResolveProperty(System.String, AuroraScript.Runtime.Property.PropertyDescriptor ByRef)
       push      r15
       push      r14
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,38
       vxorps    xmm4,xmm4,xmm4
       vmovdqa   xmmword ptr [rsp+20],xmm4
       xor       eax,eax
       mov       [rsp+30],rax
       mov       rsi,rcx
       mov       rdi,rdx
       mov       rbx,r8
M14_L00:
       mov       rbp,[rsi+20]
       mov       r8,[rbp+8]
       mov       r14d,[r8+8]
       dec       r14d
       js        near ptr M14_L07
       mov       r8,8A3D400068
       mov       r15,[r8]
M14_L01:
       mov       r8,[rbp+8]
       cmp       r14d,[r8+8]
       jae       near ptr M14_L11
       mov       edx,r14d
       shl       rdx,5
       mov       r8,[r8+rdx+10]
       cmp       r8,rdi
       je        short M14_L02
       test      r8,r8
       je        near ptr M14_L06
       test      rdi,rdi
       je        near ptr M14_L06
       mov       edx,[r8+8]
       cmp       edx,[rdi+8]
       jne       near ptr M14_L06
       lea       rcx,[r8+0C]
       mov       r8d,[r8+8]
       add       r8d,r8d
       lea       rdx,[rdi+0C]
       call      qword ptr [7FF97784C330]; System.SpanHelpers.SequenceEqual(Byte ByRef, Byte ByRef, UIntPtr)
       test      eax,eax
       je        short M14_L06
M14_L02:
       mov       eax,r14d
M14_L03:
       test      eax,eax
       jl        near ptr M14_L08
       mov       rcx,[rbp+8]
       cmp       eax,[rcx+8]
       jae       near ptr M14_L11
       mov       eax,eax
       shl       rax,5
       movzx     eax,word ptr [rcx+rax+18]
       cmp       qword ptr [rsi+18],0
       jne       near ptr M14_L09
M14_L04:
       xor       ecx,ecx
M14_L05:
       mov       rdx,[rsi+10]
       cmp       eax,[rdx+8]
       jae       near ptr M14_L11
       mov       eax,eax
       shl       rax,4
       vmovdqu   xmm0,xmmword ptr [rdx+rax+10]
       vmovdqu   xmmword ptr [rsp+28],xmm0
       mov       [rsp+20],rcx
       mov       rdi,rbx
       lea       rsi,[rsp+20]
       call      CORINFO_HELP_ASSIGN_BYREF
       call      CORINFO_HELP_ASSIGN_BYREF
       movsq
       mov       eax,1
       add       rsp,38
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       pop       r15
       ret
M14_L06:
       dec       r14d
       jns       near ptr M14_L01
M14_L07:
       mov       eax,0FFFFFFFF
       jmp       near ptr M14_L03
M14_L08:
       mov       rsi,[rsi+8]
       test      rsi,rsi
       je        short M14_L10
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rsp+20],xmm0
       vmovdqu   xmmword ptr [rsp+28],xmm0
       jmp       near ptr M14_L00
M14_L09:
       mov       rcx,[rsi+18]
       cmp       [rcx+8],eax
       jle       near ptr M14_L04
       mov       rcx,[rsi+18]
       cmp       eax,[rcx+8]
       jae       short M14_L11
       mov       edx,eax
       mov       rcx,[rcx+rdx*8+10]
       jmp       near ptr M14_L05
M14_L10:
       xor       eax,eax
       mov       [rbx],rax
       mov       [rbx+8],rax
       mov       [rbx+10],rax
       add       rsp,38
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       pop       r15
       ret
M14_L11:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 396
```
```assembly
; AuroraScript.Runtime.Property.PropertyDescriptor.get_Getter()
       mov       rcx,[rcx]
       test      rcx,rcx
       jne       short M15_L00
       xor       eax,eax
       ret
M15_L00:
       mov       rax,[rcx+8]
       ret
; Total bytes of code 16
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
; System.Runtime.CompilerServices.CastHelpers.IsInstance_Helper(Void*, System.Object)
       push      rsi
       push      rbx
       sub       rsp,28
       mov       rbx,rcx
       mov       rsi,rdx
       call      qword ptr [7FF96E839C18]
       mov       rax,[rax]
       mov       rdx,[rsi]
       add       rax,10
       mov       r8,rdx
       rol       r8,20
       xor       r8,rbx
       mov       r10,9E3779B97F4A7C15
       imul      r8,r10
       mov       ecx,[rax]
       shr       r8,cl
       xor       ecx,ecx
M17_L00:
       lea       r10d,[r8+1]
       movsxd    r10,r10d
       lea       r10,[r10+r10*2]
       lea       r10,[rax+r10*8]
       mov       r9d,[r10]
       mov       r11,[r10+8]
       and       r9d,0FFFFFFFE
       cmp       r11,rdx
       jne       short M17_L02
       mov       r11,rbx
       xor       r11,[r10+10]
       cmp       r11,1
       ja        short M17_L02
       cmp       r9d,[r10]
       jne       short M17_L04
M17_L01:
       cmp       r11d,1
       jne       short M17_L03
       mov       rax,rsi
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
M17_L02:
       test      r9d,r9d
       je        short M17_L04
       inc       ecx
       add       r8d,ecx
       and       r8d,[rax+4]
       cmp       ecx,8
       jl        short M17_L00
       jmp       short M17_L04
M17_L03:
       test      r11d,r11d
       jne       short M17_L05
       xor       eax,eax
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
M17_L04:
       mov       r11d,2
       jmp       short M17_L01
M17_L05:
       mov       rcx,rbx
       mov       rdx,rsi
       call      qword ptr [7FF96E854CA8]; Precode of System.Runtime.CompilerServices.CastHelpers.IsInstanceOfAny_NoCacheLookup(Void*, System.Object)
       nop
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
; Total bytes of code 187
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
```assembly
; System.Number.FormatFloat[[System.Double, System.Private.CoreLib],[System.Char, System.Private.CoreLib]](System.Collections.Generic.ValueListBuilder`1<Char> ByRef, Double, System.ReadOnlySpan`1<Char>, System.Globalization.NumberFormatInfo)
       push      rbp
       push      r15
       push      r14
       push      r13
       push      r12
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,188
       vmovaps   [rsp+170],xmm6
       lea       rbp,[rsp+40]
       vxorps    xmm4,xmm4,xmm4
       vmovdqa   xmmword ptr [rbp+20],xmm4
       mov       rax,0FFFFFFFFFFFFFF10
M19_L00:
       vmovdqa   xmmword ptr [rbp+rax+120],xmm4
       vmovdqa   xmmword ptr [rbp+rax+130],xmm4
       vmovdqa   xmmword ptr [rbp+rax+140],xmm4
       add       rax,30
       jne       short M19_L00
       mov       [rbp+120],rax
       mov       rax,383870C868D3
       mov       [rbp+8],rax
       mov       rdi,rcx
       vmovaps   xmm6,xmm1
       mov       rbx,r8
       mov       rsi,r9
       vmovq     rax,xmm6
       mov       rcx,7FF0000000000000
       andn      rax,rax,rcx
       je        near ptr M19_L20
       mov       r14,[rbx]
       mov       r15d,[rbx+8]
       xor       ecx,ecx
       test      r15d,r15d
       jg        near ptr M19_L27
M19_L01:
       mov       dword ptr [rbp+128],0FFFFFFFF
       test      r15d,r15d
       jne       near ptr M19_L37
M19_L02:
       mov       ecx,47
M19_L03:
       mov       r14d,ecx
       test      [rsp],esp
       sub       rsp,310
       lea       r9,[rsp+40]
       mov       edx,0F
       test      ecx,ecx
       cmovne    edx,[rbp+128]
       mov       [rbp+128],edx
       xor       edx,edx
       mov       [rbp+108],edx
       mov       [rbp+10C],edx
       mov       byte ptr [rbp+110],0
       mov       byte ptr [rbp+111],0
       mov       byte ptr [rbp+112],3
       mov       [rbp+118],r9
       mov       dword ptr [rbp+120],301
       cmp       dword ptr [rbp+120],0
       jbe       near ptr M19_L51
       mov       r9,[rbp+118]
       mov       byte ptr [r9],0
       vmovq     r9,xmm6
       shr       r9,3F
       mov       [rbp+110],r9b
       lea       r9,[rbp+100]
       lea       rdx,[rbp+128]
       mov       r8,rsi
       call      qword ptr [7FF977BC7150]; System.Number.GetFloatingPointMaxDigitsAndPrecision(Char, Int32 ByRef, System.Globalization.NumberFormatInfo, Boolean ByRef)
       mov       r15d,eax
       vxorps    xmm0,xmm0,xmm0
       vucomisd  xmm6,xmm0
       jp        short M19_L04
       je        near ptr M19_L16
M19_L04:
       cmp       byte ptr [rbp+100],0
       je        near ptr M19_L49
       mov       r13d,[rbp+128]
       vmovq     rax,xmm6
       test      rax,rax
       jl        near ptr M19_L38
       vmovaps   xmm0,xmm6
M19_L05:
       cmp       r13d,0FFFFFFFF
       jne       near ptr M19_L48
       vmovq     rax,xmm0
       mov       rdx,0FFFFFFFFFFFFF
       and       rdx,rax
       shr       rax,34
       and       eax,7FF
       je        near ptr M19_L39
       mov       rcx,10000000000000
       or        rdx,rcx
       add       eax,0FFFFFBCD
M19_L06:
       lea       rcx,[rdx*2+1]
       lea       r8d,[rax-1]
       xor       r10d,r10d
       lzcnt     r10,rcx
       shlx      rcx,rcx,r10
       sub       r8d,r10d
       mov       r10,10000000000000
       cmp       rdx,r10
       je        near ptr M19_L40
       lea       r10,[rdx*2-1]
       lea       r9d,[rax-1]
M19_L07:
       sub       r9d,r8d
       shlx      r10,r10,r9
       xor       r9d,r9d
       lzcnt     r9,rdx
       shlx      rdx,rdx,r9
       sub       eax,r9d
       mov       r13,[rbp+118]
       mov       [rbp+18],r13
       mov       r12d,[rbp+120]
       mov       [rbp+3C],r12d
       lea       r9d,[rax+40]
       neg       r9d
       add       r9d,3
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2sd xmm0,xmm0,r9d
       vmulsd    xmm0,xmm0,qword ptr [7FF9778C67B0]
       vroundsd  xmm0,xmm0,xmm0,0A
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       r9d,7FFFFFFF
       vcvttsd2si r11d,xmm1
       vucomisd  xmm0,qword ptr [7FF9778C67B8]
       cmovb     r9d,r11d
       add       r9d,15B
       mov       r11d,r9d
       sar       r11d,1F
       and       r11d,7
       add       r9d,r11d
       sar       r9d,3
       inc       r9d
       cmp       r9d,57
       jae       near ptr M19_L51
       mov       r11d,r9d
       mov       r12,7FF96DBD5658
       movsx     r12,word ptr [r12+r11*2]
       mov       [rbp+0CC],r12d
       mov       r11d,r9d
       mov       r12,7FF96DBC4128
       mov       r11,[r12+r11*8]
       mov       r12,7FF96DBCCF40
       movsx     r9,word ptr [r12+r9*2]
       mov       r12d,edx
       mov       r13,r11
       shr       r13,20
       mov       [rbp+0B0],r11d
       shr       rdx,20
       mov       r11d,edx
       mov       r13d,r13d
       imul      r11,r13
       mov       [rbp+0A8],r11
       mov       r11,r12
       imul      r11,r13
       mov       [rbp+0A0],r11
       mov       edx,edx
       mov       r11d,[rbp+0B0]
       imul      rdx,r11
       mov       [rbp+98],rdx
       imul      r12,r11
       shr       r12,20
       mov       edx,edx
       add       rdx,r12
       mov       r12d,[rbp+0A0]
       add       rdx,r12
       mov       r12d,80000000
       add       rdx,r12
       mov       [rbp+90],rdx
       lea       eax,[rax+r9+40]
       mov       r12,[rbp+98]
       shr       r12,20
       add       r12,[rbp+0A8]
       mov       rdx,[rbp+0A0]
       shr       rdx,20
       add       rdx,r12
       mov       r12,[rbp+90]
       shr       r12,20
       add       rdx,r12
       mov       [rbp+0B8],rdx
       mov       [rbp+0C0],eax
       mov       eax,r10d
       shr       r10,20
       mov       edx,r10d
       imul      rdx,r13
       mov       [rbp+88],rdx
       mov       r12,rax
       imul      r12,r13
       mov       r10d,r10d
       imul      r10,r11
       imul      rax,r11
       shr       rax,20
       mov       edx,r10d
       add       rax,rdx
       mov       edx,r12d
       add       rax,rdx
       mov       edx,80000000
       add       rax,rdx
       shr       r10,20
       add       r10,[rbp+88]
       shr       r12,20
       add       r10,r12
       shr       rax,20
       add       rax,r10
       mov       edx,ecx
       shr       rcx,20
       mov       r10d,ecx
       imul      r10,r13
       imul      r13,rdx
       mov       ecx,ecx
       imul      rcx,r11
       imul      rdx,r11
       shr       rdx,20
       mov       r11d,ecx
       add       rdx,r11
       mov       r11d,r13d
       add       rdx,r11
       mov       r11d,80000000
       add       rdx,r11
       lea       r8d,[r8+r9+40]
       shr       rcx,20
       add       rcx,r10
       shr       r13,20
       add       rcx,r13
       shr       rdx,20
       add       rdx,rcx
       mov       r13,[rbp+18]
       mov       r12d,[rbp+3C]
       mov       qword ptr [rbp+80],1
       dec       rax
       inc       rdx
       mov       [rbp+70],rdx
       mov       [rbp+78],r8d
       mov       rcx,[rbp+70]
       sub       rcx,rax
       mov       eax,[rbp+0C0]
       neg       eax
       mov       r8d,eax
       and       r8d,3F
       mov       edx,1
       shlx      r9,rdx,r8
       mov       [rbp+30],r9
       shrx      r11,[rbp+70],r8
       lea       rdx,[r9-1]
       and       rdx,[rbp+70]
       mov       [rbp+68],rdx
       neg       eax
       add       eax,41
       imul      eax,4D1
       sar       eax,0C
       cmp       eax,0A
       jae       near ptr M19_L51
       mov       r10d,eax
       mov       r9,7FF96DBD36D0
       mov       r10d,[r9+r10*4]
       cmp       r11d,r10d
       jb        near ptr M19_L41
M19_L08:
       lea       r9d,[rax+1]
       xor       eax,eax
       mov       [rbp+0D8],eax
       test      r9d,r9d
       jle       near ptr M19_L42
M19_L09:
       mov       eax,r11d
       xor       edx,edx
       div       r10d
       mov       edx,eax
       imul      edx,r10d
       sub       r11d,edx
       mov       edx,[rbp+0D8]
       cmp       edx,r12d
       jae       near ptr M19_L51
       add       eax,30
       mov       [rdx+r13],al
       mov       edx,[rbp+0D8]
       inc       edx
       mov       [rbp+0D8],edx
       dec       r9d
       mov       edx,r11d
       shlx      rdx,rdx,r8
       mov       rax,[rbp+68]
       add       rdx,rax
       cmp       rdx,rcx
       jb        short M19_L10
       mov       edx,0CCCCCCCD
       mov       r10d,r10d
       imul      r10,rdx
       shr       r10,23
       test      r9d,r9d
       jle       near ptr M19_L47
       mov       [rbp+68],rax
       jmp       short M19_L09
M19_L10:
       mov       eax,[rbp+0D8]
       mov       r11,[rbp+70]
       sub       r11,[rbp+0B8]
       mov       [rbp+10],r13
       mov       r10d,r10d
       shlx      r8,r10,r8
       mov       r10,r11
       sub       r10,1
       inc       r11
M19_L11:
       cmp       rdx,r10
       jb        near ptr M19_L44
M19_L12:
       cmp       rdx,r11
       jae       short M19_L13
       mov       rax,rcx
       sub       rax,rdx
       cmp       rax,r8
       jae       near ptr M19_L45
M19_L13:
       cmp       rdx,2
       jb        near ptr M19_L46
       sub       rcx,4
       cmp       rcx,rdx
       setae     al
       movzx     eax,al
M19_L14:
       mov       edx,r9d
       sub       edx,[rbp+0CC]
       mov       [rbp+0D0],edx
       movzx     r13d,al
M19_L15:
       test      r13d,r13d
       je        near ptr M19_L49
       mov       r8d,[rbp+0D8]
       add       r8d,[rbp+0D0]
       mov       [rbp+10C],r8d
       mov       r8d,[rbp+120]
       cmp       [rbp+0D8],r8d
       jae       near ptr M19_L51
       mov       r8,[rbp+118]
       mov       r9d,[rbp+0D8]
       mov       byte ptr [r8+r9],0
       mov       r8d,[rbp+0D8]
       mov       [rbp+108],r8d
M19_L16:
       test      r14d,r14d
       je        near ptr M19_L50
       cmp       dword ptr [rbp+128],0FFFFFFFF
       jne       short M19_L17
       mov       r15d,[rbp+108]
       mov       edx,11
       cmp       r15d,11
       cmovl     r15d,edx
M19_L17:
       mov       [rsp+20],rsi
       lea       rdx,[rbp+108]
       mov       rcx,rdi
       mov       r8d,r14d
       mov       r9d,r15d
       call      qword ptr [7FF977C45FC8]; System.Number.NumberToString[[System.Char, System.Private.CoreLib]](System.Collections.Generic.ValueListBuilder`1<Char> ByRef, NumberBuffer ByRef, Char, Int32, System.Globalization.NumberFormatInfo)
M19_L18:
       xor       eax,eax
       mov       r8,383870C868D3
       cmp       [rbp+8],r8
       je        short M19_L19
       call      CORINFO_HELP_FAIL_FAST
M19_L19:
       nop
       vmovaps   xmm6,[rbp+130]
       lea       rsp,[rbp+148]
       pop       rbx
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       pop       rbp
       ret
M19_L20:
       vucomisd  xmm6,xmm6
       jp        short M19_L21
       je        short M19_L23
M19_L21:
       mov       rax,[rsi+58]
       mov       r8,383870C868D3
       cmp       [rbp+8],r8
       je        short M19_L22
       call      CORINFO_HELP_FAIL_FAST
M19_L22:
       nop
       vmovaps   xmm6,[rbp+130]
       lea       rsp,[rbp+148]
       pop       rbx
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       pop       rbp
       ret
M19_L23:
       vmovq     rax,xmm6
       test      rax,rax
       jl        short M19_L25
       mov       rax,[rsi+60]
       mov       r8,383870C868D3
       cmp       [rbp+8],r8
       je        short M19_L24
       call      CORINFO_HELP_FAIL_FAST
M19_L24:
       nop
       vmovaps   xmm6,[rbp+130]
       lea       rsp,[rbp+148]
       pop       rbx
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       pop       rbp
       ret
M19_L25:
       mov       rax,[rsi+68]
       mov       r8,383870C868D3
       cmp       [rbp+8],r8
       je        short M19_L26
       call      CORINFO_HELP_FAIL_FAST
M19_L26:
       nop
       vmovaps   xmm6,[rbp+130]
       lea       rsp,[rbp+148]
       pop       rbx
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       pop       rbp
       ret
M19_L27:
       movzx     ecx,word ptr [r14]
       mov       eax,ecx
       or        eax,20
       add       eax,0FFFFFF9F
       cmp       eax,19
       ja        near ptr M19_L01
       cmp       r15d,1
       jne       short M19_L29
       mov       dword ptr [rbp+128],0FFFFFFFF
M19_L28:
       jmp       near ptr M19_L03
M19_L29:
       cmp       r15d,2
       jne       short M19_L30
       movzx     eax,word ptr [r14+2]
       add       eax,0FFFFFFD0
       cmp       eax,0A
       jae       short M19_L31
       mov       [rbp+128],eax
       jmp       short M19_L28
M19_L30:
       cmp       r15d,3
       jne       short M19_L31
       movzx     eax,word ptr [r14+2]
       add       eax,0FFFFFFD0
       movzx     edx,word ptr [r14+4]
       add       edx,0FFFFFFD0
       cmp       eax,0A
       jae       short M19_L31
       cmp       edx,0A
       jae       short M19_L31
       lea       eax,[rax+rax*4]
       lea       eax,[rdx+rax*2]
       mov       [rbp+128],eax
       jmp       short M19_L28
M19_L31:
       xor       r13d,r13d
       mov       eax,1
       jmp       short M19_L33
M19_L32:
       cmp       r13d,5F5E100
       jge       short M19_L34
       lea       edx,[rax+1]
       mov       eax,eax
       movzx     eax,word ptr [r14+rax*2]
       lea       r8d,[r13+r13*4]
       lea       r13d,[rax+r8*2-30]
       mov       eax,edx
M19_L33:
       cmp       eax,r15d
       jae       short M19_L36
       mov       edx,eax
       movzx     edx,word ptr [r14+rdx*2]
       add       edx,0FFFFFFD0
       cmp       edx,9
       jbe       short M19_L32
       jmp       short M19_L35
M19_L34:
       call      qword ptr [7FF977C4C498]
       int       3
M19_L35:
       mov       r9d,eax
       cmp       word ptr [r14+r9*2],0
       jne       near ptr M19_L01
M19_L36:
       mov       [rbp+128],r13d
       jmp       near ptr M19_L28
M19_L37:
       test      ecx,ecx
       je        near ptr M19_L02
       xor       ecx,ecx
       jmp       near ptr M19_L03
M19_L38:
       vxorps    xmm0,xmm6,[7FF9778C67C0]
       jmp       near ptr M19_L05
M19_L39:
       mov       eax,0FFFFFBCE
       jmp       near ptr M19_L06
M19_L40:
       lea       r9d,[rax-2]
       mov       r10,3FFFFFFFFFFFFF
       jmp       near ptr M19_L07
M19_L41:
       dec       eax
       cmp       eax,0A
       jae       near ptr M19_L51
       mov       r10d,eax
       mov       r9,7FF96DBD36D0
       mov       r10d,[r9+r10*4]
       jmp       near ptr M19_L08
M19_L42:
       mov       rax,[rbp+68]
       jmp       short M19_L47
M19_L43:
       lea       r13d,[rax-1]
       cmp       r13d,r12d
       jae       near ptr M19_L51
       lea       r13d,[rax-1]
       add       r13,[rbp+10]
       dec       byte ptr [r13]
       add       rdx,r8
       jmp       near ptr M19_L11
M19_L44:
       mov       r13,rcx
       sub       r13,rdx
       cmp       r13,r8
       jb        near ptr M19_L12
       lea       r13,[rdx+r8]
       cmp       r13,r10
       jb        short M19_L43
       mov       r13,r10
       sub       r13,rdx
       mov       [rbp],r13
       lea       r13,[rdx+r8]
       sub       r13,r10
       cmp       [rbp],r13
       jae       short M19_L43
       jmp       near ptr M19_L12
M19_L45:
       lea       rax,[rdx+r8]
       cmp       rax,r11
       jb        short M19_L46
       mov       rax,r11
       sub       rax,rdx
       add       r8,rdx
       sub       r8,r11
       cmp       rax,r8
       jbe       near ptr M19_L13
M19_L46:
       xor       eax,eax
       jmp       near ptr M19_L14
M19_L47:
       lea       rax,[rax+rax*4]
       add       rax,rax
       mov       r10,[rbp+80]
       lea       r10,[r10+r10*4]
       add       r10,r10
       mov       [rbp+80],r10
       lea       r11,[rcx+rcx*4]
       add       r11,r11
       mov       rcx,r11
       shrx      rdx,rax,r8
       mov       r10d,[rbp+0D8]
       cmp       r10d,r12d
       jae       near ptr M19_L51
       add       edx,30
       mov       [r10+r13],dl
       mov       edx,[rbp+0D8]
       inc       edx
       mov       [rbp+0D8],edx
       dec       r9d
       mov       [rbp+0B4],r9d
       mov       r10,[rbp+30]
       lea       rdx,[r10-1]
       and       rax,rdx
       mov       [rbp+68],rax
       cmp       rax,rcx
       mov       [rbp+48],r11
       jae       near ptr M19_L52
       mov       r8d,[rbp+0D8]
       mov       [rbp+64],r8d
       lea       rcx,[rbp+70]
       lea       rdx,[rbp+50]
       lea       r8,[rbp+0B8]
       call      qword ptr [7FF977C4CB70]
       mov       [rbp+20],r13
       mov       [rbp+28],r12d
       mov       r13,[rbp+68]
       mov       [rsp+20],r13
       mov       r13,[rbp+30]
       mov       [rsp+28],r13
       mov       r13,[rbp+80]
       mov       [rsp+30],r13
       lea       rcx,[rbp+20]
       mov       r8,r13
       imul      r8,[rbp+50]
       mov       edx,[rbp+64]
       mov       r9,[rbp+48]
       call      qword ptr [7FF977C45FB0]; System.Number+Grisu3.TryRoundWeedShortest(System.Span`1<Byte>, Int32, UInt64, UInt64, UInt64, UInt64, UInt64)
       mov       r9d,[rbp+0B4]
       jmp       near ptr M19_L14
M19_L48:
       lea       rdx,[rbp+40]
       call      qword ptr [7FF977BC7180]; System.Number.ExtractFractionAndBiasedExponent[[System.Double, System.Private.CoreLib]](Double, Int32 ByRef)
       mov       ecx,[rbp+40]
       mov       [rbp+0F0],rax
       mov       [rbp+0F8],ecx
       lea       rcx,[rbp+0F0]
       lea       rdx,[rbp+0E0]
       call      qword ptr [7FF977BC7198]; System.Number+DiyFp.Normalize()
       vmovdqu   xmm0,xmmword ptr [rbp+118]
       vmovdqu   xmmword ptr [rbp+20],xmm0
       lea       r8,[rbp+0D0]
       mov       [rsp+20],r8
       lea       r8,[rbp+20]
       lea       rcx,[rbp+0E0]
       lea       r9,[rbp+0D8]
       mov       edx,r13d
       call      qword ptr [7FF977BC71B0]; System.Number+Grisu3.TryRunCounted(DiyFp ByRef, Int32, System.Span`1<Byte>, Int32 ByRef, Int32 ByRef)
       mov       r13d,eax
       jmp       near ptr M19_L15
M19_L49:
       movzx     r8d,byte ptr [rbp+100]
       lea       r9,[rbp+108]
       vmovaps   xmm0,xmm6
       mov       edx,[rbp+128]
       call      qword ptr [7FF977BC7258]; System.Number.Dragon4[[System.Double, System.Private.CoreLib]](Double, Int32, Boolean, NumberBuffer ByRef)
       jmp       near ptr M19_L16
M19_L50:
       lea       rdx,[rbp+108]
       mov       rcx,rdi
       mov       r8,rbx
       mov       r9,rsi
       call      qword ptr [7FF977BC7330]; System.Number.NumberToStringFormat[[System.Char, System.Private.CoreLib]](System.Collections.Generic.ValueListBuilder`1<Char> ByRef, NumberBuffer ByRef, System.ReadOnlySpan`1<Char>, System.Globalization.NumberFormatInfo)
       jmp       near ptr M19_L18
M19_L51:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
M19_L52:
       mov       rax,[rbp+68]
       mov       r9d,[rbp+0B4]
       jmp       near ptr M19_L47
; Total bytes of code 2664
```
```assembly
; System.String.Ctor(System.ReadOnlySpan`1<Char>)
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,20
       mov       rbx,[rcx]
       mov       esi,[rcx+8]
       test      esi,esi
       je        short M20_L00
       mov       edx,esi
       mov       rcx,offset MT_System.String
       call      00007FF9D74C4E80
       mov       rdi,rax
       cmp       [rdi],dil
       lea       rcx,[rdi+0C]
       mov       r8d,esi
       add       r8,r8
       mov       rdx,rbx
       call      qword ptr [7FF977845818]; System.SpanHelpers.Memmove(Byte ByRef, Byte ByRef, UIntPtr)
       mov       rax,rdi
       add       rsp,20
       pop       rbx
       pop       rsi
       pop       rdi
       ret
M20_L00:
       mov       rax,0AA6FF60008
       add       rsp,20
       pop       rbx
       pop       rsi
       pop       rdi
       ret
; Total bytes of code 88
```
```assembly
; System.String.FastAllocateString(IntPtr)
       mov       rdx,rcx
       mov       rcx,offset MT_System.String
       jmp       near ptr 00007FF9D74C4E80
; Total bytes of code 18
```
```assembly
; System.Number.UInt32ToDecStrForKnownSmallNumber(UInt32)
       push      rsi
       push      rbx
       sub       rsp,28
       mov       ebx,ecx
       mov       rcx,8A3D400A20
       mov       rcx,[rcx]
       cmp       ebx,12C
       jae       short M22_L01
       mov       eax,ebx
       mov       rsi,[rcx+rax*8+10]
       test      rsi,rsi
       je        short M22_L00
       mov       rcx,7FF977D15410
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rax,rsi
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
M22_L00:
       mov       rcx,7FF977D15414
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rcx,7FF977D15410
       call      CORINFO_HELP_COUNTPROFILE32
       mov       ecx,ebx
       add       rsp,28
       pop       rbx
       pop       rsi
       jmp       qword ptr [7FF977BC55C0]; System.Number.<UInt32ToDecStrForKnownSmallNumber>g__CreateAndCacheString|50_0(UInt32)
M22_L01:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 116
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
       je        short M23_L00
       mov       rcx,rsi
       cmp       [r8],rcx
       je        short M23_L00
       mov       rdx,rax
       call      qword ptr [7FF96E839090]; Precode of System.Runtime.CompilerServices.CastHelpers.ChkCastAny(Void*, System.Object)
       mov       r8,rax
M23_L00:
       mov       rax,r8
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
; Total bytes of code 88
```
```assembly
; System.Runtime.CompilerServices.CastHelpers.ChkCastClassSpecial(Void*, System.Object)
       mov       rax,[rdx]
       mov       rax,[rax+10]
       cmp       rax,rcx
       jne       short M24_L01
M24_L00:
       mov       rax,rdx
       ret
M24_L01:
       test      rax,rax
       je        short M24_L04
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M24_L00
       jmp       short M24_L03
M24_L02:
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M24_L00
       jmp       short M24_L01
M24_L03:
       test      rax,rax
       je        short M24_L04
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M24_L00
       test      rax,rax
       je        short M24_L04
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M24_L00
       test      rax,rax
       jne       short M24_L02
M24_L04:
       lea       rax,[System.Reflection.CustomAttributeExtensions.GetCustomAttribute[[System.__Canon, System.Private.CoreLib]](System.Reflection.Assembly)]
       jmp       qword ptr [rax]
; Total bytes of code 86
```

## .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3 (Job: Job-BZNPUE(IterationCount=5, IterationTime=200ms, LaunchCount=1, WarmupCount=3))

```assembly
; AuroraBenchmark.BoundaryOpsBenchmarks.WriteIntProperty()
       push      r15
       push      r14
       push      r13
       push      r12
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,298
       vxorps    xmm4,xmm4,xmm4
       mov       rax,0FFFFFFFFFFFFFD90
M00_L00:
       vmovdqa   xmmword ptr [rsp+rax+290],xmm4
       vmovdqa   xmmword ptr [rsp+rax+2A0],xmm4
       vmovdqa   xmmword ptr [rsp+rax+2B0],xmm4
       add       rax,30
       jne       short M00_L00
       mov       [rsp+290],rax
       mov       rbx,rcx
M00_L01:
       xor       esi,esi
       jmp       near ptr M00_L31
M00_L02:
       mov       rdx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       cmp       [rcx],rdx
       jne       near ptr M00_L41
       xor       ecx,ecx
M00_L03:
       test      rcx,rcx
       jne       near ptr M00_L42
       mov       r13,rdi
       test      r13,r13
       je        short M00_L04
       mov       rdx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       cmp       [r13],rdx
       jne       near ptr M00_L43
       xor       r13d,r13d
M00_L04:
       test      r13,r13
       jne       near ptr M00_L44
       xor       ecx,ecx
       mov       [rsp+198],rcx
       mov       rcx,401C000000000000
       mov       [rsp+1A0],rcx
       xor       ecx,ecx
       mov       [rsp+190],rcx
       mov       [rsp+180],rdi
       mov       [rsp+188],rbp
       cmp       qword ptr [rsp+180],0
       jne       near ptr M00_L33
       mov       rcx,[rsp+188]
       test      rcx,rcx
       je        near ptr M00_L216
       dec       rcx
       cmp       rcx,1
       jbe       near ptr M00_L217
       mov       r13d,2
M00_L05:
       cmp       r13d,4
       jbe       near ptr M00_L34
       cmp       r13d,1000
       je        near ptr M00_L240
       cmp       r13d,2000
       je        near ptr M00_L241
       mov       rdx,[rsp+180]
       mov       rdi,rdx
       test      rdi,rdi
       je        short M00_L06
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       cmp       [rdi],rcx
       jne       near ptr M00_L218
M00_L06:
       test      rdi,rdi
       je        near ptr M00_L242
M00_L07:
       mov       rbp,rdi
       test      rbp,rbp
       je        short M00_L08
       mov       rdx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       cmp       [rbp],rdx
       jne       near ptr M00_L246
       xor       ebp,ebp
M00_L08:
       test      rbp,rbp
       jne       near ptr M00_L247
M00_L09:
       mov       rbp,rdi
       test      rbp,rbp
       je        short M00_L10
       mov       rdx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       cmp       [rbp],rdx
       jne       near ptr M00_L253
       xor       ebp,ebp
M00_L10:
       test      rbp,rbp
       jne       near ptr M00_L254
M00_L11:
       vmovdqu   xmm0,xmmword ptr [rsp+198]
       vmovdqu   xmmword ptr [rsp+20],xmm0
       lea       rcx,[rsp+20]
       call      qword ptr [7FF977C25D58]; AuroraScript.Runtime.ScriptDatum.ToString(AuroraScript.Runtime.ScriptDatum)
       mov       rbp,rax
       mov       r8,offset MT_AuroraScript.Runtime.Types.ScriptObject
       cmp       [rdi],r8
       jne       near ptr M00_L439
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rsp+70],xmm0
       vmovdqu   xmmword ptr [rsp+78],xmm0
       mov       r13,[rdi+20]
       mov       r8,[r13+8]
       mov       r12d,[r8+8]
       dec       r12d
       js        near ptr M00_L36
M00_L12:
       mov       r8,[r13+8]
       cmp       r12d,[r8+8]
       jae       near ptr M00_L440
       mov       edx,r12d
       shl       rdx,5
       mov       r8,[r8+rdx+10]
       cmp       r8,rbp
       je        short M00_L13
       test      r8,r8
       je        near ptr M00_L35
       test      rbp,rbp
       je        near ptr M00_L35
       mov       edx,[r8+8]
       cmp       edx,[rbp+8]
       jne       near ptr M00_L35
       lea       rcx,[r8+0C]
       mov       r8d,[r8+8]
       add       r8d,r8d
       lea       rdx,[rbp+0C]
       call      qword ptr [7FF97782C330]; System.SpanHelpers.SequenceEqual(Byte ByRef, Byte ByRef, UIntPtr)
       test      eax,eax
       je        near ptr M00_L35
M00_L13:
       mov       r8d,r12d
M00_L14:
       test      r8d,r8d
       jl        near ptr M00_L429
       mov       rcx,[r13+8]
       cmp       r8d,[rcx+8]
       jae       near ptr M00_L440
       mov       r8d,r8d
       shl       r8,5
       movzx     r8d,word ptr [rcx+r8+18]
       cmp       qword ptr [rdi+18],0
       jne       near ptr M00_L430
M00_L15:
       xor       ecx,ecx
M00_L16:
       mov       rdx,[rdi+10]
       cmp       r8d,[rdx+8]
       jae       near ptr M00_L440
       mov       r8d,r8d
       shl       r8,4
       vmovdqu   xmm0,xmmword ptr [rdx+r8+10]
       vmovdqu   xmmword ptr [rsp+50],xmm0
       vmovdqu   xmm0,xmmword ptr [rsp+48]
       vmovdqu   xmmword ptr [rsp+70],xmm0
       mov       r8,[rsp+58]
       mov       [rsp+80],r8
       mov       [rsp+70],rcx
M00_L17:
       mov       rdx,[rsp+70]
       test      rdx,rdx
       jne       near ptr M00_L433
M00_L18:
       mov       rdx,[rsp+78]
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BondingAccessor
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rax,rax
       jne       near ptr M00_L434
M00_L19:
       mov       r13d,3
       mov       r12,[rdi+20]
       mov       r8,[r12+8]
       mov       eax,[r8+8]
       dec       eax
       js        near ptr M00_L38
M00_L20:
       mov       r8,[r12+8]
       cmp       eax,[r8+8]
       jae       near ptr M00_L440
       mov       [rsp+44],eax
       mov       edx,eax
       shl       rdx,5
       mov       r8,[r8+rdx+10]
       cmp       r8,rbp
       je        short M00_L21
       test      r8,r8
       je        near ptr M00_L37
       test      rbp,rbp
       je        near ptr M00_L37
       mov       edx,[r8+8]
       cmp       edx,[rbp+8]
       jne       near ptr M00_L37
       lea       rcx,[r8+0C]
       mov       r8d,[r8+8]
       add       r8d,r8d
       lea       rdx,[rbp+0C]
       call      qword ptr [7FF97782C330]; System.SpanHelpers.SequenceEqual(Byte ByRef, Byte ByRef, UIntPtr)
       test      eax,eax
       je        near ptr M00_L37
M00_L21:
       mov       eax,[rsp+44]
M00_L22:
       test      eax,eax
       jl        short M00_L23
       mov       rcx,[r12+8]
       cmp       eax,[rcx+8]
       jae       near ptr M00_L440
       mov       eax,eax
       shl       rax,5
       movzx     r13d,word ptr [rcx+rax+1C]
       and       r13d,18
       or        r13d,3
M00_L23:
       xor       eax,eax
       mov       [rsp+38],rax
       mov       eax,[rdi+30]
       test      al,2
       jne       near ptr M00_L30
       test      al,1
       jne       near ptr M00_L435
M00_L24:
       mov       r12,[rdi+20]
       mov       r8,[r12+8]
       mov       eax,[r8+8]
       dec       eax
       js        near ptr M00_L40
M00_L25:
       mov       r8,[r12+8]
       cmp       eax,[r8+8]
       jae       near ptr M00_L440
       mov       [rsp+34],eax
       mov       edx,eax
       shl       rdx,5
       mov       r8,[r8+rdx+10]
       cmp       r8,rbp
       je        short M00_L26
       test      r8,r8
       je        near ptr M00_L39
       test      rbp,rbp
       je        near ptr M00_L39
       mov       edx,[r8+8]
       cmp       edx,[rbp+8]
       jne       near ptr M00_L39
       lea       rcx,[r8+0C]
       mov       r8d,[r8+8]
       add       r8d,r8d
       lea       rdx,[rbp+0C]
       call      qword ptr [7FF97782C330]; System.SpanHelpers.SequenceEqual(Byte ByRef, Byte ByRef, UIntPtr)
       test      eax,eax
       je        near ptr M00_L39
M00_L26:
       mov       eax,[rsp+34]
M00_L27:
       test      eax,eax
       jl        near ptr M00_L436
       mov       rcx,[r12+8]
       cmp       eax,[rcx+8]
       jae       near ptr M00_L440
       mov       eax,eax
       shl       rax,5
       mov       rax,[rcx+rax+18]
       mov       [rsp+38],rax
       test      byte ptr [rsp+3C],1
       je        near ptr M00_L437
M00_L28:
       mov       rcx,[rdi+20]
       lea       r9,[rsp+38]
       mov       rdx,rbp
       mov       r8d,r13d
       cmp       [rcx],ecx
       call      qword ptr [7FF9779FE928]; AuroraScript.Runtime.Property.HiddenClass.AddProperty(System.String, AuroraScript.Runtime.Property.PropertyFlags, AuroraScript.Runtime.Property.PropertyMeta ByRef)
       lea       rcx,[rdi+20]
       mov       rdx,rax
       call      CORINFO_HELP_ASSIGN_REF
       movzx     ecx,word ptr [rsp+38]
       mov       rax,[rdi+10]
       mov       eax,[rax+8]
       cmp       ecx,eax
       jge       near ptr M00_L438
M00_L29:
       mov       rcx,[rdi+10]
       movzx     edx,word ptr [rsp+38]
       cmp       edx,[rcx+8]
       jae       near ptr M00_L440
       shl       rdx,4
       lea       rbp,[rcx+rdx+10]
       mov       rcx,rbp
       mov       rdx,r14
       call      CORINFO_HELP_ASSIGN_REF
       mov       [rbp+8],r15
M00_L30:
       inc       esi
       cmp       esi,200
       jge       short M00_L32
M00_L31:
       mov       rdi,[rbx+68]
       mov       rbp,[rbx+70]
       mov       rdx,[rbx+8]
       mov       ecx,esi
       and       ecx,1F
       cmp       ecx,[rdx+8]
       jae       near ptr M00_L440
       mov       ecx,esi
       and       ecx,1F
       shl       rcx,4
       lea       rdx,[rdx+rcx+10]
       mov       r14,[rdx]
       mov       r15,[rdx+8]
       mov       rcx,rdi
       test      rcx,rcx
       je        near ptr M00_L03
       jmp       near ptr M00_L02
M00_L32:
       add       rsp,298
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       ret
M00_L33:
       mov       rcx,[rsp+180]
       mov       rax,0BA3C000448
       cmp       rcx,[rax]
       je        near ptr M00_L214
       mov       rcx,[rsp+180]
       mov       rax,0BA3C000450
       cmp       rcx,[rax]
       je        near ptr M00_L215
       movsx     r13,word ptr [rsp+188]
       jmp       near ptr M00_L05
M00_L34:
       mov       ecx,r13d
       lea       rax,[7FF9778B2458]
       mov       eax,[rax+rcx*4]
       lea       rdx,[M00_L01]
       add       rax,rdx
       jmp       rax
       lea       rcx,[rsp+180]
       call      qword ptr [7FF977C25E60]
       mov       rdi,rax
       jmp       near ptr M00_L07
       mov       rcx,[rsp+188]
       mov       rax,8007FFFFFFFFFFFF
       add       rax,rcx
       cmp       rax,3
       jbe       near ptr M00_L223
       jmp       near ptr M00_L222
       cmp       qword ptr [rsp+188],2
       je        near ptr M00_L220
       jmp       near ptr M00_L219
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rdx,0BA3C000408
       mov       rdi,[rdx]
       jmp       near ptr M00_L07
M00_L35:
       dec       r12d
       jns       near ptr M00_L12
M00_L36:
       mov       r8d,0FFFFFFFF
       jmp       near ptr M00_L14
M00_L37:
       mov       eax,[rsp+44]
       dec       eax
       jns       near ptr M00_L20
M00_L38:
       mov       eax,0FFFFFFFF
       jmp       near ptr M00_L22
M00_L39:
       mov       eax,[rsp+34]
       dec       eax
       jns       near ptr M00_L25
M00_L40:
       mov       eax,0FFFFFFFF
       jmp       near ptr M00_L27
M00_L41:
       mov       rdx,rdi
       mov       rcx,offset MT_AuroraScript.Runtime.Types.IAuroraNativeIndexer
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfInterface(Void*, System.Object)
       mov       rcx,rax
       jmp       near ptr M00_L03
M00_L42:
       mov       [rsp+20],r14
       mov       [rsp+28],r15
       lea       r8,[rsp+20]
       mov       r11,7FF977770500
       mov       edx,7
       call      qword ptr [r11]
       jmp       near ptr M00_L30
M00_L43:
       mov       rdx,rdi
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptPackedArray
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r13,rax
       jmp       near ptr M00_L04
M00_L44:
       mov       rdx,r13
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt32Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       near ptr M00_L45
       mov       rdx,r13
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt8Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       near ptr M00_L59
       mov       rdx,r13
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptFloat32Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       near ptr M00_L73
       mov       rdx,r13
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptFloat64Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       near ptr M00_L87
       mov       rdx,r13
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptBooleanArray
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       near ptr M00_L101
       mov       rdx,r13
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptUInt8Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       near ptr M00_L115
       mov       rdx,r13
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt16Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       near ptr M00_L129
       mov       rdx,r13
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptUInt16Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       near ptr M00_L143
       mov       rdx,r13
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptUInt32Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       near ptr M00_L157
       mov       rdx,r13
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt64Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       near ptr M00_L171
       mov       rdx,r13
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptUInt64Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       near ptr M00_L191
       mov       rcx,r13
       mov       rax,[r13]
       mov       rax,[rax+58]
       call      qword ptr [rax+8]
       cmp       eax,7
       ja        near ptr M00_L213
       jmp       near ptr M00_L212
M00_L45:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],7
       jg        short M00_L46
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,7
       call      qword ptr [7FF977C2D4B8]
M00_L46:
       mov       rdi,[rdi+38]
       xor       ecx,ecx
       mov       [rsp+290],rcx
       mov       [rsp+280],r14
       mov       [rsp+288],r15
       cmp       qword ptr [rsp+280],0
       je        short M00_L47
       mov       rcx,[rsp+280]
       mov       rdx,0BA3C000448
       cmp       rcx,[rdx]
       je        short M00_L51
       mov       rcx,[rsp+280]
       mov       rdx,0BA3C000450
       cmp       rcx,[rdx]
       je        short M00_L51
       movsx     rcx,word ptr [rsp+288]
       jmp       short M00_L50
M00_L47:
       mov       rcx,[rsp+288]
       test      rcx,rcx
       je        short M00_L48
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L49
       mov       ecx,2
       jmp       short M00_L50
M00_L48:
       xor       ecx,ecx
       jmp       short M00_L50
M00_L49:
       mov       ecx,1
M00_L50:
       test      ecx,ecx
       jne       short M00_L51
       xor       ecx,ecx
       mov       [rsp+290],rcx
       jmp       near ptr M00_L57
M00_L51:
       cmp       qword ptr [rsp+280],0
       jne       short M00_L55
       cmp       qword ptr [rsp+288],2
       jbe       short M00_L55
       mov       rcx,[rsp+288]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L52
       vmovq     xmm0,rcx
       jmp       short M00_L54
M00_L52:
       cmp       rdx,2
       jbe       short M00_L53
       vmovsd    xmm0,qword ptr [7FF9778B2470]
       jmp       short M00_L54
M00_L53:
       vmovq     xmm0,rdx
M00_L54:
       vmovsd    qword ptr [rsp+290],xmm0
       mov       r14d,1
       jmp       short M00_L56
M00_L55:
       lea       rcx,[rsp+280]
       lea       rdx,[rsp+290]
       call      qword ptr [7FF977C2D4D0]
       mov       r14d,eax
M00_L56:
       test      r14d,r14d
       jne       short M00_L57
       vmovsd    xmm0,qword ptr [7FF9778B2470]
       jmp       short M00_L58
M00_L57:
       vmovsd    xmm0,qword ptr [rsp+290]
M00_L58:
       cmp       dword ptr [rdi+8],7
       jbe       near ptr M00_L440
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       r8d,7FFFFFFF
       vcvttsd2si ecx,xmm1
       vucomisd  xmm0,qword ptr [7FF9778B2478]
       cmovb     r8d,ecx
       mov       [rdi+2C],r8d
       jmp       near ptr M00_L30
M00_L59:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],7
       jg        short M00_L60
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,7
       call      qword ptr [7FF977C2D4B8]
M00_L60:
       mov       rdi,[rdi+38]
       xor       ecx,ecx
       mov       [rsp+278],rcx
       mov       [rsp+268],r14
       mov       [rsp+270],r15
       cmp       qword ptr [rsp+268],0
       je        short M00_L61
       mov       rcx,[rsp+268]
       mov       rdx,0BA3C000448
       cmp       rcx,[rdx]
       je        short M00_L65
       mov       rcx,[rsp+268]
       mov       rdx,0BA3C000450
       cmp       rcx,[rdx]
       je        short M00_L65
       movsx     rcx,word ptr [rsp+270]
       jmp       short M00_L64
M00_L61:
       mov       rcx,[rsp+270]
       test      rcx,rcx
       je        short M00_L62
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L63
       mov       ecx,2
       jmp       short M00_L64
M00_L62:
       xor       ecx,ecx
       jmp       short M00_L64
M00_L63:
       mov       ecx,1
M00_L64:
       test      ecx,ecx
       jne       short M00_L65
       xor       ecx,ecx
       mov       [rsp+278],rcx
       jmp       near ptr M00_L71
M00_L65:
       cmp       qword ptr [rsp+268],0
       jne       short M00_L69
       cmp       qword ptr [rsp+270],2
       jbe       short M00_L69
       mov       rcx,[rsp+270]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L66
       vmovq     xmm0,rcx
       jmp       short M00_L68
M00_L66:
       cmp       rdx,2
       jbe       short M00_L67
       vmovsd    xmm0,qword ptr [7FF9778B2470]
       jmp       short M00_L68
M00_L67:
       vmovq     xmm0,rdx
M00_L68:
       vmovsd    qword ptr [rsp+278],xmm0
       mov       r14d,1
       jmp       short M00_L70
M00_L69:
       lea       rcx,[rsp+268]
       lea       rdx,[rsp+278]
       call      qword ptr [7FF977C2D4D0]
       mov       r14d,eax
M00_L70:
       test      r14d,r14d
       jne       short M00_L71
       vmovsd    xmm0,qword ptr [7FF9778B2470]
       jmp       short M00_L72
M00_L71:
       vmovsd    xmm0,qword ptr [rsp+278]
M00_L72:
       cmp       dword ptr [rdi+8],7
       jbe       near ptr M00_L440
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       edx,7FFFFFFF
       vcvttsd2si ecx,xmm1
       vucomisd  xmm0,qword ptr [7FF9778B2478]
       cmovb     edx,ecx
       mov       [rdi+17],dl
       jmp       near ptr M00_L30
M00_L73:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],7
       jg        short M00_L74
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,7
       call      qword ptr [7FF977C2D4B8]
M00_L74:
       mov       rdi,[rdi+38]
       xor       ecx,ecx
       mov       [rsp+260],rcx
       mov       [rsp+250],r14
       mov       [rsp+258],r15
       cmp       qword ptr [rsp+250],0
       je        short M00_L75
       mov       rcx,[rsp+250]
       mov       rdx,0BA3C000448
       cmp       rcx,[rdx]
       je        short M00_L79
       mov       rcx,[rsp+250]
       mov       rdx,0BA3C000450
       cmp       rcx,[rdx]
       je        short M00_L79
       movsx     rcx,word ptr [rsp+258]
       jmp       short M00_L78
M00_L75:
       mov       rcx,[rsp+258]
       test      rcx,rcx
       je        short M00_L76
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L77
       mov       ecx,2
       jmp       short M00_L78
M00_L76:
       xor       ecx,ecx
       jmp       short M00_L78
M00_L77:
       mov       ecx,1
M00_L78:
       test      ecx,ecx
       jne       short M00_L79
       xor       ecx,ecx
       mov       [rsp+260],rcx
       jmp       near ptr M00_L85
M00_L79:
       cmp       qword ptr [rsp+250],0
       jne       short M00_L83
       cmp       qword ptr [rsp+258],2
       jbe       short M00_L83
       mov       rcx,[rsp+258]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L80
       vmovq     xmm0,rcx
       jmp       short M00_L82
M00_L80:
       cmp       rdx,2
       jbe       short M00_L81
       vmovsd    xmm0,qword ptr [7FF9778B2470]
       jmp       short M00_L82
M00_L81:
       vmovq     xmm0,rdx
M00_L82:
       vmovsd    qword ptr [rsp+260],xmm0
       mov       r14d,1
       jmp       short M00_L84
M00_L83:
       lea       rcx,[rsp+250]
       lea       rdx,[rsp+260]
       call      qword ptr [7FF977C2D4D0]
       mov       r14d,eax
M00_L84:
       test      r14d,r14d
       jne       short M00_L85
       vmovsd    xmm0,qword ptr [7FF9778B2470]
       jmp       short M00_L86
M00_L85:
       vmovsd    xmm0,qword ptr [rsp+260]
M00_L86:
       cmp       dword ptr [rdi+8],7
       jbe       near ptr M00_L440
       vcvtsd2ss xmm0,xmm0,xmm0
       vmovss    dword ptr [rdi+2C],xmm0
       jmp       near ptr M00_L30
M00_L87:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],7
       jg        short M00_L88
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,7
       call      qword ptr [7FF977C2D4B8]
M00_L88:
       mov       rdi,[rdi+38]
       xor       ecx,ecx
       mov       [rsp+248],rcx
       mov       [rsp+238],r14
       mov       [rsp+240],r15
       cmp       qword ptr [rsp+238],0
       je        short M00_L89
       mov       rcx,[rsp+238]
       mov       rdx,0BA3C000448
       cmp       rcx,[rdx]
       je        short M00_L93
       mov       rcx,[rsp+238]
       mov       rdx,0BA3C000450
       cmp       rcx,[rdx]
       je        short M00_L93
       movsx     rcx,word ptr [rsp+240]
       jmp       short M00_L92
M00_L89:
       mov       rcx,[rsp+240]
       test      rcx,rcx
       je        short M00_L90
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L91
       mov       ecx,2
       jmp       short M00_L92
M00_L90:
       xor       ecx,ecx
       jmp       short M00_L92
M00_L91:
       mov       ecx,1
M00_L92:
       test      ecx,ecx
       jne       short M00_L93
       xor       ecx,ecx
       mov       [rsp+248],rcx
       jmp       near ptr M00_L99
M00_L93:
       cmp       qword ptr [rsp+238],0
       jne       short M00_L97
       cmp       qword ptr [rsp+240],2
       jbe       short M00_L97
       mov       rcx,[rsp+240]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L94
       vmovq     xmm0,rcx
       jmp       short M00_L96
M00_L94:
       cmp       rdx,2
       jbe       short M00_L95
       vmovsd    xmm0,qword ptr [7FF9778B2470]
       jmp       short M00_L96
M00_L95:
       vmovq     xmm0,rdx
M00_L96:
       vmovsd    qword ptr [rsp+248],xmm0
       mov       r14d,1
       jmp       short M00_L98
M00_L97:
       lea       rcx,[rsp+238]
       lea       rdx,[rsp+248]
       call      qword ptr [7FF977C2D4D0]
       mov       r14d,eax
M00_L98:
       test      r14d,r14d
       jne       short M00_L99
       vmovsd    xmm0,qword ptr [7FF9778B2470]
       jmp       short M00_L100
M00_L99:
       vmovsd    xmm0,qword ptr [rsp+248]
M00_L100:
       cmp       dword ptr [rdi+8],7
       jbe       near ptr M00_L440
       vmovsd    qword ptr [rdi+48],xmm0
       jmp       near ptr M00_L30
M00_L101:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],7
       jg        short M00_L102
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,7
       call      qword ptr [7FF977C2D4B8]
M00_L102:
       mov       rdi,[rdi+38]
       mov       [rsp+228],r14
       mov       [rsp+230],r15
       cmp       qword ptr [rsp+228],0
       je        short M00_L105
       mov       rdx,[rsp+228]
       mov       rcx,0BA3C000448
       cmp       rdx,[rcx]
       jne       short M00_L103
       mov       r14d,1000
       jmp       short M00_L108
M00_L103:
       mov       rdx,[rsp+228]
       mov       rcx,0BA3C000450
       cmp       rdx,[rcx]
       jne       short M00_L104
       mov       r14d,2000
       jmp       short M00_L108
M00_L104:
       movsx     r14,word ptr [rsp+230]
       jmp       short M00_L108
M00_L105:
       mov       rdx,[rsp+230]
       test      rdx,rdx
       je        short M00_L106
       dec       rdx
       cmp       rdx,1
       jbe       short M00_L107
       mov       r14d,2
       jmp       short M00_L108
M00_L106:
       xor       r14d,r14d
       jmp       short M00_L108
M00_L107:
       mov       r14d,1
M00_L108:
       cmp       r14d,4
       ja        short M00_L109
       mov       edx,r14d
       lea       rcx,[7FF9778B2480]
       mov       ecx,[rcx+rdx*4]
       lea       rax,[M00_L01]
       add       rcx,rax
       jmp       rcx
M00_L109:
       cmp       r14d,1000
       je        near ptr M00_L111
       cmp       r14d,2000
       je        near ptr M00_L111
       lea       rcx,[rsp+228]
       call      qword ptr [7FF977C25E78]; AuroraScript.Runtime.ScriptDatum.get_Object()
       mov       r15,rax
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rdx,0BA3C000408
       cmp       r15,[rdx]
       setne     r15b
       movzx     r15d,r15b
       jmp       near ptr M00_L114
       cmp       qword ptr [rsp+230],2
       sete      r15b
       movzx     r15d,r15b
       jmp       near ptr M00_L114
       mov       rcx,7FF8000000000001
       cmp       [rsp+230],rcx
       je        short M00_L110
       mov       rcx,8000000000000000
       cmp       [rsp+230],rcx
       je        short M00_L110
       mov       rcx,7FF8000000000004
       cmp       [rsp+230],rcx
       setne     r15b
       movzx     r15d,r15b
       jmp       short M00_L114
M00_L110:
       xor       r15d,r15d
       jmp       short M00_L114
M00_L111:
       cmp       qword ptr [rsp+230],0
       setne     r15b
       movzx     r15d,r15b
       jmp       short M00_L114
       mov       rdx,[rsp+228]
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rax,rax
       je        short M00_L112
       cmp       dword ptr [rax+8],0
       sete      cl
       movzx     ecx,cl
       jmp       short M00_L113
M00_L112:
       mov       ecx,1
M00_L113:
       test      ecx,ecx
       sete      r15b
       movzx     r15d,r15b
M00_L114:
       cmp       dword ptr [rdi+8],7
       jbe       near ptr M00_L440
       mov       [rdi+17],r15b
       jmp       near ptr M00_L30
M00_L115:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],7
       jg        short M00_L116
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,7
       call      qword ptr [7FF977C2D4B8]
M00_L116:
       mov       rdi,[rdi+38]
       xor       ecx,ecx
       mov       [rsp+220],rcx
       mov       [rsp+210],r14
       mov       [rsp+218],r15
       cmp       qword ptr [rsp+210],0
       je        short M00_L117
       mov       rcx,[rsp+210]
       mov       rdx,0BA3C000448
       cmp       rcx,[rdx]
       je        short M00_L121
       mov       rcx,[rsp+210]
       mov       rdx,0BA3C000450
       cmp       rcx,[rdx]
       je        short M00_L121
       movsx     rcx,word ptr [rsp+218]
       jmp       short M00_L120
M00_L117:
       mov       rcx,[rsp+218]
       test      rcx,rcx
       je        short M00_L118
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L119
       mov       ecx,2
       jmp       short M00_L120
M00_L118:
       xor       ecx,ecx
       jmp       short M00_L120
M00_L119:
       mov       ecx,1
M00_L120:
       test      ecx,ecx
       jne       short M00_L121
       xor       ecx,ecx
       mov       [rsp+220],rcx
       jmp       near ptr M00_L127
M00_L121:
       cmp       qword ptr [rsp+210],0
       jne       short M00_L125
       cmp       qword ptr [rsp+218],2
       jbe       short M00_L125
       mov       rcx,[rsp+218]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L122
       vmovq     xmm0,rcx
       jmp       short M00_L124
M00_L122:
       cmp       rdx,2
       jbe       short M00_L123
       vmovsd    xmm0,qword ptr [7FF9778B2470]
       jmp       short M00_L124
M00_L123:
       vmovq     xmm0,rdx
M00_L124:
       vmovsd    qword ptr [rsp+220],xmm0
       mov       r14d,1
       jmp       short M00_L126
M00_L125:
       lea       rcx,[rsp+210]
       lea       rdx,[rsp+220]
       call      qword ptr [7FF977C2D4D0]
       mov       r14d,eax
M00_L126:
       test      r14d,r14d
       jne       short M00_L127
       vmovsd    xmm0,qword ptr [7FF9778B2470]
       jmp       short M00_L128
M00_L127:
       vmovsd    xmm0,qword ptr [rsp+220]
M00_L128:
       cmp       dword ptr [rdi+8],7
       jbe       near ptr M00_L440
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       edx,7FFFFFFF
       vcvttsd2si ecx,xmm1
       vucomisd  xmm0,qword ptr [7FF9778B2478]
       cmovb     edx,ecx
       mov       [rdi+17],dl
       jmp       near ptr M00_L30
M00_L129:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],7
       jg        short M00_L130
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,7
       call      qword ptr [7FF977C2D4B8]
M00_L130:
       mov       rdi,[rdi+38]
       xor       ecx,ecx
       mov       [rsp+208],rcx
       mov       [rsp+1F8],r14
       mov       [rsp+200],r15
       cmp       qword ptr [rsp+1F8],0
       je        short M00_L131
       mov       rcx,[rsp+1F8]
       mov       rdx,0BA3C000448
       cmp       rcx,[rdx]
       je        short M00_L135
       mov       rcx,[rsp+1F8]
       mov       rdx,0BA3C000450
       cmp       rcx,[rdx]
       je        short M00_L135
       movsx     rcx,word ptr [rsp+200]
       jmp       short M00_L134
M00_L131:
       mov       rcx,[rsp+200]
       test      rcx,rcx
       je        short M00_L132
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L133
       mov       ecx,2
       jmp       short M00_L134
M00_L132:
       xor       ecx,ecx
       jmp       short M00_L134
M00_L133:
       mov       ecx,1
M00_L134:
       test      ecx,ecx
       jne       short M00_L135
       xor       ecx,ecx
       mov       [rsp+208],rcx
       jmp       near ptr M00_L141
M00_L135:
       cmp       qword ptr [rsp+1F8],0
       jne       short M00_L139
       cmp       qword ptr [rsp+200],2
       jbe       short M00_L139
       mov       rcx,[rsp+200]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L136
       vmovq     xmm0,rcx
       jmp       short M00_L138
M00_L136:
       cmp       rdx,2
       jbe       short M00_L137
       vmovsd    xmm0,qword ptr [7FF9778B2470]
       jmp       short M00_L138
M00_L137:
       vmovq     xmm0,rdx
M00_L138:
       vmovsd    qword ptr [rsp+208],xmm0
       mov       r14d,1
       jmp       short M00_L140
M00_L139:
       lea       rcx,[rsp+1F8]
       lea       rdx,[rsp+208]
       call      qword ptr [7FF977C2D4D0]
       mov       r14d,eax
M00_L140:
       test      r14d,r14d
       jne       short M00_L141
       vmovsd    xmm0,qword ptr [7FF9778B2470]
       jmp       short M00_L142
M00_L141:
       vmovsd    xmm0,qword ptr [rsp+208]
M00_L142:
       cmp       dword ptr [rdi+8],7
       jbe       near ptr M00_L440
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       edx,7FFFFFFF
       vcvttsd2si ecx,xmm1
       vucomisd  xmm0,qword ptr [7FF9778B2478]
       cmovb     edx,ecx
       mov       [rdi+1E],dx
       jmp       near ptr M00_L30
M00_L143:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],7
       jg        short M00_L144
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,7
       call      qword ptr [7FF977C2D4B8]
M00_L144:
       mov       rdi,[rdi+38]
       xor       ecx,ecx
       mov       [rsp+1F0],rcx
       mov       [rsp+1E0],r14
       mov       [rsp+1E8],r15
       cmp       qword ptr [rsp+1E0],0
       je        short M00_L145
       mov       rcx,[rsp+1E0]
       mov       rdx,0BA3C000448
       cmp       rcx,[rdx]
       je        short M00_L149
       mov       rcx,[rsp+1E0]
       mov       rdx,0BA3C000450
       cmp       rcx,[rdx]
       je        short M00_L149
       movsx     rcx,word ptr [rsp+1E8]
       jmp       short M00_L148
M00_L145:
       mov       rcx,[rsp+1E8]
       test      rcx,rcx
       je        short M00_L146
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L147
       mov       ecx,2
       jmp       short M00_L148
M00_L146:
       xor       ecx,ecx
       jmp       short M00_L148
M00_L147:
       mov       ecx,1
M00_L148:
       test      ecx,ecx
       jne       short M00_L149
       xor       ecx,ecx
       mov       [rsp+1F0],rcx
       jmp       near ptr M00_L155
M00_L149:
       cmp       qword ptr [rsp+1E0],0
       jne       short M00_L153
       cmp       qword ptr [rsp+1E8],2
       jbe       short M00_L153
       mov       rcx,[rsp+1E8]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L150
       vmovq     xmm0,rcx
       jmp       short M00_L152
M00_L150:
       cmp       rdx,2
       jbe       short M00_L151
       vmovsd    xmm0,qword ptr [7FF9778B2470]
       jmp       short M00_L152
M00_L151:
       vmovq     xmm0,rdx
M00_L152:
       vmovsd    qword ptr [rsp+1F0],xmm0
       mov       r14d,1
       jmp       short M00_L154
M00_L153:
       lea       rcx,[rsp+1E0]
       lea       rdx,[rsp+1F0]
       call      qword ptr [7FF977C2D4D0]
       mov       r14d,eax
M00_L154:
       test      r14d,r14d
       jne       short M00_L155
       vmovsd    xmm0,qword ptr [7FF9778B2470]
       jmp       short M00_L156
M00_L155:
       vmovsd    xmm0,qword ptr [rsp+1F0]
M00_L156:
       cmp       dword ptr [rdi+8],7
       jbe       near ptr M00_L440
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       edx,7FFFFFFF
       vcvttsd2si ecx,xmm1
       vucomisd  xmm0,qword ptr [7FF9778B2478]
       cmovb     edx,ecx
       mov       [rdi+1E],dx
       jmp       near ptr M00_L30
M00_L157:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],7
       jg        short M00_L158
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,7
       call      qword ptr [7FF977C2D4B8]
M00_L158:
       mov       rdi,[rdi+38]
       xor       ecx,ecx
       mov       [rsp+1D8],rcx
       mov       [rsp+1C8],r14
       mov       [rsp+1D0],r15
       cmp       qword ptr [rsp+1C8],0
       je        short M00_L159
       mov       rcx,[rsp+1C8]
       mov       rdx,0BA3C000448
       cmp       rcx,[rdx]
       je        short M00_L163
       mov       rcx,[rsp+1C8]
       mov       rdx,0BA3C000450
       cmp       rcx,[rdx]
       je        short M00_L163
       movsx     rcx,word ptr [rsp+1D0]
       jmp       short M00_L162
M00_L159:
       mov       rcx,[rsp+1D0]
       test      rcx,rcx
       je        short M00_L160
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L161
       mov       ecx,2
       jmp       short M00_L162
M00_L160:
       xor       ecx,ecx
       jmp       short M00_L162
M00_L161:
       mov       ecx,1
M00_L162:
       test      ecx,ecx
       jne       short M00_L163
       xor       ecx,ecx
       mov       [rsp+1D8],rcx
       jmp       near ptr M00_L169
M00_L163:
       cmp       qword ptr [rsp+1C8],0
       jne       short M00_L167
       cmp       qword ptr [rsp+1D0],2
       jbe       short M00_L167
       mov       rcx,[rsp+1D0]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L164
       vmovq     xmm0,rcx
       jmp       short M00_L166
M00_L164:
       cmp       rdx,2
       jbe       short M00_L165
       vmovsd    xmm0,qword ptr [7FF9778B2470]
       jmp       short M00_L166
M00_L165:
       vmovq     xmm0,rdx
M00_L166:
       vmovsd    qword ptr [rsp+1D8],xmm0
       mov       r14d,1
       jmp       short M00_L168
M00_L167:
       lea       rcx,[rsp+1C8]
       lea       rdx,[rsp+1D8]
       call      qword ptr [7FF977C2D4D0]
       mov       r14d,eax
M00_L168:
       test      r14d,r14d
       jne       short M00_L169
       vmovsd    xmm0,qword ptr [7FF9778B2470]
       jmp       short M00_L170
M00_L169:
       vmovsd    xmm0,qword ptr [rsp+1D8]
M00_L170:
       cmp       dword ptr [rdi+8],7
       jbe       near ptr M00_L440
       vxorps    xmm1,xmm1,xmm1
       vmaxsd    xmm1,xmm0,xmm1
       mov       edx,0FFFFFFFF
       vcvttsd2si rcx,xmm1
       vucomisd  xmm0,qword ptr [7FF9778B2498]
       cmovb     edx,ecx
       mov       [rdi+2C],edx
       jmp       near ptr M00_L30
M00_L171:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],7
       jg        short M00_L172
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,7
       call      qword ptr [7FF977C2D4B8]
M00_L172:
       mov       rdi,[rdi+38]
       mov       r8,r14
       mov       r14,r15
       test      r8,r8
       je        short M00_L175
       mov       rcx,0BA3C000448
       cmp       r8,[rcx]
       jne       short M00_L173
       mov       ecx,1000
       jmp       short M00_L178
M00_L173:
       mov       rcx,0BA3C000450
       cmp       r8,[rcx]
       jne       short M00_L174
       mov       ecx,2000
       jmp       short M00_L178
M00_L174:
       movsx     rcx,r14w
       jmp       short M00_L178
M00_L175:
       test      r14,r14
       je        short M00_L176
       lea       rcx,[r14-1]
       cmp       rcx,1
       jbe       short M00_L177
       mov       ecx,2
       jmp       short M00_L178
M00_L176:
       xor       ecx,ecx
       jmp       short M00_L178
M00_L177:
       mov       ecx,1
M00_L178:
       cmp       ecx,2
       je        short M00_L180
       cmp       ecx,1000
       je        short M00_L179
       cmp       ecx,2000
       jne       near ptr M00_L185
       test      r14,r14
       jl        near ptr M00_L185
M00_L179:
       jmp       near ptr M00_L190
M00_L180:
       mov       rcx,8007FFFFFFFFFFFF
       add       rcx,r14
       cmp       rcx,3
       jbe       short M00_L181
       vmovq     xmm0,r14
       jmp       short M00_L183
M00_L181:
       cmp       rcx,2
       jbe       short M00_L182
       vmovsd    xmm0,qword ptr [7FF9778B2470]
       jmp       short M00_L183
M00_L182:
       vmovq     xmm0,rcx
M00_L183:
       vucomisd  xmm0,qword ptr [7FF9778B24A0]
       jb        short M00_L185
       vmovsd    xmm1,qword ptr [7FF9778B24A8]
       vucomisd  xmm1,xmm0
       jbe       short M00_L185
       vroundsd  xmm1,xmm1,xmm0,0B
       vucomisd  xmm1,xmm0
       jp        short M00_L185
       jne       short M00_L185
       vxorps    xmm1,xmm1,xmm1
       vucomisd  xmm0,xmm1
       jp        short M00_L184
       jne       short M00_L184
       vmovq     rcx,xmm0
       not       rcx
       shr       rcx,3F
       test      ecx,ecx
       je        short M00_L185
M00_L184:
       mov       r8,r14
       mov       rcx,8007FFFFFFFFFFFF
       add       rcx,r8
       cmp       rcx,3
       jbe       short M00_L187
       jmp       short M00_L186
M00_L185:
       mov       [rsp+20],r8
       mov       [rsp+28],r14
       lea       r8,[rsp+20]
       lea       rcx,[rsp+1B8]
       mov       edx,13
       call      qword ptr [7FF977C2D4E8]
       xor       r14d,r14d
       jmp       short M00_L190
M00_L186:
       vmovq     xmm0,r8
       jmp       short M00_L189
M00_L187:
       cmp       rcx,2
       jbe       short M00_L188
       vmovsd    xmm0,qword ptr [7FF9778B2470]
       jmp       short M00_L189
M00_L188:
       vmovq     xmm0,rcx
M00_L189:
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       r8,7FFFFFFFFFFFFFFF
       vcvttsd2si rcx,xmm1
       vucomisd  xmm0,qword ptr [7FF9778B24A8]
       cmovb     r8,rcx
       mov       r14,r8
M00_L190:
       cmp       dword ptr [rdi+8],7
       jbe       near ptr M00_L440
       mov       [rdi+48],r14
       jmp       near ptr M00_L30
M00_L191:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],7
       jg        short M00_L192
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,7
       call      qword ptr [7FF977C2D4B8]
M00_L192:
       mov       rdi,[rdi+38]
       test      r14,r14
       je        short M00_L195
       mov       r8,0BA3C000448
       cmp       r14,[r8]
       jne       short M00_L193
       mov       r8d,1000
       jmp       short M00_L198
M00_L193:
       mov       r8,0BA3C000450
       cmp       r14,[r8]
       jne       short M00_L194
       mov       r8d,2000
       jmp       short M00_L198
M00_L194:
       movsx     r8,r15w
       jmp       short M00_L198
M00_L195:
       test      r15,r15
       je        short M00_L196
       lea       r8,[r15-1]
       cmp       r8,1
       jbe       short M00_L197
       mov       r8d,2
       jmp       short M00_L198
M00_L196:
       xor       r8d,r8d
       jmp       short M00_L198
M00_L197:
       mov       r8d,1
M00_L198:
       cmp       r8d,2
       je        short M00_L201
       cmp       r8d,1000
       je        short M00_L199
       cmp       r8d,2000
       jne       near ptr M00_L206
       jmp       short M00_L200
M00_L199:
       test      r15,r15
       jl        near ptr M00_L206
M00_L200:
       jmp       near ptr M00_L211
M00_L201:
       mov       r8,8007FFFFFFFFFFFF
       add       r8,r15
       cmp       r8,3
       jbe       short M00_L202
       vmovq     xmm0,r15
       jmp       short M00_L204
M00_L202:
       cmp       r8,2
       jbe       short M00_L203
       vmovsd    xmm0,qword ptr [7FF9778B2470]
       jmp       short M00_L204
M00_L203:
       vmovq     xmm0,r8
M00_L204:
       vxorps    xmm1,xmm1,xmm1
       vucomisd  xmm0,xmm1
       jb        short M00_L206
       vmovsd    xmm1,qword ptr [7FF9778B24B0]
       vucomisd  xmm1,xmm0
       jbe       short M00_L206
       vroundsd  xmm1,xmm1,xmm0,0B
       vucomisd  xmm1,xmm0
       jp        short M00_L206
       jne       short M00_L206
       vxorps    xmm1,xmm1,xmm1
       vucomisd  xmm0,xmm1
       jp        short M00_L205
       jne       short M00_L205
       vmovq     r8,xmm0
       not       r8
       shr       r8,3F
       test      r8d,r8d
       je        short M00_L206
M00_L205:
       mov       r8,r15
       mov       rcx,8007FFFFFFFFFFFF
       add       rcx,r8
       cmp       rcx,3
       jbe       short M00_L208
       jmp       short M00_L207
M00_L206:
       mov       [rsp+20],r14
       mov       [rsp+28],r15
       lea       r8,[rsp+20]
       lea       rcx,[rsp+1A8]
       mov       edx,14
       call      qword ptr [7FF977C2D4E8]
       xor       r15d,r15d
       jmp       short M00_L211
M00_L207:
       vmovq     xmm0,r8
       jmp       short M00_L210
M00_L208:
       cmp       rcx,2
       jbe       short M00_L209
       vmovsd    xmm0,qword ptr [7FF9778B2470]
       jmp       short M00_L210
M00_L209:
       vmovq     xmm0,rcx
M00_L210:
       vxorps    xmm1,xmm1,xmm1
       vmaxsd    xmm1,xmm0,xmm1
       vmovsd    xmm2,qword ptr [7FF9778B24B0]
       vsubsd    xmm3,xmm0,xmm2
       vcvttsd2si r8,xmm1
       vcvttsd2si rcx,xmm3
       mov       rdx,r8
       sar       rdx,3F
       and       rcx,rdx
       mov       rdx,0FFFFFFFFFFFFFFFF
       or        r8,rcx
       vucomisd  xmm0,xmm2
       cmovb     rdx,r8
       mov       r15,rdx
M00_L211:
       cmp       dword ptr [rdi+8],7
       jbe       near ptr M00_L440
       mov       [rdi+48],r15
       jmp       near ptr M00_L30
M00_L212:
       mov       rcx,r13
       mov       rax,[r13]
       mov       rax,[rax+58]
       call      qword ptr [rax+8]
       mov       edx,eax
       mov       ecx,7
       call      qword ptr [7FF977C2D4B8]
M00_L213:
       mov       [rsp+20],r14
       mov       [rsp+28],r15
       lea       r8,[rsp+20]
       mov       rcx,r13
       mov       edx,7
       mov       rax,[r13]
       mov       rax,[rax+58]
       call      qword ptr [rax+18]
       jmp       near ptr M00_L30
M00_L214:
       mov       r13d,1000
       jmp       near ptr M00_L05
M00_L215:
       mov       r13d,2000
       jmp       near ptr M00_L05
M00_L216:
       xor       r13d,r13d
       jmp       near ptr M00_L05
M00_L217:
       mov       r13d,1
       jmp       near ptr M00_L05
M00_L218:
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       jmp       near ptr M00_L06
M00_L219:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BooleanValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0BA3C001520
       mov       rdi,[rcx]
       jmp       short M00_L221
M00_L220:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BooleanValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0BA3C001518
       mov       rdi,[rcx]
M00_L221:
       jmp       near ptr M00_L07
M00_L222:
       vmovq     xmm1,rcx
       jmp       short M00_L225
M00_L223:
       cmp       rax,2
       jbe       short M00_L224
       vmovsd    xmm1,qword ptr [7FF9778B2470]
       jmp       short M00_L225
M00_L224:
       vmovq     xmm1,rax
M00_L225:
       vucomisd  xmm1,xmm1
       jp        short M00_L226
       je        short M00_L227
M00_L226:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0BA3C001508
       mov       rdi,[rcx]
       jmp       near ptr M00_L239
M00_L227:
       vxorps    xmm0,xmm0,xmm0
       vucomisd  xmm1,xmm0
       jp        short M00_L228
       jne       short M00_L228
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0BA3C0014B8
       mov       rdi,[rcx]
       jmp       near ptr M00_L239
M00_L228:
       vucomisd  xmm1,qword ptr [7FF9778B24B8]
       jp        short M00_L229
       jne       short M00_L229
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0BA3C0014C0
       mov       rdi,[rcx]
       jmp       near ptr M00_L239
M00_L229:
       vucomisd  xmm1,qword ptr [7FF9778B24C0]
       jp        short M00_L230
       jne       short M00_L230
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0BA3C0014C8
       mov       rdi,[rcx]
       jmp       near ptr M00_L239
M00_L230:
       vucomisd  xmm1,qword ptr [7FF9778B24C8]
       jp        short M00_L231
       jne       short M00_L231
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0BA3C0014D0
       mov       rdi,[rcx]
       jmp       near ptr M00_L239
M00_L231:
       vucomisd  xmm1,qword ptr [7FF9778B24D0]
       jp        short M00_L232
       jne       short M00_L232
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0BA3C0014D8
       mov       rdi,[rcx]
       jmp       near ptr M00_L239
M00_L232:
       vucomisd  xmm1,qword ptr [7FF9778B24D8]
       jp        short M00_L233
       jne       short M00_L233
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0BA3C0014E0
       mov       rdi,[rcx]
       jmp       near ptr M00_L239
M00_L233:
       vucomisd  xmm1,qword ptr [7FF9778B24E0]
       jp        short M00_L234
       jne       short M00_L234
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0BA3C0014E8
       mov       rdi,[rcx]
       jmp       near ptr M00_L239
M00_L234:
       vucomisd  xmm1,qword ptr [7FF9778B24E8]
       jp        short M00_L235
       jne       short M00_L235
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0BA3C0014F0
       mov       rdi,[rcx]
       jmp       near ptr M00_L239
M00_L235:
       vucomisd  xmm1,qword ptr [7FF9778B24F0]
       jp        short M00_L236
       jne       short M00_L236
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0BA3C0014F8
       mov       rdi,[rcx]
       jmp       near ptr M00_L239
M00_L236:
       vucomisd  xmm1,qword ptr [7FF9778B24F8]
       jp        short M00_L237
       jne       short M00_L237
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0BA3C001500
       mov       rdi,[rcx]
       jmp       short M00_L239
M00_L237:
       vmovsd    qword ptr [rsp+178],xmm1
       vucomisd  xmm1,qword ptr [7FF9778B2500]
       jp        short M00_L238
       jne       short M00_L238
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0BA3C0014B0
       mov       rdi,[rcx]
       jmp       short M00_L239
M00_L238:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      CORINFO_HELP_NEWSFAST
       mov       rdi,rax
       mov       rcx,rdi
       vmovsd    xmm1,qword ptr [rsp+178]
       call      qword ptr [7FF977C2D500]
M00_L239:
       jmp       near ptr M00_L07
M00_L240:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.Int64Value
       call      CORINFO_HELP_NEWSFAST
       mov       rdi,rax
       mov       rcx,rdi
       mov       rdx,[rsp+188]
       call      qword ptr [7FF977C25E18]
       jmp       near ptr M00_L07
M00_L241:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.UInt64Value
       call      CORINFO_HELP_NEWSFAST
       mov       rdi,rax
       mov       rcx,rdi
       mov       rdx,[rsp+188]
       call      qword ptr [7FF977C25E48]
       jmp       near ptr M00_L07
M00_L242:
       mov       rdx,[rsp+180]
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       short M00_L243
       xor       edi,edi
       jmp       near ptr M00_L07
M00_L243:
       cmp       dword ptr [rbp+8],0
       jne       short M00_L244
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0BA3C001490
       mov       rdi,[rcx]
       jmp       short M00_L245
M00_L244:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      CORINFO_HELP_NEWSFAST
       mov       rdi,rax
       mov       rcx,rdi
       mov       rdx,rbp
       call      qword ptr [7FF977C2CA38]
M00_L245:
       jmp       near ptr M00_L07
M00_L246:
       mov       rdx,rdi
       mov       rcx,offset MT_AuroraScript.Runtime.Types.IAuroraNativeIndexer
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfInterface(Void*, System.Object)
       mov       rbp,rax
       jmp       near ptr M00_L08
M00_L247:
       cmp       qword ptr [rsp+198],0
       jne       short M00_L251
       cmp       qword ptr [rsp+1A0],2
       jbe       short M00_L251
       mov       rcx,[rsp+1A0]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L248
       vmovq     xmm0,rcx
       jmp       short M00_L250
M00_L248:
       cmp       rdx,2
       jbe       short M00_L249
       vmovsd    xmm0,qword ptr [7FF9778B2470]
       jmp       short M00_L250
M00_L249:
       vmovq     xmm0,rdx
M00_L250:
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       rcx,7FFFFFFFFFFFFFFF
       vcvttsd2si rdx,xmm1
       vucomisd  xmm0,qword ptr [7FF9778B24A8]
       cmovb     rcx,rdx
       mov       [rsp+190],rcx
       jmp       short M00_L252
M00_L251:
       lea       rcx,[rsp+198]
       lea       rdx,[rsp+190]
       call      qword ptr [7FF977C2D518]
       test      eax,eax
       je        near ptr M00_L09
M00_L252:
       mov       edx,[rsp+190]
       mov       [rsp+20],r14
       mov       [rsp+28],r15
       lea       r8,[rsp+20]
       mov       rcx,rbp
       mov       r11,7FF977770508
       call      qword ptr [r11]
       jmp       near ptr M00_L30
M00_L253:
       mov       rdx,rdi
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptPackedArray
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       jmp       near ptr M00_L10
M00_L254:
       cmp       qword ptr [rsp+198],0
       jne       short M00_L258
       cmp       qword ptr [rsp+1A0],2
       jbe       short M00_L258
       mov       rcx,[rsp+1A0]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L255
       vmovq     xmm0,rcx
       jmp       short M00_L257
M00_L255:
       cmp       rdx,2
       jbe       short M00_L256
       vmovsd    xmm0,qword ptr [7FF9778B2470]
       jmp       short M00_L257
M00_L256:
       vmovq     xmm0,rdx
M00_L257:
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       rcx,7FFFFFFFFFFFFFFF
       vcvttsd2si rdx,xmm1
       vucomisd  xmm0,qword ptr [7FF9778B24A8]
       cmovb     rcx,rdx
       mov       [rsp+190],rcx
       jmp       short M00_L259
M00_L258:
       lea       rcx,[rsp+198]
       lea       rdx,[rsp+190]
       call      qword ptr [7FF977C2D518]
       test      eax,eax
       je        near ptr M00_L11
M00_L259:
       mov       edi,[rsp+190]
       mov       rdx,rbp
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt32Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r13,rax
       test      r13,r13
       jne       near ptr M00_L260
       mov       rdx,rbp
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt8Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r13,rax
       test      r13,r13
       jne       near ptr M00_L274
       mov       rdx,rbp
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptFloat32Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r13,rax
       test      r13,r13
       jne       near ptr M00_L288
       mov       rdx,rbp
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptFloat64Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r13,rax
       test      r13,r13
       jne       near ptr M00_L302
       mov       rdx,rbp
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptBooleanArray
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r13,rax
       test      r13,r13
       jne       near ptr M00_L316
       mov       rdx,rbp
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptUInt8Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r13,rax
       test      r13,r13
       jne       near ptr M00_L330
       mov       rdx,rbp
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt16Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r13,rax
       test      r13,r13
       jne       near ptr M00_L344
       mov       rdx,rbp
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptUInt16Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r13,rax
       test      r13,r13
       jne       near ptr M00_L358
       mov       rdx,rbp
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptUInt32Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r13,rax
       test      r13,r13
       jne       near ptr M00_L372
       mov       rdx,rbp
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt64Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r13,rax
       test      r13,r13
       jne       near ptr M00_L386
       mov       rdx,rbp
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptUInt64Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r13,rax
       test      r13,r13
       jne       near ptr M00_L406
       mov       rcx,rbp
       mov       rax,[rbp]
       mov       rax,[rax+58]
       call      qword ptr [rax+8]
       cmp       eax,edi
       ja        near ptr M00_L428
       jmp       near ptr M00_L427
M00_L260:
       mov       rdx,[r13+38]
       cmp       [rdx+8],edi
       ja        short M00_L261
       mov       rdx,[r13+38]
       mov       edx,[rdx+8]
       mov       ecx,edi
       call      qword ptr [7FF977C2D4B8]
M00_L261:
       mov       rbp,[r13+38]
       xor       ecx,ecx
       mov       [rsp+170],rcx
       mov       [rsp+160],r14
       mov       [rsp+168],r15
       cmp       qword ptr [rsp+160],0
       je        short M00_L262
       mov       rcx,[rsp+160]
       mov       rdx,0BA3C000448
       cmp       rcx,[rdx]
       je        short M00_L266
       mov       rcx,[rsp+160]
       mov       rdx,0BA3C000450
       cmp       rcx,[rdx]
       je        short M00_L266
       movsx     rcx,word ptr [rsp+168]
       jmp       short M00_L265
M00_L262:
       mov       rcx,[rsp+168]
       test      rcx,rcx
       je        short M00_L263
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L264
       mov       ecx,2
       jmp       short M00_L265
M00_L263:
       xor       ecx,ecx
       jmp       short M00_L265
M00_L264:
       mov       ecx,1
M00_L265:
       test      ecx,ecx
       jne       short M00_L266
       xor       ecx,ecx
       mov       [rsp+170],rcx
       jmp       near ptr M00_L272
M00_L266:
       cmp       qword ptr [rsp+160],0
       jne       short M00_L270
       cmp       qword ptr [rsp+168],2
       jbe       short M00_L270
       mov       rcx,[rsp+168]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L267
       vmovq     xmm0,rcx
       jmp       short M00_L269
M00_L267:
       cmp       rdx,2
       jbe       short M00_L268
       vmovsd    xmm0,qword ptr [7FF9778B2470]
       jmp       short M00_L269
M00_L268:
       vmovq     xmm0,rdx
M00_L269:
       vmovsd    qword ptr [rsp+170],xmm0
       mov       r14d,1
       jmp       short M00_L271
M00_L270:
       lea       rcx,[rsp+160]
       lea       rdx,[rsp+170]
       call      qword ptr [7FF977C2D4D0]
       mov       r14d,eax
M00_L271:
       test      r14d,r14d
       jne       short M00_L272
       vmovsd    xmm0,qword ptr [7FF9778B2470]
       jmp       short M00_L273
M00_L272:
       vmovsd    xmm0,qword ptr [rsp+170]
M00_L273:
       cmp       edi,[rbp+8]
       jae       near ptr M00_L440
       mov       ecx,edi
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       eax,7FFFFFFF
       vcvttsd2si edx,xmm1
       vucomisd  xmm0,qword ptr [7FF9778B2478]
       cmovb     eax,edx
       mov       [rbp+rcx*4+10],eax
       jmp       near ptr M00_L30
M00_L274:
       mov       rdx,[r13+38]
       cmp       [rdx+8],edi
       ja        short M00_L275
       mov       rdx,[r13+38]
       mov       edx,[rdx+8]
       mov       ecx,edi
       call      qword ptr [7FF977C2D4B8]
M00_L275:
       mov       rbp,[r13+38]
       xor       ecx,ecx
       mov       [rsp+158],rcx
       mov       [rsp+148],r14
       mov       [rsp+150],r15
       cmp       qword ptr [rsp+148],0
       je        short M00_L276
       mov       rcx,[rsp+148]
       mov       rdx,0BA3C000448
       cmp       rcx,[rdx]
       je        short M00_L280
       mov       rcx,[rsp+148]
       mov       rdx,0BA3C000450
       cmp       rcx,[rdx]
       je        short M00_L280
       movsx     rcx,word ptr [rsp+150]
       jmp       short M00_L279
M00_L276:
       mov       rcx,[rsp+150]
       test      rcx,rcx
       je        short M00_L277
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L278
       mov       ecx,2
       jmp       short M00_L279
M00_L277:
       xor       ecx,ecx
       jmp       short M00_L279
M00_L278:
       mov       ecx,1
M00_L279:
       test      ecx,ecx
       jne       short M00_L280
       xor       ecx,ecx
       mov       [rsp+158],rcx
       jmp       near ptr M00_L286
M00_L280:
       cmp       qword ptr [rsp+148],0
       jne       short M00_L284
       cmp       qword ptr [rsp+150],2
       jbe       short M00_L284
       mov       rcx,[rsp+150]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L281
       vmovq     xmm0,rcx
       jmp       short M00_L283
M00_L281:
       cmp       rdx,2
       jbe       short M00_L282
       vmovsd    xmm0,qword ptr [7FF9778B2470]
       jmp       short M00_L283
M00_L282:
       vmovq     xmm0,rdx
M00_L283:
       vmovsd    qword ptr [rsp+158],xmm0
       mov       r14d,1
       jmp       short M00_L285
M00_L284:
       lea       rcx,[rsp+148]
       lea       rdx,[rsp+158]
       call      qword ptr [7FF977C2D4D0]
       mov       r14d,eax
M00_L285:
       test      r14d,r14d
       jne       short M00_L286
       vmovsd    xmm0,qword ptr [7FF9778B2470]
       jmp       short M00_L287
M00_L286:
       vmovsd    xmm0,qword ptr [rsp+158]
M00_L287:
       cmp       edi,[rbp+8]
       jae       near ptr M00_L440
       mov       edx,edi
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       ecx,7FFFFFFF
       vcvttsd2si eax,xmm1
       vucomisd  xmm0,qword ptr [7FF9778B2478]
       cmovb     ecx,eax
       mov       [rbp+rdx+10],cl
       jmp       near ptr M00_L30
M00_L288:
       mov       rdx,[r13+38]
       cmp       [rdx+8],edi
       ja        short M00_L289
       mov       rdx,[r13+38]
       mov       edx,[rdx+8]
       mov       ecx,edi
       call      qword ptr [7FF977C2D4B8]
M00_L289:
       mov       rbp,[r13+38]
       xor       ecx,ecx
       mov       [rsp+140],rcx
       mov       [rsp+130],r14
       mov       [rsp+138],r15
       cmp       qword ptr [rsp+130],0
       je        short M00_L290
       mov       rcx,[rsp+130]
       mov       rdx,0BA3C000448
       cmp       rcx,[rdx]
       je        short M00_L294
       mov       rcx,[rsp+130]
       mov       rdx,0BA3C000450
       cmp       rcx,[rdx]
       je        short M00_L294
       movsx     rcx,word ptr [rsp+138]
       jmp       short M00_L293
M00_L290:
       mov       rcx,[rsp+138]
       test      rcx,rcx
       je        short M00_L291
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L292
       mov       ecx,2
       jmp       short M00_L293
M00_L291:
       xor       ecx,ecx
       jmp       short M00_L293
M00_L292:
       mov       ecx,1
M00_L293:
       test      ecx,ecx
       jne       short M00_L294
       xor       ecx,ecx
       mov       [rsp+140],rcx
       jmp       near ptr M00_L300
M00_L294:
       cmp       qword ptr [rsp+130],0
       jne       short M00_L298
       cmp       qword ptr [rsp+138],2
       jbe       short M00_L298
       mov       rcx,[rsp+138]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L295
       vmovq     xmm0,rcx
       jmp       short M00_L297
M00_L295:
       cmp       rdx,2
       jbe       short M00_L296
       vmovsd    xmm0,qword ptr [7FF9778B2470]
       jmp       short M00_L297
M00_L296:
       vmovq     xmm0,rdx
M00_L297:
       vmovsd    qword ptr [rsp+140],xmm0
       mov       r14d,1
       jmp       short M00_L299
M00_L298:
       lea       rcx,[rsp+130]
       lea       rdx,[rsp+140]
       call      qword ptr [7FF977C2D4D0]
       mov       r14d,eax
M00_L299:
       test      r14d,r14d
       jne       short M00_L300
       vmovsd    xmm0,qword ptr [7FF9778B2470]
       jmp       short M00_L301
M00_L300:
       vmovsd    xmm0,qword ptr [rsp+140]
M00_L301:
       cmp       edi,[rbp+8]
       jae       near ptr M00_L440
       mov       edx,edi
       vcvtsd2ss xmm0,xmm0,xmm0
       vmovss    dword ptr [rbp+rdx*4+10],xmm0
       jmp       near ptr M00_L30
M00_L302:
       mov       rdx,[r13+38]
       cmp       [rdx+8],edi
       ja        short M00_L303
       mov       rdx,[r13+38]
       mov       edx,[rdx+8]
       mov       ecx,edi
       call      qword ptr [7FF977C2D4B8]
M00_L303:
       mov       rbp,[r13+38]
       xor       ecx,ecx
       mov       [rsp+128],rcx
       mov       [rsp+118],r14
       mov       [rsp+120],r15
       cmp       qword ptr [rsp+118],0
       je        short M00_L304
       mov       rcx,[rsp+118]
       mov       rdx,0BA3C000448
       cmp       rcx,[rdx]
       je        short M00_L308
       mov       rcx,[rsp+118]
       mov       rdx,0BA3C000450
       cmp       rcx,[rdx]
       je        short M00_L308
       movsx     rcx,word ptr [rsp+120]
       jmp       short M00_L307
M00_L304:
       mov       rcx,[rsp+120]
       test      rcx,rcx
       je        short M00_L305
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L306
       mov       ecx,2
       jmp       short M00_L307
M00_L305:
       xor       ecx,ecx
       jmp       short M00_L307
M00_L306:
       mov       ecx,1
M00_L307:
       test      ecx,ecx
       jne       short M00_L308
       xor       ecx,ecx
       mov       [rsp+128],rcx
       jmp       near ptr M00_L314
M00_L308:
       cmp       qword ptr [rsp+118],0
       jne       short M00_L312
       cmp       qword ptr [rsp+120],2
       jbe       short M00_L312
       mov       rcx,[rsp+120]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L309
       vmovq     xmm0,rcx
       jmp       short M00_L311
M00_L309:
       cmp       rdx,2
       jbe       short M00_L310
       vmovsd    xmm0,qword ptr [7FF9778B2470]
       jmp       short M00_L311
M00_L310:
       vmovq     xmm0,rdx
M00_L311:
       vmovsd    qword ptr [rsp+128],xmm0
       mov       r14d,1
       jmp       short M00_L313
M00_L312:
       lea       rcx,[rsp+118]
       lea       rdx,[rsp+128]
       call      qword ptr [7FF977C2D4D0]
       mov       r14d,eax
M00_L313:
       test      r14d,r14d
       jne       short M00_L314
       vmovsd    xmm0,qword ptr [7FF9778B2470]
       jmp       short M00_L315
M00_L314:
       vmovsd    xmm0,qword ptr [rsp+128]
M00_L315:
       cmp       edi,[rbp+8]
       jae       near ptr M00_L440
       mov       edx,edi
       vmovsd    qword ptr [rbp+rdx*8+10],xmm0
       jmp       near ptr M00_L30
M00_L316:
       mov       rdx,[r13+38]
       cmp       [rdx+8],edi
       ja        short M00_L317
       mov       rdx,[r13+38]
       mov       edx,[rdx+8]
       mov       ecx,edi
       call      qword ptr [7FF977C2D4B8]
M00_L317:
       mov       rbp,[r13+38]
       mov       [rsp+108],r14
       mov       [rsp+110],r15
       cmp       qword ptr [rsp+108],0
       je        short M00_L320
       mov       rdx,[rsp+108]
       mov       rcx,0BA3C000448
       cmp       rdx,[rcx]
       jne       short M00_L318
       mov       r14d,1000
       jmp       short M00_L323
M00_L318:
       mov       rdx,[rsp+108]
       mov       rcx,0BA3C000450
       cmp       rdx,[rcx]
       jne       short M00_L319
       mov       r14d,2000
       jmp       short M00_L323
M00_L319:
       movsx     r14,word ptr [rsp+110]
       jmp       short M00_L323
M00_L320:
       mov       rdx,[rsp+110]
       test      rdx,rdx
       je        short M00_L321
       dec       rdx
       cmp       rdx,1
       jbe       short M00_L322
       mov       r14d,2
       jmp       short M00_L323
M00_L321:
       xor       r14d,r14d
       jmp       short M00_L323
M00_L322:
       mov       r14d,1
M00_L323:
       cmp       r14d,4
       ja        short M00_L324
       mov       edx,r14d
       lea       rcx,[7FF9778B2508]
       mov       ecx,[rcx+rdx*4]
       lea       rax,[M00_L01]
       add       rcx,rax
       jmp       rcx
M00_L324:
       cmp       r14d,1000
       je        near ptr M00_L326
       cmp       r14d,2000
       je        near ptr M00_L326
       lea       rcx,[rsp+108]
       call      qword ptr [7FF977C25E78]; AuroraScript.Runtime.ScriptDatum.get_Object()
       mov       r15,rax
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rdx,0BA3C000408
       cmp       r15,[rdx]
       setne     r15b
       movzx     r15d,r15b
       jmp       near ptr M00_L329
       cmp       qword ptr [rsp+110],2
       sete      r15b
       movzx     r15d,r15b
       jmp       near ptr M00_L329
       mov       rcx,7FF8000000000001
       cmp       [rsp+110],rcx
       je        short M00_L325
       mov       rcx,8000000000000000
       cmp       [rsp+110],rcx
       je        short M00_L325
       mov       rcx,7FF8000000000004
       cmp       [rsp+110],rcx
       setne     r15b
       movzx     r15d,r15b
       jmp       short M00_L329
M00_L325:
       xor       r15d,r15d
       jmp       short M00_L329
M00_L326:
       cmp       qword ptr [rsp+110],0
       setne     r15b
       movzx     r15d,r15b
       jmp       short M00_L329
       mov       rdx,[rsp+108]
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rax,rax
       je        short M00_L327
       cmp       dword ptr [rax+8],0
       sete      cl
       movzx     ecx,cl
       jmp       short M00_L328
M00_L327:
       mov       ecx,1
M00_L328:
       test      ecx,ecx
       sete      r15b
       movzx     r15d,r15b
M00_L329:
       cmp       edi,[rbp+8]
       jae       near ptr M00_L440
       mov       edx,edi
       mov       [rbp+rdx+10],r15b
       jmp       near ptr M00_L30
M00_L330:
       mov       rdx,[r13+38]
       cmp       [rdx+8],edi
       ja        short M00_L331
       mov       rdx,[r13+38]
       mov       edx,[rdx+8]
       mov       ecx,edi
       call      qword ptr [7FF977C2D4B8]
M00_L331:
       mov       rbp,[r13+38]
       xor       ecx,ecx
       mov       [rsp+100],rcx
       mov       [rsp+0F0],r14
       mov       [rsp+0F8],r15
       cmp       qword ptr [rsp+0F0],0
       je        short M00_L332
       mov       rcx,[rsp+0F0]
       mov       rdx,0BA3C000448
       cmp       rcx,[rdx]
       je        short M00_L336
       mov       rcx,[rsp+0F0]
       mov       rdx,0BA3C000450
       cmp       rcx,[rdx]
       je        short M00_L336
       movsx     rcx,word ptr [rsp+0F8]
       jmp       short M00_L335
M00_L332:
       mov       rcx,[rsp+0F8]
       test      rcx,rcx
       je        short M00_L333
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L334
       mov       ecx,2
       jmp       short M00_L335
M00_L333:
       xor       ecx,ecx
       jmp       short M00_L335
M00_L334:
       mov       ecx,1
M00_L335:
       test      ecx,ecx
       jne       short M00_L336
       xor       ecx,ecx
       mov       [rsp+100],rcx
       jmp       near ptr M00_L342
M00_L336:
       cmp       qword ptr [rsp+0F0],0
       jne       short M00_L340
       cmp       qword ptr [rsp+0F8],2
       jbe       short M00_L340
       mov       rcx,[rsp+0F8]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L337
       vmovq     xmm0,rcx
       jmp       short M00_L339
M00_L337:
       cmp       rdx,2
       jbe       short M00_L338
       vmovsd    xmm0,qword ptr [7FF9778B2470]
       jmp       short M00_L339
M00_L338:
       vmovq     xmm0,rdx
M00_L339:
       vmovsd    qword ptr [rsp+100],xmm0
       mov       r14d,1
       jmp       short M00_L341
M00_L340:
       lea       rcx,[rsp+0F0]
       lea       rdx,[rsp+100]
       call      qword ptr [7FF977C2D4D0]
       mov       r14d,eax
M00_L341:
       test      r14d,r14d
       jne       short M00_L342
       vmovsd    xmm0,qword ptr [7FF9778B2470]
       jmp       short M00_L343
M00_L342:
       vmovsd    xmm0,qword ptr [rsp+100]
M00_L343:
       cmp       edi,[rbp+8]
       jae       near ptr M00_L440
       mov       edx,edi
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       ecx,7FFFFFFF
       vcvttsd2si eax,xmm1
       vucomisd  xmm0,qword ptr [7FF9778B2478]
       cmovb     ecx,eax
       mov       [rbp+rdx+10],cl
       jmp       near ptr M00_L30
M00_L344:
       mov       rdx,[r13+38]
       cmp       [rdx+8],edi
       ja        short M00_L345
       mov       rdx,[r13+38]
       mov       edx,[rdx+8]
       mov       ecx,edi
       call      qword ptr [7FF977C2D4B8]
M00_L345:
       mov       rbp,[r13+38]
       xor       ecx,ecx
       mov       [rsp+0E8],rcx
       mov       [rsp+0D8],r14
       mov       [rsp+0E0],r15
       cmp       qword ptr [rsp+0D8],0
       je        short M00_L346
       mov       rcx,[rsp+0D8]
       mov       rdx,0BA3C000448
       cmp       rcx,[rdx]
       je        short M00_L350
       mov       rcx,[rsp+0D8]
       mov       rdx,0BA3C000450
       cmp       rcx,[rdx]
       je        short M00_L350
       movsx     rcx,word ptr [rsp+0E0]
       jmp       short M00_L349
M00_L346:
       mov       rcx,[rsp+0E0]
       test      rcx,rcx
       je        short M00_L347
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L348
       mov       ecx,2
       jmp       short M00_L349
M00_L347:
       xor       ecx,ecx
       jmp       short M00_L349
M00_L348:
       mov       ecx,1
M00_L349:
       test      ecx,ecx
       jne       short M00_L350
       xor       ecx,ecx
       mov       [rsp+0E8],rcx
       jmp       near ptr M00_L356
M00_L350:
       cmp       qword ptr [rsp+0D8],0
       jne       short M00_L354
       cmp       qword ptr [rsp+0E0],2
       jbe       short M00_L354
       mov       rcx,[rsp+0E0]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L351
       vmovq     xmm0,rcx
       jmp       short M00_L353
M00_L351:
       cmp       rdx,2
       jbe       short M00_L352
       vmovsd    xmm0,qword ptr [7FF9778B2470]
       jmp       short M00_L353
M00_L352:
       vmovq     xmm0,rdx
M00_L353:
       vmovsd    qword ptr [rsp+0E8],xmm0
       mov       r14d,1
       jmp       short M00_L355
M00_L354:
       lea       rcx,[rsp+0D8]
       lea       rdx,[rsp+0E8]
       call      qword ptr [7FF977C2D4D0]
       mov       r14d,eax
M00_L355:
       test      r14d,r14d
       jne       short M00_L356
       vmovsd    xmm0,qword ptr [7FF9778B2470]
       jmp       short M00_L357
M00_L356:
       vmovsd    xmm0,qword ptr [rsp+0E8]
M00_L357:
       cmp       edi,[rbp+8]
       jae       near ptr M00_L440
       mov       edx,edi
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       ecx,7FFFFFFF
       vcvttsd2si eax,xmm1
       vucomisd  xmm0,qword ptr [7FF9778B2478]
       cmovb     ecx,eax
       mov       [rbp+rdx*2+10],cx
       jmp       near ptr M00_L30
M00_L358:
       mov       rdx,[r13+38]
       cmp       [rdx+8],edi
       ja        short M00_L359
       mov       rdx,[r13+38]
       mov       edx,[rdx+8]
       mov       ecx,edi
       call      qword ptr [7FF977C2D4B8]
M00_L359:
       mov       rbp,[r13+38]
       xor       ecx,ecx
       mov       [rsp+0D0],rcx
       mov       [rsp+0C0],r14
       mov       [rsp+0C8],r15
       cmp       qword ptr [rsp+0C0],0
       je        short M00_L360
       mov       rcx,[rsp+0C0]
       mov       rdx,0BA3C000448
       cmp       rcx,[rdx]
       je        short M00_L364
       mov       rcx,[rsp+0C0]
       mov       rdx,0BA3C000450
       cmp       rcx,[rdx]
       je        short M00_L364
       movsx     rcx,word ptr [rsp+0C8]
       jmp       short M00_L363
M00_L360:
       mov       rcx,[rsp+0C8]
       test      rcx,rcx
       je        short M00_L361
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L362
       mov       ecx,2
       jmp       short M00_L363
M00_L361:
       xor       ecx,ecx
       jmp       short M00_L363
M00_L362:
       mov       ecx,1
M00_L363:
       test      ecx,ecx
       jne       short M00_L364
       xor       ecx,ecx
       mov       [rsp+0D0],rcx
       jmp       near ptr M00_L370
M00_L364:
       cmp       qword ptr [rsp+0C0],0
       jne       short M00_L368
       cmp       qword ptr [rsp+0C8],2
       jbe       short M00_L368
       mov       rcx,[rsp+0C8]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L365
       vmovq     xmm0,rcx
       jmp       short M00_L367
M00_L365:
       cmp       rdx,2
       jbe       short M00_L366
       vmovsd    xmm0,qword ptr [7FF9778B2470]
       jmp       short M00_L367
M00_L366:
       vmovq     xmm0,rdx
M00_L367:
       vmovsd    qword ptr [rsp+0D0],xmm0
       mov       r14d,1
       jmp       short M00_L369
M00_L368:
       lea       rcx,[rsp+0C0]
       lea       rdx,[rsp+0D0]
       call      qword ptr [7FF977C2D4D0]
       mov       r14d,eax
M00_L369:
       test      r14d,r14d
       jne       short M00_L370
       vmovsd    xmm0,qword ptr [7FF9778B2470]
       jmp       short M00_L371
M00_L370:
       vmovsd    xmm0,qword ptr [rsp+0D0]
M00_L371:
       cmp       edi,[rbp+8]
       jae       near ptr M00_L440
       mov       edx,edi
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       ecx,7FFFFFFF
       vcvttsd2si eax,xmm1
       vucomisd  xmm0,qword ptr [7FF9778B2478]
       cmovb     ecx,eax
       mov       [rbp+rdx*2+10],cx
       jmp       near ptr M00_L30
M00_L372:
       mov       rdx,[r13+38]
       cmp       [rdx+8],edi
       ja        short M00_L373
       mov       rdx,[r13+38]
       mov       edx,[rdx+8]
       mov       ecx,edi
       call      qword ptr [7FF977C2D4B8]
M00_L373:
       mov       rbp,[r13+38]
       xor       ecx,ecx
       mov       [rsp+0B8],rcx
       mov       [rsp+0A8],r14
       mov       [rsp+0B0],r15
       cmp       qword ptr [rsp+0A8],0
       je        short M00_L374
       mov       rcx,[rsp+0A8]
       mov       rdx,0BA3C000448
       cmp       rcx,[rdx]
       je        short M00_L378
       mov       rcx,[rsp+0A8]
       mov       rdx,0BA3C000450
       cmp       rcx,[rdx]
       je        short M00_L378
       movsx     rcx,word ptr [rsp+0B0]
       jmp       short M00_L377
M00_L374:
       mov       rcx,[rsp+0B0]
       test      rcx,rcx
       je        short M00_L375
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L376
       mov       ecx,2
       jmp       short M00_L377
M00_L375:
       xor       ecx,ecx
       jmp       short M00_L377
M00_L376:
       mov       ecx,1
M00_L377:
       test      ecx,ecx
       jne       short M00_L378
       xor       ecx,ecx
       mov       [rsp+0B8],rcx
       jmp       near ptr M00_L384
M00_L378:
       cmp       qword ptr [rsp+0A8],0
       jne       short M00_L382
       cmp       qword ptr [rsp+0B0],2
       jbe       short M00_L382
       mov       rcx,[rsp+0B0]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L379
       vmovq     xmm0,rcx
       jmp       short M00_L381
M00_L379:
       cmp       rdx,2
       jbe       short M00_L380
       vmovsd    xmm0,qword ptr [7FF9778B2470]
       jmp       short M00_L381
M00_L380:
       vmovq     xmm0,rdx
M00_L381:
       vmovsd    qword ptr [rsp+0B8],xmm0
       mov       r14d,1
       jmp       short M00_L383
M00_L382:
       lea       rcx,[rsp+0A8]
       lea       rdx,[rsp+0B8]
       call      qword ptr [7FF977C2D4D0]
       mov       r14d,eax
M00_L383:
       test      r14d,r14d
       jne       short M00_L384
       vmovsd    xmm0,qword ptr [7FF9778B2470]
       jmp       short M00_L385
M00_L384:
       vmovsd    xmm0,qword ptr [rsp+0B8]
M00_L385:
       cmp       edi,[rbp+8]
       jae       near ptr M00_L440
       mov       edx,edi
       vxorps    xmm1,xmm1,xmm1
       vmaxsd    xmm1,xmm0,xmm1
       mov       ecx,0FFFFFFFF
       vcvttsd2si rax,xmm1
       vucomisd  xmm0,qword ptr [7FF9778B2498]
       cmovb     ecx,eax
       mov       [rbp+rdx*4+10],ecx
       jmp       near ptr M00_L30
M00_L386:
       mov       rdx,[r13+38]
       cmp       [rdx+8],edi
       ja        short M00_L387
       mov       rdx,[r13+38]
       mov       edx,[rdx+8]
       mov       ecx,edi
       call      qword ptr [7FF977C2D4B8]
M00_L387:
       mov       rbp,[r13+38]
       test      r14,r14
       je        short M00_L390
       mov       r8,0BA3C000448
       cmp       r14,[r8]
       jne       short M00_L388
       mov       r8d,1000
       jmp       short M00_L393
M00_L388:
       mov       r8,0BA3C000450
       cmp       r14,[r8]
       jne       short M00_L389
       mov       r8d,2000
       jmp       short M00_L393
M00_L389:
       movsx     r8,r15w
       jmp       short M00_L393
M00_L390:
       test      r15,r15
       je        short M00_L391
       lea       r8,[r15-1]
       cmp       r8,1
       jbe       short M00_L392
       mov       r8d,2
       jmp       short M00_L393
M00_L391:
       xor       r8d,r8d
       jmp       short M00_L393
M00_L392:
       mov       r8d,1
M00_L393:
       cmp       r8d,2
       je        short M00_L395
       cmp       r8d,1000
       je        short M00_L394
       cmp       r8d,2000
       jne       near ptr M00_L400
       test      r15,r15
       jl        near ptr M00_L400
M00_L394:
       mov       rax,r15
       jmp       near ptr M00_L405
M00_L395:
       mov       rax,8007FFFFFFFFFFFF
       add       rax,r15
       cmp       rax,3
       jbe       short M00_L396
       vmovq     xmm0,r15
       jmp       short M00_L398
M00_L396:
       cmp       rax,2
       jbe       short M00_L397
       vmovsd    xmm0,qword ptr [7FF9778B2470]
       jmp       short M00_L398
M00_L397:
       vmovq     xmm0,rax
M00_L398:
       vucomisd  xmm0,qword ptr [7FF9778B24A0]
       jb        short M00_L400
       vmovsd    xmm1,qword ptr [7FF9778B24A8]
       vucomisd  xmm1,xmm0
       jbe       short M00_L400
       vroundsd  xmm1,xmm1,xmm0,0B
       vucomisd  xmm1,xmm0
       jp        short M00_L400
       jne       short M00_L400
       vxorps    xmm1,xmm1,xmm1
       vucomisd  xmm0,xmm1
       jp        short M00_L399
       jne       short M00_L399
       vmovq     r8,xmm0
       not       r8
       shr       r8,3F
       test      r8d,r8d
       je        short M00_L400
M00_L399:
       mov       r8,8007FFFFFFFFFFFF
       add       r8,r15
       cmp       r8,3
       jbe       short M00_L402
       jmp       short M00_L401
M00_L400:
       mov       [rsp+20],r14
       mov       [rsp+28],r15
       lea       r8,[rsp+20]
       lea       rcx,[rsp+98]
       mov       edx,13
       call      qword ptr [7FF977C2D4E8]
       xor       eax,eax
       jmp       short M00_L405
M00_L401:
       vmovq     xmm0,r15
       jmp       short M00_L404
M00_L402:
       cmp       r8,2
       jbe       short M00_L403
       vmovsd    xmm0,qword ptr [7FF9778B2470]
       jmp       short M00_L404
M00_L403:
       vmovq     xmm0,r8
M00_L404:
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       r8,7FFFFFFFFFFFFFFF
       vcvttsd2si rcx,xmm1
       vucomisd  xmm0,qword ptr [7FF9778B24A8]
       cmovb     r8,rcx
       mov       rax,r8
M00_L405:
       cmp       edi,[rbp+8]
       jae       near ptr M00_L440
       mov       edx,edi
       mov       [rbp+rdx*8+10],rax
       jmp       near ptr M00_L30
M00_L406:
       mov       rdx,[r13+38]
       cmp       [rdx+8],edi
       ja        short M00_L407
       mov       rdx,[r13+38]
       mov       edx,[rdx+8]
       mov       ecx,edi
       call      qword ptr [7FF977C2D4B8]
M00_L407:
       mov       rbp,[r13+38]
       test      r14,r14
       je        short M00_L410
       mov       r8,0BA3C000448
       cmp       r14,[r8]
       jne       short M00_L408
       mov       r8d,1000
       jmp       short M00_L413
M00_L408:
       mov       r8,0BA3C000450
       cmp       r14,[r8]
       jne       short M00_L409
       mov       r8d,2000
       jmp       short M00_L413
M00_L409:
       movsx     r8,r15w
       jmp       short M00_L413
M00_L410:
       test      r15,r15
       je        short M00_L411
       lea       r8,[r15-1]
       cmp       r8,1
       jbe       short M00_L412
       mov       r8d,2
       jmp       short M00_L413
M00_L411:
       xor       r8d,r8d
       jmp       short M00_L413
M00_L412:
       mov       r8d,1
M00_L413:
       cmp       r8d,2
       je        short M00_L416
       cmp       r8d,1000
       je        short M00_L414
       cmp       r8d,2000
       jne       near ptr M00_L421
       jmp       short M00_L415
M00_L414:
       test      r15,r15
       jl        near ptr M00_L421
M00_L415:
       jmp       near ptr M00_L426
M00_L416:
       mov       r8,8007FFFFFFFFFFFF
       add       r8,r15
       cmp       r8,3
       jbe       short M00_L417
       vmovq     xmm0,r15
       jmp       short M00_L419
M00_L417:
       cmp       r8,2
       jbe       short M00_L418
       vmovsd    xmm0,qword ptr [7FF9778B2470]
       jmp       short M00_L419
M00_L418:
       vmovq     xmm0,r8
M00_L419:
       vxorps    xmm1,xmm1,xmm1
       vucomisd  xmm0,xmm1
       jb        short M00_L421
       vmovsd    xmm1,qword ptr [7FF9778B24B0]
       vucomisd  xmm1,xmm0
       jbe       short M00_L421
       vroundsd  xmm1,xmm1,xmm0,0B
       vucomisd  xmm1,xmm0
       jp        short M00_L421
       jne       short M00_L421
       vxorps    xmm1,xmm1,xmm1
       vucomisd  xmm0,xmm1
       jp        short M00_L420
       jne       short M00_L420
       vmovq     r8,xmm0
       not       r8
       shr       r8,3F
       test      r8d,r8d
       je        short M00_L421
M00_L420:
       mov       r8,8007FFFFFFFFFFFF
       add       r8,r15
       cmp       r8,3
       jbe       short M00_L423
       jmp       short M00_L422
M00_L421:
       mov       [rsp+20],r14
       mov       [rsp+28],r15
       lea       r8,[rsp+20]
       lea       rcx,[rsp+88]
       mov       edx,14
       call      qword ptr [7FF977C2D4E8]
       xor       r15d,r15d
       jmp       short M00_L426
M00_L422:
       vmovq     xmm0,r15
       jmp       short M00_L425
M00_L423:
       cmp       r8,2
       jbe       short M00_L424
       vmovsd    xmm0,qword ptr [7FF9778B2470]
       jmp       short M00_L425
M00_L424:
       vmovq     xmm0,r8
M00_L425:
       vxorps    xmm1,xmm1,xmm1
       vmaxsd    xmm1,xmm0,xmm1
       vmovsd    xmm2,qword ptr [7FF9778B24B0]
       vsubsd    xmm3,xmm0,xmm2
       vcvttsd2si r8,xmm1
       vcvttsd2si rcx,xmm3
       mov       rdx,r8
       sar       rdx,3F
       and       rcx,rdx
       mov       rdx,0FFFFFFFFFFFFFFFF
       or        r8,rcx
       vucomisd  xmm0,xmm2
       cmovb     rdx,r8
       mov       r15,rdx
M00_L426:
       cmp       edi,[rbp+8]
       jae       near ptr M00_L440
       mov       edx,edi
       mov       [rbp+rdx*8+10],r15
       jmp       near ptr M00_L30
M00_L427:
       mov       rcx,rbp
       mov       rax,[rbp]
       mov       rax,[rax+58]
       call      qword ptr [rax+8]
       mov       edx,eax
       mov       ecx,edi
       call      qword ptr [7FF977C2D4B8]
M00_L428:
       mov       [rsp+20],r14
       mov       [rsp+28],r15
       lea       r8,[rsp+20]
       mov       edx,edi
       mov       rcx,rbp
       mov       rax,[rbp]
       mov       rax,[rax+58]
       call      qword ptr [rax+18]
       jmp       near ptr M00_L30
M00_L429:
       mov       rcx,[rdi+8]
       test      rcx,rcx
       je        short M00_L432
       jmp       short M00_L431
M00_L430:
       mov       rcx,[rdi+18]
       cmp       [rcx+8],r8d
       jle       near ptr M00_L15
       mov       rcx,[rdi+18]
       cmp       r8d,[rcx+8]
       jae       near ptr M00_L440
       mov       edx,r8d
       mov       rcx,[rcx+rdx*8+10]
       jmp       near ptr M00_L16
M00_L431:
       lea       r8,[rsp+70]
       mov       rdx,rbp
       call      qword ptr [7FF977C25FE0]; AuroraScript.Runtime.Types.ScriptObject.TryResolveProperty(System.String, AuroraScript.Runtime.Property.PropertyDescriptor ByRef)
       test      eax,eax
       je        near ptr M00_L19
       jmp       near ptr M00_L17
M00_L432:
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rsp+70],xmm0
       vmovdqu   xmmword ptr [rsp+78],xmm0
       jmp       near ptr M00_L19
M00_L433:
       mov       rdx,[rdx+10]
       test      rdx,rdx
       je        near ptr M00_L18
       mov       r9,[rsp+70]
       mov       rcx,[r9+10]
       cmp       [rcx],cl
       mov       [rsp+20],r14
       mov       [rsp+28],r15
       lea       r9,[rsp+20]
       lea       rdx,[rsp+60]
       xor       r8d,r8d
       call      qword ptr [7FF977C2CBA0]
       jmp       near ptr M00_L30
M00_L434:
       mov       rax,[rax+40]
       test      rax,rax
       je        near ptr M00_L19
       mov       [rsp+20],r14
       mov       [rsp+28],r15
       lea       r9,[rsp+20]
       mov       r8,rdi
       xor       edx,edx
       mov       rcx,[rax+8]
       call      qword ptr [rax+18]
       jmp       near ptr M00_L30
M00_L435:
       call      qword ptr [7FF9779FE8E0]
       jmp       near ptr M00_L24
M00_L436:
       xor       ecx,ecx
       mov       [rsp+38],rcx
       jmp       near ptr M00_L28
M00_L437:
       call      qword ptr [7FF9779FE910]
       jmp       near ptr M00_L28
M00_L438:
       mov       rcx,rdi
       call      qword ptr [7FF9779FE940]; AuroraScript.Runtime.Types.ScriptObject.Resize()
       jmp       near ptr M00_L29
M00_L439:
       mov       [rsp+20],r14
       mov       [rsp+28],r15
       lea       r9,[rsp+20]
       mov       rcx,rdi
       mov       r8,rbp
       xor       edx,edx
       mov       rax,[rdi]
       mov       rax,[rax+50]
       call      qword ptr [rax+20]
       jmp       near ptr M00_L30
M00_L440:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 12696
```
```assembly
; AuroraScript.Runtime.ScriptDatum.ToString(AuroraScript.Runtime.ScriptDatum)
       sub       rsp,28
M01_L00:
       mov       rdx,[rcx]
       test      rdx,rdx
       jne       near ptr M01_L05
       mov       rax,[rcx+8]
       test      rax,rax
       je        near ptr M01_L11
       dec       rax
       mov       r8d,1
       mov       r10d,2
       cmp       rax,1
       cmova     r8d,r10d
M01_L01:
       cmp       r8d,2
       jne       near ptr M01_L12
       mov       rcx,[rcx+8]
       mov       rax,8007FFFFFFFFFFFF
       add       rax,rcx
       cmp       rax,3
       jbe       near ptr M01_L14
       vmovq     xmm0,rcx
M01_L02:
       mov       rcx,gs:[58]
       mov       rcx,[rcx+48]
       cmp       dword ptr [rcx+238],2
       jle       near ptr M01_L18
       mov       rcx,[rcx+240]
       mov       rax,[rcx+10]
       test      rax,rax
       je        near ptr M01_L16
M01_L03:
       mov       rcx,[rax+10]
       test      rcx,rcx
       jne       short M01_L04
       mov       rcx,0BA3C000190
       mov       rcx,[rcx]
       test      rcx,rcx
       jne       short M01_L04
       mov       rcx,0BA3C000170
       mov       rcx,[rcx]
       test      rcx,rcx
       jne       short M01_L04
       vmovsd    qword ptr [rsp+20],xmm0
       call      qword ptr [7FF97782D4B8]; System.Globalization.CultureInfo.InitializeUserDefaultCulture()
       mov       rcx,rax
       vmovsd    xmm0,qword ptr [rsp+20]
M01_L04:
       cmp       byte ptr [rcx+61],0
       jne       short M01_L06
       mov       r8,[rcx+18]
       test      r8,r8
       je        near ptr M01_L09
       vmovsd    qword ptr [rsp+20],xmm0
       jmp       short M01_L08
M01_L05:
       mov       r8,0BA3C000448
       cmp       rdx,[r8]
       je        short M01_L10
       mov       rax,0BA3C000450
       movsx     r8,word ptr [rcx+8]
       mov       r10d,2000
       cmp       rdx,[rax]
       cmove     r8d,r10d
       jmp       near ptr M01_L01
M01_L06:
       vmovsd    qword ptr [rsp+20],xmm0
M01_L07:
       mov       rdx,0DA6EC61518
       mov       rax,[rcx]
       mov       rax,[rax+50]
       call      qword ptr [rax]
       mov       r8,rax
       test      r8,r8
       je        short M01_L08
       mov       rcx,offset MT_System.Globalization.NumberFormatInfo
       cmp       [r8],rcx
       jne       near ptr M01_L19
M01_L08:
       vmovsd    xmm0,qword ptr [rsp+20]
       xor       edx,edx
       add       rsp,28
       jmp       qword ptr [7FF977BA7108]; System.Number.FormatFloat[[System.Double, System.Private.CoreLib]](Double, System.String, System.Globalization.NumberFormatInfo)
M01_L09:
       vmovsd    qword ptr [rsp+20],xmm0
       jmp       short M01_L07
M01_L10:
       mov       r8d,1000
       jmp       near ptr M01_L01
M01_L11:
       xor       r8d,r8d
       jmp       near ptr M01_L01
M01_L12:
       cmp       r8d,4
       ja        short M01_L13
       mov       eax,r8d
       lea       r8,[7FF9778A5C28]
       mov       r8d,[r8+rax*4]
       lea       r10,[M01_L00]
       add       r8,r10
       jmp       r8
M01_L13:
       cmp       r8d,1000
       je        near ptr M01_L20
       cmp       r8d,2000
       jne       near ptr M01_L21
       mov       rcx,[rcx+8]
       add       rsp,28
       jmp       qword ptr [7FF977C25B78]; System.Number.UInt64ToDecStr(UInt64)
       mov       rax,0DA6EC69130
       jmp       near ptr M01_L22
       mov       rax,0DA6EC601E0
       mov       rdx,0DA6EC601C0
       cmp       qword ptr [rcx+8],2
       cmovne    rax,rdx
       jmp       near ptr M01_L22
M01_L14:
       cmp       rax,2
       jbe       short M01_L15
       vmovsd    xmm0,qword ptr [7FF9778A5C40]
       jmp       near ptr M01_L02
M01_L15:
       vmovq     xmm0,rax
       vmovsd    qword ptr [rsp+20],xmm0
       vmovsd    xmm0,qword ptr [rsp+20]
       jmp       near ptr M01_L02
M01_L16:
       vmovsd    qword ptr [rsp+20],xmm0
M01_L17:
       mov       ecx,2
       call      qword ptr [7FF977BACB28]; System.Runtime.CompilerServices.StaticsHelpers.GetOptimizedGCThreadStaticBase(Int32)
       vmovsd    xmm0,qword ptr [rsp+20]
       jmp       near ptr M01_L03
M01_L18:
       vmovsd    qword ptr [rsp+20],xmm0
       jmp       short M01_L17
M01_L19:
       mov       rdx,rax
       call      qword ptr [7FF977826328]; System.Runtime.CompilerServices.CastHelpers.ChkCastClass(Void*, System.Object)
       int       3
M01_L20:
       mov       rcx,[rcx+8]
       mov       r8,0BA3C000180
       mov       r8,[r8]
       xor       edx,edx
       add       rsp,28
       jmp       qword ptr [7FF977C2CA68]
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       jmp       short M01_L22
M01_L21:
       call      qword ptr [7FF977C25E78]; AuroraScript.Runtime.ScriptDatum.get_Object()
       mov       rcx,rax
       mov       rax,[rax]
       mov       rax,[rax+40]
       add       rsp,28
       jmp       qword ptr [rax+8]
M01_L22:
       add       rsp,28
       ret
; Total bytes of code 662
```
```assembly
; System.SpanHelpers.SequenceEqual(Byte ByRef, Byte ByRef, UIntPtr)
       cmp       r8,8
       jae       short M02_L05
       cmp       r8,4
       jae       short M02_L04
       xor       eax,eax
       mov       r10,r8
       and       r10,2
       je        short M02_L00
       movzx     eax,word ptr [rcx]
       movzx     r9d,word ptr [rdx]
       sub       eax,r9d
M02_L00:
       test      r8b,1
       jne       short M02_L03
M02_L01:
       test      eax,eax
       sete      al
       movzx     eax,al
M02_L02:
       vzeroupper
       ret
M02_L03:
       movzx     r8d,byte ptr [rcx+r10]
       movzx     ecx,byte ptr [rdx+r10]
       sub       r8d,ecx
       or        eax,r8d
       jmp       short M02_L01
M02_L04:
       add       r8,0FFFFFFFFFFFFFFFC
       mov       eax,[rcx]
       sub       eax,[rdx]
       mov       ecx,[rcx+r8]
       sub       ecx,[rdx+r8]
       or        eax,ecx
       sete      al
       movzx     eax,al
       jmp       short M02_L02
M02_L05:
       cmp       rcx,rdx
       je        near ptr M02_L11
       cmp       r8,20
       jb        short M02_L08
       xor       eax,eax
       add       r8,0FFFFFFFFFFFFFFE0
       je        short M02_L07
M02_L06:
       vmovups   ymm0,[rcx+rax]
       vpcmpeqb  ymm0,ymm0,[rdx+rax]
       vpmovmskb r10d,ymm0
       cmp       r10d,0FFFFFFFF
       jne       near ptr M02_L13
       add       rax,20
       cmp       r8,rax
       ja        short M02_L06
M02_L07:
       vmovups   ymm0,[rcx+r8]
       vpcmpeqb  ymm0,ymm0,[rdx+r8]
       vpmovmskb ecx,ymm0
       cmp       ecx,0FFFFFFFF
       jne       short M02_L13
       jmp       short M02_L11
M02_L08:
       cmp       r8,10
       jb        short M02_L12
       xor       eax,eax
       add       r8,0FFFFFFFFFFFFFFF0
       je        short M02_L10
M02_L09:
       vmovups   xmm0,[rcx+rax]
       vpcmpeqb  xmm0,xmm0,[rdx+rax]
       vpmovmskb r10d,xmm0
       cmp       r10d,0FFFF
       jne       short M02_L13
       add       rax,10
       cmp       r8,rax
       ja        short M02_L09
M02_L10:
       vmovups   xmm0,[rcx+r8]
       vpcmpeqb  xmm0,xmm0,[rdx+r8]
       vpmovmskb eax,xmm0
       cmp       eax,0FFFF
       jne       short M02_L13
M02_L11:
       mov       eax,1
       vzeroupper
       ret
M02_L12:
       lea       rax,[r8-8]
       mov       r8,[rcx]
       sub       r8,[rdx]
       mov       rcx,[rcx+rax]
       sub       rcx,[rdx+rax]
       or        rcx,r8
       sete      al
       movzx     eax,al
       jmp       near ptr M02_L02
M02_L13:
       xor       eax,eax
       vzeroupper
       ret
; Total bytes of code 290
```
```assembly
; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rdx,rdx
       je        short M03_L02
       mov       rax,[rdx]
       cmp       rax,rcx
       je        short M03_L02
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M03_L02
M03_L00:
       test      rax,rax
       je        short M03_L01
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M03_L02
       test      rax,rax
       jne       short M03_L03
M03_L01:
       xor       edx,edx
M03_L02:
       mov       rax,rdx
       ret
M03_L03:
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M03_L02
       test      rax,rax
       je        short M03_L01
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M03_L02
       test      rax,rax
       je        short M03_L01
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M03_L02
       jmp       short M03_L00
; Total bytes of code 86
```
```assembly
; AuroraScript.Runtime.Property.HiddenClass.AddProperty(System.String, AuroraScript.Runtime.Property.PropertyFlags, AuroraScript.Runtime.Property.PropertyMeta ByRef)
       push      r15
       push      r14
       push      r13
       push      r12
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,98
       xor       eax,eax
       mov       [rsp+38],rax
       vxorps    xmm4,xmm4,xmm4
       vmovdqu   ymmword ptr [rsp+40],ymm4
       vmovdqu   ymmword ptr [rsp+60],ymm4
       vmovdqa   xmmword ptr [rsp+80],xmm4
       mov       [rsp+90],rax
       mov       rbx,rcx
       mov       rbp,rdx
       mov       r15d,r8d
       mov       r14,r9
       mov       r8,[rbx+8]
       mov       esi,[r8+8]
       dec       esi
       js        near ptr M04_L04
       mov       r8,0BA3C000068
       mov       rdi,[r8]
M04_L00:
       mov       r8,[rbx+8]
       cmp       esi,[r8+8]
       jae       near ptr M04_L11
       mov       edx,esi
       shl       rdx,5
       mov       r8,[r8+rdx+10]
       cmp       r8,rbp
       je        short M04_L01
       test      r8,r8
       je        short M04_L03
       test      rbp,rbp
       je        short M04_L03
       mov       edx,[r8+8]
       cmp       edx,[rbp+8]
       jne       short M04_L03
       lea       rcx,[r8+0C]
       mov       r8d,[r8+8]
       add       r8d,r8d
       lea       rdx,[rbp+0C]
       call      qword ptr [7FF97782C330]; System.SpanHelpers.SequenceEqual(Byte ByRef, Byte ByRef, UIntPtr)
       test      eax,eax
       je        short M04_L03
M04_L01:
       mov       r13d,esi
M04_L02:
       test      r13d,r13d
       jl        short M04_L05
       mov       rax,[rbx+8]
       cmp       r13d,[rax+8]
       jae       near ptr M04_L11
       mov       ecx,r13d
       shl       rcx,5
       lea       rax,[rax+rcx+18]
       movzx     ecx,word ptr [rax+4]
       mov       rax,[rax]
       movzx     edx,r15w
       cmp       ecx,edx
       jne       short M04_L05
       mov       [r14],rax
       mov       [r14+4],cx
       mov       rax,rbx
       add       rsp,98
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       ret
M04_L03:
       dec       esi
       jns       near ptr M04_L00
M04_L04:
       mov       r13d,0FFFFFFFF
       jmp       short M04_L02
M04_L05:
       movzx     r12d,r15w
       cmp       qword ptr [rbx+10],0
       je        short M04_L06
       mov       rcx,[rbx+10]
       mov       [rsp+38],rbp
       mov       [rsp+40],r12w
       lea       rdx,[rsp+38]
       lea       r8,[rsp+90]
       cmp       [rcx],ecx
       call      qword ptr [7FF977AC0BB0]; Precode of System.Collections.Generic.Dictionary`2[[AuroraScript.Runtime.Property.TransitionKey, AuroraScript],[System.__Canon, System.Private.CoreLib]].TryGetValue(AuroraScript.Runtime.Property.TransitionKey, System.__Canon ByRef)
       test      eax,eax
       je        short M04_L06
       mov       rcx,[rsp+90]
       mov       rdx,rbp
       mov       r8,r14
       cmp       [rcx],ecx
       call      qword ptr [7FF9779FE838]; AuroraScript.Runtime.Property.HiddenClass.TryGet(System.String, AuroraScript.Runtime.Property.PropertyMeta ByRef)
       mov       rax,[rsp+90]
       add       rsp,98
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       ret
M04_L06:
       test      r13d,r13d
       jl        near ptr M04_L07
       mov       rcx,[rbx+8]
       cmp       r13d,[rcx+8]
       jae       near ptr M04_L11
       mov       eax,r13d
       shl       rax,5
       movzx     eax,word ptr [rcx+rax+18]
       movzx     esi,word ptr [rbx+38]
       mov       [rsp+8C],esi
       xor       ecx,ecx
       mov       [r14],rcx
       mov       [r14],ax
       mov       [r14+4],r15w
       mov       rcx,[rbx+8]
       cmp       [rcx],cl
       call      qword ptr [7FF977BA54E8]; System.Object.MemberwiseClone()
       mov       r15,rax
       mov       r8,[r14]
       lea       rcx,[rsp+48]
       mov       rdx,rbp
       call      qword ptr [7FF9779FEA18]; AuroraScript.Runtime.Property.HiddenProperty..ctor(System.String, AuroraScript.Runtime.Property.PropertyMeta)
       cmp       r13d,[r15+8]
       jae       near ptr M04_L11
       mov       eax,r13d
       shl       rax,5
       lea       rdi,[r15+rax+10]
       lea       rsi,[rsp+48]
       call      CORINFO_HELP_ASSIGN_BYREF
       movsq
       call      CORINFO_HELP_ASSIGN_BYREF
       movsq
       jmp       near ptr M04_L09
M04_L07:
       movzx     eax,word ptr [rbx+38]
       movzx     edx,word ptr [rbx+38]
       add       edx,1
       jo        near ptr M04_L12
       cmp       edx,0FFFF
       ja        near ptr M04_L12
       mov       esi,edx
       mov       [rsp+8C],esi
       xor       edx,edx
       mov       [r14],rdx
       mov       [r14],ax
       mov       [r14+4],r15w
       mov       rdx,[rbx+8]
       mov       edx,[rdx+8]
       inc       edx
       movsxd    rdx,edx
       mov       rcx,offset MT_AuroraScript.Runtime.Property.HiddenProperty[]
       call      CORINFO_HELP_NEWARR_1_VC
       mov       r15,rax
       mov       r8,[rbx+8]
       cmp       dword ptr [r8+8],0
       je        short M04_L08
       mov       r8,[rbx+8]
       mov       r8d,[r8+8]
       mov       rcx,[rbx+8]
       mov       rdx,r15
       call      qword ptr [7FF97782F588]; System.Array.Copy(System.Array, System.Array, Int32)
M04_L08:
       mov       edi,[r15+8]
       dec       edi
       mov       r8,[r14]
       lea       rcx,[rsp+68]
       mov       rdx,rbp
       call      qword ptr [7FF9779FEA18]; AuroraScript.Runtime.Property.HiddenProperty..ctor(System.String, AuroraScript.Runtime.Property.PropertyMeta)
       cmp       edi,[r15+8]
       jae       near ptr M04_L11
       mov       eax,edi
       shl       rax,5
       lea       rdi,[r15+rax+10]
       lea       rsi,[rsp+68]
       call      CORINFO_HELP_ASSIGN_BYREF
       movsq
       call      CORINFO_HELP_ASSIGN_BYREF
       movsq
M04_L09:
       mov       rcx,offset MT_AuroraScript.Runtime.Property.HiddenClass
       call      CORINFO_HELP_NEWSFAST
       mov       rsi,rax
       mov       [rsp+20],rbp
       mov       ecx,r13d
       shr       ecx,1F
       mov       [rsp+28],ecx
       mov       rcx,rsi
       mov       rdx,r15
       mov       r8d,[rsp+8C]
       mov       r9,rbx
       call      qword ptr [7FF9779FEA30]; AuroraScript.Runtime.Property.HiddenClass..ctor(AuroraScript.Runtime.Property.HiddenProperty[], UInt16, AuroraScript.Runtime.Property.HiddenClass, System.String, Boolean)
       cmp       qword ptr [rbx+10],0
       jne       short M04_L10
       mov       rcx,offset MT_System.Collections.Generic.Dictionary<AuroraScript.Runtime.Property.TransitionKey, AuroraScript.Runtime.Property.HiddenClass>
       call      CORINFO_HELP_NEWSFAST
       mov       rdi,rax
       mov       rcx,rdi
       mov       edx,4
       xor       r8d,r8d
       call      qword ptr [7FF9779FEAD8]; System.Collections.Generic.Dictionary`2[[AuroraScript.Runtime.Property.TransitionKey, AuroraScript],[System.__Canon, System.Private.CoreLib]]..ctor(Int32, System.Collections.Generic.IEqualityComparer`1<AuroraScript.Runtime.Property.TransitionKey>)
       lea       rcx,[rbx+10]
       mov       rdx,rdi
       call      CORINFO_HELP_ASSIGN_REF
M04_L10:
       mov       rcx,[rbx+10]
       cmp       [rcx],cl
       mov       [rsp+38],rbp
       mov       [rsp+40],r12w
       lea       rdx,[rsp+38]
       mov       r8,rsi
       mov       r9d,1
       call      qword ptr [7FF9779FEBE0]; System.Collections.Generic.Dictionary`2[[AuroraScript.Runtime.Property.TransitionKey, AuroraScript],[System.__Canon, System.Private.CoreLib]].TryInsert(AuroraScript.Runtime.Property.TransitionKey, System.__Canon, System.Collections.Generic.InsertionBehavior)
       mov       rax,rsi
       add       rsp,98
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       ret
M04_L11:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
M04_L12:
       call      CORINFO_HELP_OVERFLOW
       int       3
; Total bytes of code 868
```
```assembly
; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rax,[rcx+20]
       mov       rax,[rax-18]
       mov       rdx,rax
       test      dl,1
       jne       short M05_L00
       ret
M05_L00:
       jmp       qword ptr [7FF977825C38]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBaseSlow(System.Runtime.CompilerServices.MethodTable*)
; Total bytes of code 23
```
```assembly
; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfInterface(Void*, System.Object)
       test      rdx,rdx
       je        short M06_L01
       mov       rax,[rdx]
       movzx     r8d,word ptr [rax+0E]
       test      r8,r8
       jne       short M06_L02
M06_L00:
       test      dword ptr [rax],500C0000
       jne       short M06_L05
       xor       edx,edx
M06_L01:
       mov       rax,rdx
       ret
M06_L02:
       mov       r10,[rax+38]
       cmp       r8,4
       jl        short M06_L04
M06_L03:
       cmp       [r10],rcx
       je        short M06_L01
       cmp       [r10+8],rcx
       je        short M06_L01
       cmp       [r10+10],rcx
       je        short M06_L01
       cmp       [r10+18],rcx
       je        short M06_L01
       add       r10,20
       add       r8,0FFFFFFFFFFFFFFFC
       cmp       r8,4
       jge       short M06_L03
       test      r8,r8
       je        short M06_L00
M06_L04:
       cmp       [r10],rcx
       je        short M06_L01
       add       r10,8
       dec       r8
       test      r8,r8
       jg        short M06_L04
       jmp       short M06_L00
M06_L05:
       jmp       qword ptr [7FF977BA5470]; System.Runtime.CompilerServices.CastHelpers.IsInstance_Helper(Void*, System.Object)
; Total bytes of code 109
```
```assembly
; AuroraScript.Runtime.ScriptDatum.get_Object()
       push      rsi
       push      rbx
       sub       rsp,28
       mov       rbx,rcx
       mov       rdx,[rbx]
       mov       rax,rdx
       test      rax,rax
       je        short M07_L00
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       cmp       [rax],rcx
       jne       short M07_L01
M07_L00:
       test      rax,rax
       je        short M07_L02
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
M07_L01:
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       jmp       short M07_L00
M07_L02:
       mov       rdx,[rbx]
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbx,rax
       test      rbx,rbx
       jne       short M07_L03
       xor       eax,eax
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
M07_L03:
       cmp       dword ptr [rbx+8],0
       jne       short M07_L04
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0BA3C001490
       mov       rax,[rcx]
       jmp       short M07_L05
M07_L04:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      CORINFO_HELP_NEWSFAST
       mov       rsi,rax
       mov       rcx,rsi
       mov       rdx,rbx
       call      qword ptr [7FF977C2CA38]
       mov       rax,rsi
M07_L05:
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
; Total bytes of code 165
```
```assembly
; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rax,[rcx+20]
       mov       rax,[rax-10]
       mov       rdx,rax
       test      dl,1
       jne       short M08_L00
       ret
M08_L00:
       jmp       qword ptr [7FF9779FFD50]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBaseSlow(System.Runtime.CompilerServices.MethodTable*)
; Total bytes of code 23
```
```assembly
; AuroraScript.Runtime.Types.ScriptObject.TryResolveProperty(System.String, AuroraScript.Runtime.Property.PropertyDescriptor ByRef)
       push      r15
       push      r14
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,38
       vxorps    xmm4,xmm4,xmm4
       vmovdqa   xmmword ptr [rsp+20],xmm4
       xor       eax,eax
       mov       [rsp+30],rax
       mov       rsi,rcx
       mov       rdi,rdx
       mov       rbx,r8
M09_L00:
       mov       rbp,[rsi+20]
       mov       r8,[rbp+8]
       mov       r14d,[r8+8]
       dec       r14d
       js        near ptr M09_L07
       mov       r8,0BA3C000068
       mov       r15,[r8]
M09_L01:
       mov       r8,[rbp+8]
       cmp       r14d,[r8+8]
       jae       near ptr M09_L11
       mov       edx,r14d
       shl       rdx,5
       mov       r8,[r8+rdx+10]
       cmp       r8,rdi
       je        short M09_L02
       test      r8,r8
       je        near ptr M09_L06
       test      rdi,rdi
       je        near ptr M09_L06
       mov       edx,[r8+8]
       cmp       edx,[rdi+8]
       jne       near ptr M09_L06
       lea       rcx,[r8+0C]
       mov       r8d,[r8+8]
       add       r8d,r8d
       lea       rdx,[rdi+0C]
       call      qword ptr [7FF97782C330]; System.SpanHelpers.SequenceEqual(Byte ByRef, Byte ByRef, UIntPtr)
       test      eax,eax
       je        short M09_L06
M09_L02:
       mov       eax,r14d
M09_L03:
       test      eax,eax
       jl        near ptr M09_L08
       mov       rcx,[rbp+8]
       cmp       eax,[rcx+8]
       jae       near ptr M09_L11
       mov       eax,eax
       shl       rax,5
       movzx     eax,word ptr [rcx+rax+18]
       cmp       qword ptr [rsi+18],0
       jne       near ptr M09_L09
M09_L04:
       xor       ecx,ecx
M09_L05:
       mov       rdx,[rsi+10]
       cmp       eax,[rdx+8]
       jae       near ptr M09_L11
       mov       eax,eax
       shl       rax,4
       vmovdqu   xmm0,xmmword ptr [rdx+rax+10]
       vmovdqu   xmmword ptr [rsp+28],xmm0
       mov       [rsp+20],rcx
       mov       rdi,rbx
       lea       rsi,[rsp+20]
       call      CORINFO_HELP_ASSIGN_BYREF
       call      CORINFO_HELP_ASSIGN_BYREF
       movsq
       mov       eax,1
       add       rsp,38
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       pop       r15
       ret
M09_L06:
       dec       r14d
       jns       near ptr M09_L01
M09_L07:
       mov       eax,0FFFFFFFF
       jmp       near ptr M09_L03
M09_L08:
       mov       rsi,[rsi+8]
       test      rsi,rsi
       je        short M09_L10
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rsp+20],xmm0
       vmovdqu   xmmword ptr [rsp+28],xmm0
       jmp       near ptr M09_L00
M09_L09:
       mov       rcx,[rsi+18]
       cmp       [rcx+8],eax
       jle       near ptr M09_L04
       mov       rcx,[rsi+18]
       cmp       eax,[rcx+8]
       jae       short M09_L11
       mov       edx,eax
       mov       rcx,[rcx+rdx*8+10]
       jmp       near ptr M09_L05
M09_L10:
       xor       eax,eax
       mov       [rbx],rax
       mov       [rbx+8],rax
       mov       [rbx+10],rax
       add       rsp,38
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       pop       r15
       ret
M09_L11:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 396
```
```assembly
; AuroraScript.Runtime.Types.ScriptObject.Resize()
       push      rbp
       sub       rsp,30
       lea       rbp,[rsp+30]
       xor       eax,eax
       mov       [rbp-4],eax
       mov       [rbp-8],eax
       mov       [rbp+10],rcx
       mov       rax,[rbp+10]
       mov       rax,[rax+10]
       cmp       dword ptr [rax+8],0
       je        short M10_L00
       mov       rax,[rbp+10]
       mov       rax,[rax+10]
       mov       eax,[rax+8]
       add       eax,eax
       mov       [rbp-8],eax
       jmp       short M10_L01
M10_L00:
       mov       dword ptr [rbp-8],2
M10_L01:
       mov       eax,[rbp-8]
       mov       [rbp-4],eax
       mov       rax,[rbp+10]
       cmp       [rax],al
       mov       rax,[rbp+10]
       lea       rcx,[rax+10]
       mov       edx,[rbp-4]
       call      qword ptr [7FF9779FED90]; System.Array.Resize[[AuroraScript.Runtime.ScriptDatum, AuroraScript]](AuroraScript.Runtime.ScriptDatum[] ByRef, Int32)
       mov       rax,[rbp+10]
       cmp       qword ptr [rax+18],0
       je        short M10_L02
       mov       rax,[rbp+10]
       cmp       [rax],al
       mov       rax,[rbp+10]
       lea       rdx,[rax+18]
       mov       r8d,[rbp-4]
       mov       rcx,7FF977AC3990
       call      qword ptr [7FF9779FC600]; System.Array.Resize[[System.__Canon, System.Private.CoreLib]](System.__Canon[] ByRef, Int32)
M10_L02:
       nop
       add       rsp,30
       pop       rbp
       ret
; Total bytes of code 142
```
```assembly
; System.Globalization.CultureInfo.InitializeUserDefaultCulture()
       push      rsi
       push      rbx
       sub       rsp,28
       call      qword ptr [7FF96E8397E8]
       mov       rbx,rax
       mov       rsi,rbx
       call      qword ptr [7FF96E850A28]
       mov       rdx,rax
       test      rsi,rsi
       je        short M11_L00
       mov       rcx,rsi
       xor       r8d,r8d
       call      qword ptr [7FF96E851BC8]
       mov       rax,[rbx]
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
M11_L00:
       call      qword ptr [7FF96E84F410]
       int       3
; Total bytes of code 61
```
```assembly
; System.Number.FormatFloat[[System.Double, System.Private.CoreLib]](Double, System.String, System.Globalization.NumberFormatInfo)
       push      rbp
       push      rbx
       sub       rsp,58
       lea       rbp,[rsp+20]
       xor       eax,eax
       mov       [rbp+8],rax
       vxorps    xmm4,xmm4,xmm4
       vmovdqu   ymmword ptr [rbp+10],ymm4
       mov       [rbp+30],rax
       mov       rax,5275BDC81ABA
       mov       [rbp],rax
       mov       r9,r8
       test      [rsp],esp
       sub       rsp,40
       lea       r8,[rsp+20]
       mov       [rbp+28],r8
       mov       dword ptr [rbp+30],20
       test      rdx,rdx
       jne       near ptr M12_L04
       xor       r8d,r8d
       xor       ecx,ecx
M12_L00:
       mov       [rbp+8],r8
       mov       [rbp+10],ecx
       lea       r8,[rbp+8]
       lea       rcx,[rbp+18]
       vmovaps   xmm1,xmm0
       call      qword ptr [7FF977BA7120]; System.Number.FormatFloat[[System.Double, System.Private.CoreLib],[System.Char, System.Private.CoreLib]](System.Collections.Generic.ValueListBuilder`1<Char> ByRef, Double, System.ReadOnlySpan`1<Char>, System.Globalization.NumberFormatInfo)
       mov       rbx,rax
       test      rbx,rbx
       jne       short M12_L01
       mov       ecx,[rbp+20]
       cmp       ecx,[rbp+30]
       ja        short M12_L05
       mov       rax,[rbp+28]
       mov       [rbp+8],rax
       mov       [rbp+10],ecx
       lea       rcx,[rbp+8]
       call      System.String.Ctor(System.ReadOnlySpan`1<Char>)
       mov       rbx,rax
M12_L01:
       mov       rdx,[rbp+18]
       test      rdx,rdx
       je        short M12_L02
       xor       ecx,ecx
       mov       [rbp+18],rcx
       mov       rcx,0BA3C0002B0
       mov       rcx,[rcx]
       xor       r8d,r8d
       call      qword ptr [7FF977A19840]; Precode of System.Buffers.SharedArrayPool`1[[System.Char, System.Private.CoreLib]].Return(Char[], Boolean)
M12_L02:
       mov       rax,rbx
       mov       r8,5275BDC81ABA
       cmp       [rbp],r8
       je        short M12_L03
       call      CORINFO_HELP_FAIL_FAST
M12_L03:
       nop
       lea       rsp,[rbp+38]
       pop       rbx
       pop       rbp
       ret
M12_L04:
       lea       r8,[rdx+0C]
       mov       ecx,[rdx+8]
       jmp       near ptr M12_L00
M12_L05:
       call      qword ptr [7FF9779F7D50]
       int       3
; Total bytes of code 236
```
```assembly
; System.Number.UInt64ToDecStr(UInt64)
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,28
       xor       eax,eax
       mov       [rsp+20],rax
       mov       rbx,rcx
       cmp       rbx,12C
       jb        near ptr M13_L08
       mov       rcx,rbx
       or        rcx,1
       lzcnt     rcx,rcx
       xor       ecx,3F
       mov       rax,7FF96DBC7050
       movzx     ecx,byte ptr [rax+rcx]
       mov       rax,7FF96DBCCE38
       cmp       rbx,[rax+rcx*8]
       setb      al
       movzx     eax,al
       sub       ecx,eax
       movsxd    rsi,ecx
       mov       rcx,rsi
       call      qword ptr [7FF977826670]; System.String.FastAllocateString(IntPtr)
       mov       rdi,rax
       test      rdi,rdi
       je        short M13_L00
       mov       rcx,7FF977CF5DB4
       call      CORINFO_HELP_COUNTPROFILE32
       lea       rcx,[rdi+0C]
       mov       [rsp+20],rcx
       mov       rbp,[rsp+20]
       jmp       short M13_L01
M13_L00:
       xor       edi,edi
       xor       ebp,ebp
M13_L01:
       mov       rcx,7FF977CF5DB8
       call      CORINFO_HELP_COUNTPROFILE32
       lea       rax,[rbp+rsi*2]
       cmp       rbx,0A
       jb        short M13_L06
       cmp       rbx,64
       jb        short M13_L03
       mov       rcx,0DA6EC61B94
M13_L02:
       add       rax,0FFFFFFFFFFFFFFFC
       mov       r8,28F5C28F5C28F5C3
       mov       rdx,rbx
       shr       rdx,2
       mulx      rdx,rdx,r8
       shr       rdx,2
       imul      r8,rdx,64
       sub       rbx,r8
       mov       r8,rcx
       shl       ebx,2
       mov       r10d,ebx
       mov       r8d,[r8+r10]
       mov       [rax],r8d
       cmp       rdx,64
       jb        short M13_L04
       mov       rbx,rdx
       jmp       short M13_L02
M13_L03:
       mov       rdx,rbx
M13_L04:
       cmp       rdx,0A
       jb        short M13_L05
       add       rax,0FFFFFFFFFFFFFFFC
       mov       rcx,0DA6EC61B94
       shl       edx,2
       mov       ecx,[rcx+rdx]
       mov       [rax],ecx
       jmp       short M13_L07
M13_L05:
       mov       rbx,rdx
M13_L06:
       lea       ecx,[rbx+30]
       mov       [rax-2],cx
M13_L07:
       xor       eax,eax
       mov       [rsp+20],rax
       mov       rax,rdi
       add       rsp,28
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       ret
M13_L08:
       mov       rcx,7FF977CF5DB0
       call      CORINFO_HELP_COUNTPROFILE32
       mov       ecx,ebx
       call      qword ptr [7FF977BA5578]; System.Number.UInt32ToDecStrForKnownSmallNumber(UInt32)
       nop
       add       rsp,28
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       ret
; Total bytes of code 338
```
```assembly
; System.Runtime.CompilerServices.StaticsHelpers.GetOptimizedGCThreadStaticBase(Int32)
       push      rbx
       sub       rsp,20
       mov       ebx,ecx
       call      qword ptr [7FF96E851D18]; Precode of System.Threading.Thread.GetThreadStaticsBase()
       mov       ecx,ebx
       and       ecx,0FFFFFF
       mov       edx,ecx
       mov       r8d,ebx
       sar       r8d,18
       jne       short M14_L01
       cmp       [rax],ecx
       jle       short M14_L03
       mov       rax,[rax+8]
       cmp       [rax],al
       add       edx,0FFFFFFFE
       movsxd    rcx,edx
       mov       rax,[rax+rcx*8+10]
       test      rax,rax
       je        short M14_L03
M14_L00:
       add       rsp,20
       pop       rbx
       ret
M14_L01:
       mov       ecx,ebx
       sar       ecx,18
       cmp       ecx,2
       jne       short M14_L02
       movsxd    rcx,edx
       add       rax,rcx
       jmp       short M14_L00
M14_L02:
       cmp       [rax+4],edx
       jle       short M14_L03
       mov       rcx,[rax+10]
       movsxd    rax,edx
       mov       rcx,[rcx+rax*8]
       test      rcx,rcx
       je        short M14_L03
       mov       rax,[rcx]
       test      rax,rax
       je        short M14_L03
       jmp       short M14_L00
M14_L03:
       mov       ecx,ebx
       lea       rax,[System.Reflection.CustomAttributeExtensions.GetCustomAttribute[[System.__Canon, System.Private.CoreLib]](System.Reflection.Assembly)]
       add       rsp,20
       pop       rbx
       jmp       qword ptr [rax]
; Total bytes of code 130
```
```assembly
; System.Runtime.CompilerServices.CastHelpers.ChkCastClass(Void*, System.Object)
       test      rdx,rdx
       je        short M15_L00
       cmp       [rdx],rcx
       jne       short M15_L01
M15_L00:
       mov       rax,rdx
       ret
M15_L01:
       jmp       qword ptr [7FF9779F4D20]; System.Runtime.CompilerServices.CastHelpers.ChkCastClassSpecial(Void*, System.Object)
; Total bytes of code 20
```
```assembly
; AuroraScript.Runtime.Property.HiddenClass.TryGet(System.String, AuroraScript.Runtime.Property.PropertyMeta ByRef)
       push      r14
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,20
       mov       rbx,rcx
       mov       rsi,rdx
       mov       rdi,r8
       mov       r8,[rbx+8]
       mov       ebp,[r8+8]
       dec       ebp
       js        near ptr M16_L04
       mov       r8,0BA3C000068
       mov       r14,[r8]
M16_L00:
       mov       r8,[rbx+8]
       cmp       ebp,[r8+8]
       jae       near ptr M16_L06
       mov       edx,ebp
       shl       rdx,5
       mov       r8,[r8+rdx+10]
       cmp       r8,rsi
       je        short M16_L01
       test      r8,r8
       je        short M16_L03
       test      rsi,rsi
       je        short M16_L03
       mov       edx,[r8+8]
       cmp       edx,[rsi+8]
       jne       short M16_L03
       lea       rcx,[r8+0C]
       mov       r8d,[r8+8]
       add       r8d,r8d
       lea       rdx,[rsi+0C]
       call      qword ptr [7FF97782C330]; System.SpanHelpers.SequenceEqual(Byte ByRef, Byte ByRef, UIntPtr)
       test      eax,eax
       je        short M16_L03
M16_L01:
       mov       eax,ebp
M16_L02:
       test      eax,eax
       jl        short M16_L05
       mov       rcx,[rbx+8]
       cmp       eax,[rcx+8]
       jae       short M16_L06
       mov       eax,eax
       shl       rax,5
       mov       rax,[rcx+rax+18]
       mov       [rdi],rax
       mov       eax,1
       add       rsp,20
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       ret
M16_L03:
       dec       ebp
       jns       short M16_L00
M16_L04:
       mov       eax,0FFFFFFFF
       jmp       short M16_L02
M16_L05:
       xor       eax,eax
       mov       [rdi],rax
       add       rsp,20
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       ret
M16_L06:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 200
```
```assembly
; System.Object.MemberwiseClone()
       push      rbp
       push      r15
       push      r14
       push      r13
       push      r12
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,88
       lea       rbp,[rsp+0C0]
       mov       rbx,rcx
       mov       [rbp+10],rbx
       mov       [rbp-40],rbx
       lea       rcx,[rbp-40]
       mov       [rbp-0A0],rcx
       lea       rcx,[rbp-98]
       call      qword ptr [7FF96E839030]; CORINFO_HELP_JIT_PINVOKE_BEGIN
       mov       rax,[System.Reflection.CustomAttributeExtensions.GetCustomAttribute[[System.__Canon, System.Private.CoreLib]](System.Reflection.Assembly)]
       mov       rcx,[rbp-0A0]
       call      qword ptr [rax]
       lea       rcx,[rbp-98]
       call      qword ptr [7FF96E839038]; CORINFO_HELP_JIT_PINVOKE_END
       mov       rcx,[rbp-40]
       mov       rdx,[rcx]
       mov       r8,rdx
       mov       eax,[r8+4]
       add       rax,0FFFFFFFFFFFFFFF0
       test      dword ptr [r8],80000000
       je        short M17_L00
       mov       r10d,[rcx+8]
       movzx     r8d,word ptr [r8]
       imul      r8,r10
       add       rax,r8
M17_L00:
       mov       rbx,[rbp+10]
       cmp       [rbx],bl
       add       rbx,8
       mov       rcx,[rbp-40]
       cmp       [rcx],cl
       add       rcx,8
       test      dword ptr [rdx],1000000
       jne       short M17_L02
       mov       rdx,rbx
       mov       r8,rax
       call      qword ptr [7FF96E84D928]; Precode of System.SpanHelpers.Memmove(Byte ByRef, Byte ByRef, UIntPtr)
M17_L01:
       mov       rax,[rbp-40]
       add       rsp,88
       pop       rbx
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       pop       rbp
       ret
M17_L02:
       cmp       rax,4000
       jbe       short M17_L03
       mov       rdx,rbx
       mov       r8,rax
       call      qword ptr [7FF96E84A668]
       jmp       short M17_L01
M17_L03:
       mov       rdx,rbx
       mov       r8,rax
       call      qword ptr [7FF96E84A630]
       mov       rax,[System.Reflection.CustomAttributeExtensions.GetCustomAttribute[[System.__Canon, System.Private.CoreLib]](System.Reflection.Assembly)]
       cmp       dword ptr [rax],0
       je        short M17_L01
       call      qword ptr [7FF96E839040]; CORINFO_HELP_POLL_GC
       jmp       short M17_L01
; Total bytes of code 251
```
```assembly
; AuroraScript.Runtime.Property.HiddenProperty..ctor(System.String, AuroraScript.Runtime.Property.PropertyMeta)
       push      rbp
       push      rdi
       push      rsi
       sub       rsp,40
       lea       rbp,[rsp+50]
       vxorps    xmm4,xmm4,xmm4
       vmovdqu   xmmword ptr [rbp-28],xmm4
       xor       eax,eax
       mov       [rbp-18],rax
       mov       [rbp+10],rcx
       mov       [rbp+18],rdx
       mov       [rbp+20],r8
       mov       rcx,[rbp+10]
       mov       rdx,[rbp+18]
       call      CORINFO_HELP_CHECKED_ASSIGN_REF
       mov       rax,[rbp+10]
       mov       rcx,[rbp+20]
       mov       [rax+8],rcx
       mov       rax,[rbp+10]
       cmp       [rax],al
       mov       rax,[rbp+10]
       add       rax,10
       mov       [rbp-28],rax
       lea       rcx,[rbp-20]
       mov       rdx,[rbp+18]
       call      qword ptr [7FF9779FEA78]; AuroraScript.Runtime.ScriptDatum.FromString(System.String)
       mov       rdi,[rbp-28]
       lea       rsi,[rbp-20]
       call      CORINFO_HELP_ASSIGN_BYREF
       movsq
       add       rsp,40
       pop       rsi
       pop       rdi
       pop       rbp
       ret
; Total bytes of code 119
```
```assembly
; System.Array.Copy(System.Array, System.Array, Int32)
       push      r14
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,30
       mov       rsi,rcx
       mov       rbx,rdx
       mov       edi,r8d
       test      rsi,rsi
       jne       short M19_L01
       xor       ebp,ebp
M19_L00:
       test      rbx,rbx
       jne       short M19_L03
       xor       eax,eax
       jmp       short M19_L04
M19_L01:
       test      rbx,rbx
       je        short M19_L02
       mov       r14,[rsi]
       cmp       r14,[rbx]
       jne       short M19_L02
       cmp       dword ptr [r14+4],18
       jne       short M19_L02
       cmp       edi,[rsi+8]
       ja        short M19_L02
       cmp       edi,[rbx+8]
       jbe       short M19_L06
M19_L02:
       mov       rcx,rsi
       xor       edx,edx
       call      qword ptr [7FF96E84A390]; Precode of System.Array.GetLowerBound(Int32)
       mov       ebp,eax
       jmp       short M19_L00
M19_L03:
       mov       rcx,rbx
       xor       edx,edx
       call      qword ptr [7FF96E84A390]; Precode of System.Array.GetLowerBound(Int32)
M19_L04:
       mov       [rsp+20],edi
       xor       ecx,ecx
       mov       [rsp+28],ecx
       mov       rcx,rsi
       mov       edx,ebp
       mov       r8,rbx
       mov       r9d,eax
       call      qword ptr [7FF96E84A328]
M19_L05:
       nop
       add       rsp,30
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       ret
M19_L06:
       mov       r8d,edi
       movzx     ecx,word ptr [r14]
       imul      r8,rcx
       lea       rdx,[rsi+10]
       lea       rcx,[rbx+10]
       test      dword ptr [r14],1000000
       je        short M19_L08
       cmp       r8,4000
       ja        short M19_L07
       call      qword ptr [7FF96E84A630]
       mov       rax,[System.Reflection.CustomAttributeExtensions.GetCustomAttribute[[System.__Canon, System.Private.CoreLib]](System.Reflection.Assembly)]
       cmp       dword ptr [rax],0
       je        short M19_L05
       call      qword ptr [7FF96E839040]; CORINFO_HELP_POLL_GC
       jmp       short M19_L05
M19_L07:
       lea       rax,[System.Reflection.CustomAttributeExtensions.GetCustomAttribute[[System.__Canon, System.Private.CoreLib]](System.Reflection.Assembly)]
       add       rsp,30
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       jmp       qword ptr [rax]
M19_L08:
       cmp       r8,19
       jne       short M19_L09
       movups    xmm0,[rdx]
       movups    xmm1,[rdx+9]
       movups    [rcx],xmm0
       movups    [rcx+9],xmm1
       jmp       short M19_L05
M19_L09:
       call      qword ptr [7FF96E84D928]; Precode of System.SpanHelpers.Memmove(Byte ByRef, Byte ByRef, UIntPtr)
       jmp       short M19_L05
; Total bytes of code 243
```
```assembly
; AuroraScript.Runtime.Property.HiddenClass..ctor(AuroraScript.Runtime.Property.HiddenProperty[], UInt16, AuroraScript.Runtime.Property.HiddenClass, System.String, Boolean)
       push      rbp
       sub       rsp,20
       lea       rbp,[rsp+20]
       mov       [rbp+10],rcx
       mov       [rbp+18],rdx
       mov       [rbp+20],r8d
       mov       [rbp+28],r9
       mov       rcx,[rbp+10]
       call      qword ptr [7FF97782CB10]; System.Object..ctor()
       mov       rax,[rbp+10]
       lea       rcx,[rax+8]
       mov       rdx,[rbp+18]
       call      CORINFO_HELP_ASSIGN_REF
       mov       rax,[rbp+10]
       mov       ecx,[rbp+20]
       mov       [rax+38],cx
       mov       rax,[rbp+10]
       lea       rcx,[rax+20]
       mov       rdx,[rbp+28]
       call      CORINFO_HELP_ASSIGN_REF
       mov       rax,[rbp+10]
       lea       rcx,[rax+28]
       mov       rdx,[rbp+30]
       call      CORINFO_HELP_ASSIGN_REF
       mov       rax,[rbp+10]
       mov       ecx,[rbp+38]
       mov       [rax+3A],cl
       mov       rcx,[rbp+18]
       call      qword ptr [7FF9779FEAA8]; AuroraScript.Runtime.Property.HiddenClass.CreateEnumerableKeys(AuroraScript.Runtime.Property.HiddenProperty[])
       mov       rcx,[rbp+10]
       lea       rcx,[rcx+30]
       mov       rdx,rax
       call      CORINFO_HELP_ASSIGN_REF
       nop
       add       rsp,20
       pop       rbp
       ret
; Total bytes of code 141
```
```assembly
; System.Collections.Generic.Dictionary`2[[AuroraScript.Runtime.Property.TransitionKey, AuroraScript],[System.__Canon, System.Private.CoreLib]]..ctor(Int32, System.Collections.Generic.IEqualityComparer`1<AuroraScript.Runtime.Property.TransitionKey>)
       push      rbp
       sub       rsp,30
       lea       rbp,[rsp+30]
       xor       eax,eax
       mov       [rbp-8],rax
       mov       [rbp+10],rcx
       mov       [rbp+18],edx
       mov       [rbp+20],r8
       mov       rcx,[rbp+10]
       call      qword ptr [7FF97782CB10]; System.Object..ctor()
       cmp       dword ptr [rbp+18],0
       jge       short M21_L00
       mov       ecx,16
       call      qword ptr [7FF97782F348]
M21_L00:
       cmp       dword ptr [rbp+18],0
       jle       short M21_L01
       mov       rcx,[rbp+10]
       mov       edx,[rbp+18]
       call      qword ptr [7FF9779FEAF0]; System.Collections.Generic.Dictionary`2[[AuroraScript.Runtime.Property.TransitionKey, AuroraScript],[System.__Canon, System.Private.CoreLib]].Initialize(Int32)
M21_L01:
       cmp       qword ptr [rbp+20],0
       je        short M21_L02
       call      qword ptr [7FF9779FEB08]; System.Collections.Generic.EqualityComparer`1[[AuroraScript.Runtime.Property.TransitionKey, AuroraScript]].get_Default()
       cmp       rax,[rbp+20]
       je        short M21_L02
       mov       rax,[rbp+10]
       lea       rcx,[rax+18]
       mov       rdx,[rbp+20]
       call      CORINFO_HELP_ASSIGN_REF
M21_L02:
       nop
       add       rsp,30
       pop       rbp
       ret
; Total bytes of code 116
```
```assembly
; System.Collections.Generic.Dictionary`2[[AuroraScript.Runtime.Property.TransitionKey, AuroraScript],[System.__Canon, System.Private.CoreLib]].TryInsert(AuroraScript.Runtime.Property.TransitionKey, System.__Canon, System.Collections.Generic.InsertionBehavior)
       push      rbp
       push      rdi
       push      rsi
       sub       rsp,130
       lea       rbp,[rsp+140]
       vxorps    xmm4,xmm4,xmm4
       vmovdqa   xmmword ptr [rbp-120],xmm4
       vmovdqa   xmmword ptr [rbp-110],xmm4
       mov       rax,0FFFFFFFFFFFFFF40
M22_L00:
       vmovdqa   xmmword ptr [rbp+rax-40],xmm4
       vmovdqa   xmmword ptr [rbp+rax-30],xmm4
       vmovdqa   xmmword ptr [rbp+rax-20],xmm4
       add       rax,30
       jne       short M22_L00
       mov       [rbp-40],rcx
       mov       [rbp+10],rcx
       mov       [rbp+18],rdx
       mov       [rbp+20],r8
       mov       [rbp+28],r9d
       mov       dword ptr [rbp-98],3E8
       mov       rax,[rbp+10]
       cmp       qword ptr [rax+8],0
       jne       short M22_L01
       mov       rcx,7FF977AC2880
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rcx,[rbp+10]
       xor       edx,edx
       call      qword ptr [7FF9779FEAF0]; System.Collections.Generic.Dictionary`2[[AuroraScript.Runtime.Property.TransitionKey, AuroraScript],[System.__Canon, System.Private.CoreLib]].Initialize(Int32)
M22_L01:
       mov       rax,[rbp+10]
       mov       rax,[rax+10]
       mov       [rbp-48],rax
       mov       rax,[rbp+10]
       mov       rax,[rax+18]
       mov       [rbp-50],rax
       cmp       qword ptr [rbp-50],0
       je        short M22_L02
       mov       rcx,7FF977AC2884
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rax,[rbp-50]
       mov       [rbp-80],rax
       mov       rcx,[rbp-80]
       mov       rdx,7FF977AC2888
       call      CORINFO_HELP_CLASSPROFILE32
       mov       rax,[rbp-80]
       mov       [rbp-0B0],rax
       mov       rax,[rbp+18]
       vmovdqu   xmm0,xmmword ptr [rax]
       vmovdqu   xmmword ptr [rbp-0A8],xmm0
       mov       rcx,[rbp-0B0]
       lea       rdx,[rbp-0A8]
       mov       r11,7FF977770168
       call      qword ptr [r11]
       mov       [rbp-70],eax
       jmp       short M22_L03
M22_L02:
       mov       rcx,7FF977AC2990
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rcx,[rbp+18]
       call      qword ptr [7FF9779FEC10]; AuroraScript.Runtime.Property.TransitionKey.GetHashCode()
       mov       [rbp-70],eax
M22_L03:
       mov       eax,[rbp-70]
       mov       [rbp-54],eax
       xor       eax,eax
       mov       [rbp-58],eax
       mov       rcx,[rbp+10]
       mov       edx,[rbp-54]
       call      qword ptr [7FF9779FEC28]; System.Collections.Generic.Dictionary`2[[AuroraScript.Runtime.Property.TransitionKey, AuroraScript],[System.__Canon, System.Private.CoreLib]].GetBucket(UInt32)
       mov       [rbp-60],rax
       mov       rax,[rbp-60]
       mov       eax,[rax]
       dec       eax
       mov       [rbp-64],eax
       cmp       qword ptr [rbp-50],0
       jne       short M22_L04
       jmp       near ptr M22_L10
M22_L04:
       mov       rcx,7FF977AC2994
       call      CORINFO_HELP_COUNTPROFILE32
       jmp       near ptr M22_L19
M22_L05:
       mov       rax,[rbp-48]
       mov       ecx,[rbp-64]
       cmp       ecx,[rax+8]
       jae       near ptr M22_L27
       mov       edx,ecx
       imul      rdx,20
       lea       rax,[rax+rdx+10]
       mov       eax,[rax+8]
       cmp       eax,[rbp-54]
       jne       near ptr M22_L09
       call      qword ptr [7FF9779FEB08]; System.Collections.Generic.EqualityComparer`1[[AuroraScript.Runtime.Property.TransitionKey, AuroraScript]].get_Default()
       mov       [rbp-88],rax
       mov       rcx,[rbp-88]
       mov       rdx,7FF977AC2998
       call      CORINFO_HELP_CLASSPROFILE32
       mov       rax,[rbp-88]
       mov       [rbp-0D8],rax
       mov       rax,[rbp-48]
       mov       ecx,[rbp-64]
       cmp       ecx,[rax+8]
       jae       near ptr M22_L27
       mov       edx,ecx
       imul      rdx,20
       lea       rax,[rax+rdx+10]
       vmovdqu   xmm0,xmmword ptr [rax+10]
       vmovdqu   xmmword ptr [rbp-0C0],xmm0
       mov       rax,[rbp+18]
       vmovdqu   xmm0,xmmword ptr [rax]
       vmovdqu   xmmword ptr [rbp-0D0],xmm0
       mov       rcx,[rbp-0D8]
       lea       rdx,[rbp-0C0]
       lea       r8,[rbp-0D0]
       mov       rax,[rbp-0D8]
       mov       rax,[rax]
       mov       rax,[rax+40]
       call      qword ptr [rax+20]
       test      eax,eax
       je        short M22_L06
       movzx     eax,byte ptr [rbp+28]
       cmp       eax,1
       jne       short M22_L07
       mov       rcx,7FF977AC2AA0
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rax,[rbp-48]
       mov       ecx,[rbp-64]
       cmp       ecx,[rax+8]
       jae       near ptr M22_L27
       mov       edx,ecx
       imul      rdx,20
       lea       rax,[rax+rdx+10]
       mov       rdx,[rbp+20]
       mov       rcx,rax
       call      CORINFO_HELP_CHECKED_ASSIGN_REF
       jmp       near ptr M22_L26
M22_L06:
       mov       rcx,7FF977AC2AA4
       call      CORINFO_HELP_COUNTPROFILE32
       jmp       short M22_L09
M22_L07:
       movzx     eax,byte ptr [rbp+28]
       cmp       eax,2
       jne       short M22_L08
       mov       rcx,7FF977AC2AA8
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rax,[rbp+18]
       vmovdqu   xmm0,xmmword ptr [rax]
       vmovdqu   xmmword ptr [rbp-0E8],xmm0
       lea       rcx,[rbp-0E8]
       call      qword ptr [7FF9779FEBF8]
M22_L08:
       mov       rcx,7FF977AC2AAC
       call      CORINFO_HELP_COUNTPROFILE32
       jmp       near ptr M22_L17
M22_L09:
       mov       rax,[rbp-48]
       mov       ecx,[rbp-64]
       cmp       ecx,[rax+8]
       jae       near ptr M22_L27
       mov       edx,ecx
       imul      rdx,20
       lea       rax,[rax+rdx+10]
       mov       eax,[rax+0C]
       mov       [rbp-64],eax
       mov       eax,[rbp-58]
       inc       eax
       mov       [rbp-58],eax
       mov       rax,[rbp-48]
       mov       eax,[rax+8]
       cmp       eax,[rbp-58]
       jae       short M22_L12
       mov       rcx,7FF977AC2AB0
       call      CORINFO_HELP_COUNTPROFILE32
       call      qword ptr [7FF97782F480]
M22_L10:
       mov       eax,[rbp-98]
       dec       eax
       mov       [rbp-98],eax
       cmp       dword ptr [rbp-98],0
       jg        short M22_L11
       lea       rcx,[rbp-98]
       mov       edx,0ED
       call      CORINFO_HELP_PATCHPOINT
M22_L11:
       mov       rax,[rbp-48]
       mov       eax,[rax+8]
       cmp       eax,[rbp-64]
       ja        near ptr M22_L05
       mov       rcx,7FF977AC2AB4
       call      CORINFO_HELP_COUNTPROFILE32
       jmp       near ptr M22_L21
M22_L12:
       mov       rcx,7FF977AC2AB8
       call      CORINFO_HELP_COUNTPROFILE32
       jmp       short M22_L10
M22_L13:
       mov       rax,[rbp-48]
       mov       ecx,[rbp-64]
       cmp       ecx,[rax+8]
       jae       near ptr M22_L27
       mov       edx,ecx
       imul      rdx,20
       lea       rax,[rax+rdx+10]
       mov       eax,[rax+8]
       cmp       eax,[rbp-54]
       jne       near ptr M22_L18
       mov       rax,[rbp-50]
       mov       [rbp-90],rax
       mov       rcx,[rbp-90]
       mov       rdx,7FF977AC2AC0
       call      CORINFO_HELP_CLASSPROFILE32
       mov       rax,[rbp-90]
       mov       [rbp-110],rax
       mov       rax,[rbp-48]
       mov       ecx,[rbp-64]
       cmp       ecx,[rax+8]
       jae       near ptr M22_L27
       mov       edx,ecx
       imul      rdx,20
       lea       rax,[rax+rdx+10]
       vmovdqu   xmm0,xmmword ptr [rax+10]
       vmovdqu   xmmword ptr [rbp-0F8],xmm0
       mov       rax,[rbp+18]
       vmovdqu   xmm0,xmmword ptr [rax]
       vmovdqu   xmmword ptr [rbp-108],xmm0
       mov       rcx,[rbp-110]
       lea       r8,[rbp-108]
       lea       rdx,[rbp-0F8]
       mov       r11,7FF977770160
       call      qword ptr [r11]
       test      eax,eax
       je        short M22_L14
       movzx     eax,byte ptr [rbp+28]
       cmp       eax,1
       jne       short M22_L15
       mov       rcx,7FF977AC2BC8
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rax,[rbp-48]
       mov       ecx,[rbp-64]
       cmp       ecx,[rax+8]
       jae       near ptr M22_L27
       mov       edx,ecx
       imul      rdx,20
       lea       rax,[rax+rdx+10]
       mov       rdx,[rbp+20]
       mov       rcx,rax
       call      CORINFO_HELP_CHECKED_ASSIGN_REF
       jmp       near ptr M22_L26
M22_L14:
       mov       rcx,7FF977AC2BCC
       call      CORINFO_HELP_COUNTPROFILE32
       jmp       short M22_L18
M22_L15:
       movzx     eax,byte ptr [rbp+28]
       cmp       eax,2
       jne       short M22_L16
       mov       rcx,7FF977AC2BD0
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rax,[rbp+18]
       vmovdqu   xmm0,xmmword ptr [rax]
       vmovdqu   xmmword ptr [rbp-120],xmm0
       lea       rcx,[rbp-120]
       call      qword ptr [7FF9779FEBF8]
M22_L16:
       mov       rcx,7FF977AC2BD4
       call      CORINFO_HELP_COUNTPROFILE32
M22_L17:
       xor       eax,eax
       add       rsp,130
       pop       rsi
       pop       rdi
       pop       rbp
       ret
M22_L18:
       mov       rax,[rbp-48]
       mov       ecx,[rbp-64]
       cmp       ecx,[rax+8]
       jae       near ptr M22_L27
       mov       edx,ecx
       imul      rdx,20
       lea       rax,[rax+rdx+10]
       mov       eax,[rax+0C]
       mov       [rbp-64],eax
       mov       eax,[rbp-58]
       inc       eax
       mov       [rbp-58],eax
       mov       rax,[rbp-48]
       mov       eax,[rax+8]
       cmp       eax,[rbp-58]
       jae       near ptr M22_L22
       mov       rcx,7FF977AC2BD8
       call      CORINFO_HELP_COUNTPROFILE32
       call      qword ptr [7FF97782F480]
M22_L19:
       mov       eax,[rbp-98]
       dec       eax
       mov       [rbp-98],eax
       cmp       dword ptr [rbp-98],0
       jg        short M22_L20
       lea       rcx,[rbp-98]
       mov       edx,15A
       call      CORINFO_HELP_PATCHPOINT
M22_L20:
       mov       rax,[rbp-48]
       mov       eax,[rax+8]
       cmp       eax,[rbp-64]
       ja        near ptr M22_L13
M22_L21:
       mov       rax,[rbp+10]
       cmp       dword ptr [rax+40],0
       jle       short M22_L23
       mov       rax,[rbp+10]
       mov       eax,[rax+3C]
       mov       [rbp-68],eax
       mov       rax,[rbp+10]
       mov       eax,[rax+3C]
       mov       rcx,[rbp-48]
       cmp       eax,[rcx+8]
       jae       near ptr M22_L27
       mov       edx,eax
       imul      rdx,20
       lea       rcx,[rcx+rdx+10]
       mov       eax,[rcx+0C]
       neg       eax
       add       eax,0FFFFFFFD
       mov       rcx,[rbp+10]
       mov       [rcx+3C],eax
       mov       rax,[rbp+10]
       dec       dword ptr [rax+40]
       jmp       near ptr M22_L25
M22_L22:
       mov       rcx,7FF977AC2BDC
       call      CORINFO_HELP_COUNTPROFILE32
       jmp       near ptr M22_L19
M22_L23:
       mov       rax,[rbp+10]
       mov       eax,[rax+38]
       mov       [rbp-6C],eax
       mov       rax,[rbp-48]
       mov       eax,[rax+8]
       cmp       eax,[rbp-6C]
       jne       short M22_L24
       mov       rcx,7FF977AC2BE0
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rcx,[rbp+10]
       call      qword ptr [7FF9779FEC58]
       mov       rcx,[rbp+10]
       mov       edx,[rbp-54]
       call      qword ptr [7FF9779FEC28]; System.Collections.Generic.Dictionary`2[[AuroraScript.Runtime.Property.TransitionKey, AuroraScript],[System.__Canon, System.Private.CoreLib]].GetBucket(UInt32)
       mov       [rbp-60],rax
M22_L24:
       mov       rcx,7FF977AC2BE4
       call      CORINFO_HELP_COUNTPROFILE32
       mov       eax,[rbp-6C]
       mov       [rbp-68],eax
       mov       eax,[rbp-6C]
       inc       eax
       mov       rcx,[rbp+10]
       mov       [rcx+38],eax
       mov       rax,[rbp+10]
       mov       rax,[rax+10]
       mov       [rbp-48],rax
M22_L25:
       mov       rax,[rbp-48]
       mov       ecx,[rbp-68]
       cmp       ecx,[rax+8]
       jae       short M22_L27
       mov       edx,ecx
       imul      rdx,20
       lea       rax,[rax+rdx+10]
       mov       [rbp-78],rax
       mov       rax,[rbp-78]
       mov       ecx,[rbp-54]
       mov       [rax+8],ecx
       mov       rax,[rbp-60]
       mov       eax,[rax]
       dec       eax
       mov       rcx,[rbp-78]
       mov       [rcx+0C],eax
       mov       rsi,[rbp+18]
       mov       rax,[rbp-78]
       lea       rdi,[rax+10]
       call      CORINFO_HELP_ASSIGN_BYREF
       movsq
       mov       rcx,[rbp-78]
       mov       rdx,[rbp+20]
       call      CORINFO_HELP_CHECKED_ASSIGN_REF
       mov       eax,[rbp-68]
       inc       eax
       mov       rcx,[rbp-60]
       mov       [rcx],eax
       mov       rax,[rbp+10]
       inc       dword ptr [rax+44]
       mov       rcx,7FF977AC2BE8
       call      CORINFO_HELP_COUNTPROFILE32
M22_L26:
       mov       eax,1
       add       rsp,130
       pop       rsi
       pop       rdi
       pop       rbp
       ret
M22_L27:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 1708
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
; System.Runtime.CompilerServices.CastHelpers.IsInstance_Helper(Void*, System.Object)
       push      rsi
       push      rbx
       sub       rsp,28
       mov       rbx,rcx
       mov       rsi,rdx
       call      qword ptr [7FF96E839C18]
       mov       rax,[rax]
       mov       rdx,[rsi]
       add       rax,10
       mov       r8,rdx
       rol       r8,20
       xor       r8,rbx
       mov       r10,9E3779B97F4A7C15
       imul      r8,r10
       mov       ecx,[rax]
       shr       r8,cl
       xor       ecx,ecx
M24_L00:
       lea       r10d,[r8+1]
       movsxd    r10,r10d
       lea       r10,[r10+r10*2]
       lea       r10,[rax+r10*8]
       mov       r9d,[r10]
       mov       r11,[r10+8]
       and       r9d,0FFFFFFFE
       cmp       r11,rdx
       jne       short M24_L02
       mov       r11,rbx
       xor       r11,[r10+10]
       cmp       r11,1
       ja        short M24_L02
       cmp       r9d,[r10]
       jne       short M24_L04
M24_L01:
       cmp       r11d,1
       jne       short M24_L03
       mov       rax,rsi
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
M24_L02:
       test      r9d,r9d
       je        short M24_L04
       inc       ecx
       add       r8d,ecx
       and       r8d,[rax+4]
       cmp       ecx,8
       jl        short M24_L00
       jmp       short M24_L04
M24_L03:
       test      r11d,r11d
       jne       short M24_L05
       xor       eax,eax
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
M24_L04:
       mov       r11d,2
       jmp       short M24_L01
M24_L05:
       mov       rcx,rbx
       mov       rdx,rsi
       call      qword ptr [7FF96E854CA8]; Precode of System.Runtime.CompilerServices.CastHelpers.IsInstanceOfAny_NoCacheLookup(Void*, System.Object)
       nop
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
; Total bytes of code 187
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
```assembly
; System.Array.Resize[[AuroraScript.Runtime.ScriptDatum, AuroraScript]](AuroraScript.Runtime.ScriptDatum[] ByRef, Int32)
       push      rbp
       sub       rsp,50
       lea       rbp,[rsp+50]
       vxorps    xmm4,xmm4,xmm4
       vmovdqu   ymmword ptr [rbp-20],ymm4
       mov       [rbp+10],rcx
       mov       [rbp+18],edx
       cmp       dword ptr [rbp+18],0
       jge       short M26_L00
       mov       ecx,45
       mov       edx,0D
       call      qword ptr [7FF9779FEE20]
M26_L00:
       mov       rax,[rbp+10]
       mov       rax,[rax]
       mov       [rbp-8],rax
       cmp       qword ptr [rbp-8],0
       jne       short M26_L01
       movsxd    rdx,dword ptr [rbp+18]
       mov       rcx,offset MT_AuroraScript.Runtime.ScriptDatum[]
       call      CORINFO_HELP_NEWARR_1_VC
       mov       rcx,[rbp+10]
       mov       rdx,rax
       call      CORINFO_HELP_CHECKED_ASSIGN_REF
       nop
       add       rsp,50
       pop       rbp
       ret
M26_L01:
       mov       rax,[rbp-8]
       mov       eax,[rax+8]
       cmp       eax,[rbp+18]
       je        short M26_L02
       movsxd    rdx,dword ptr [rbp+18]
       mov       rcx,offset MT_AuroraScript.Runtime.ScriptDatum[]
       call      CORINFO_HELP_NEWARR_1_VC
       mov       [rbp-10],rax
       mov       rcx,[rbp-10]
       call      qword ptr [7FF9779FEDC0]; System.Runtime.InteropServices.MemoryMarshal.GetArrayDataReference[[AuroraScript.Runtime.ScriptDatum, AuroraScript]](AuroraScript.Runtime.ScriptDatum[])
       mov       [rbp-18],rax
       mov       rcx,[rbp-8]
       call      qword ptr [7FF9779FEDC0]; System.Runtime.InteropServices.MemoryMarshal.GetArrayDataReference[[AuroraScript.Runtime.ScriptDatum, AuroraScript]](AuroraScript.Runtime.ScriptDatum[])
       mov       [rbp-20],rax
       mov       rax,[rbp-8]
       mov       edx,[rax+8]
       mov       ecx,[rbp+18]
       call      qword ptr [7FF9779FEE38]; System.Math.Min(Int32, Int32)
       mov       eax,eax
       mov       [rbp-28],rax
       mov       r8,[rbp-28]
       mov       rdx,[rbp-20]
       mov       rcx,[rbp-18]
       call      qword ptr [7FF9779FEDF0]; System.Buffer.Memmove[[AuroraScript.Runtime.ScriptDatum, AuroraScript]](AuroraScript.Runtime.ScriptDatum ByRef, AuroraScript.Runtime.ScriptDatum ByRef, UIntPtr)
       mov       rcx,[rbp+10]
       mov       rdx,[rbp-10]
       call      CORINFO_HELP_CHECKED_ASSIGN_REF
M26_L02:
       nop
       add       rsp,50
       pop       rbp
       ret
; Total bytes of code 227
```
```assembly
; System.Array.Resize[[System.__Canon, System.Private.CoreLib]](System.__Canon[] ByRef, Int32)
       push      r14
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,30
       mov       [rsp+28],rcx
       mov       rsi,rdx
       mov       ebx,r8d
       test      ebx,ebx
       jl        short M27_L03
       mov       rdi,[rsi]
       test      rdi,rdi
       je        short M27_L04
       mov       ebp,[rdi+8]
       cmp       ebp,ebx
       je        short M27_L02
       call      qword ptr [7FF96E83ED58]
       mov       rcx,rax
       mov       edx,ebx
       call      qword ptr [7FF96E839088]; CORINFO_HELP_NEWARR_1_DIRECT
       mov       r14,rax
       lea       rcx,[r14+10]
       lea       rdx,[rdi+10]
       cmp       ebx,ebp
       cmovg     ebx,ebp
       mov       r8d,ebx
       shl       r8,3
       cmp       r8,4000
       jbe       short M27_L00
       call      qword ptr [7FF96E84A668]
       jmp       short M27_L01
M27_L00:
       call      qword ptr [7FF96E84A630]
       mov       rax,[System.Reflection.CustomAttributeExtensions.GetCustomAttribute[[System.__Canon, System.Private.CoreLib]](System.Reflection.Assembly)]
       cmp       dword ptr [rax],0
       jne       short M27_L05
M27_L01:
       mov       rcx,rsi
       mov       rdx,r14
       call      qword ptr [7FF96E838FF0]; CORINFO_HELP_CHECKED_ASSIGN_REF
M27_L02:
       nop
       add       rsp,30
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       ret
M27_L03:
       mov       ecx,45
       mov       edx,0D
       call      qword ptr [7FF96E84F3E0]
       int       3
M27_L04:
       call      qword ptr [7FF96E83ED58]
       mov       rcx,rax
       mov       edx,ebx
       call      qword ptr [7FF96E839088]; CORINFO_HELP_NEWARR_1_DIRECT
       mov       rdx,rax
       mov       rcx,rsi
       call      qword ptr [7FF96E838FF0]; CORINFO_HELP_CHECKED_ASSIGN_REF
       jmp       short M27_L02
M27_L05:
       call      qword ptr [7FF96E839040]; CORINFO_HELP_POLL_GC
       jmp       short M27_L01
; Total bytes of code 195
```
```assembly
; System.Number.FormatFloat[[System.Double, System.Private.CoreLib],[System.Char, System.Private.CoreLib]](System.Collections.Generic.ValueListBuilder`1<Char> ByRef, Double, System.ReadOnlySpan`1<Char>, System.Globalization.NumberFormatInfo)
       push      rbp
       push      r15
       push      r14
       push      r13
       push      r12
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,168
       vmovaps   [rsp+150],xmm6
       lea       rbp,[rsp+40]
       xor       eax,eax
       mov       [rbp+18],rax
       vxorps    xmm4,xmm4,xmm4
       vmovdqa   xmmword ptr [rbp+20],xmm4
       vmovdqa   xmmword ptr [rbp+30],xmm4
       mov       rax,0FFFFFFFFFFFFFF40
M28_L00:
       vmovdqa   xmmword ptr [rbp+rax+100],xmm4
       vmovdqa   xmmword ptr [rbp+rax+110],xmm4
       vmovdqa   xmmword ptr [rbp+rax+120],xmm4
       add       rax,30
       jne       short M28_L00
       mov       [rbp+100],rax
       mov       rax,5275BDC81ABA
       mov       [rbp+8],rax
       mov       rdi,rcx
       vmovaps   xmm6,xmm1
       mov       rbx,r8
       mov       rsi,r9
       vmovq     rax,xmm6
       mov       rcx,7FF0000000000000
       andn      rax,rax,rcx
       je        near ptr M28_L19
       mov       r14,[rbx]
       mov       r15d,[rbx+8]
       xor       ecx,ecx
       test      r15d,r15d
       jg        near ptr M28_L26
M28_L01:
       mov       dword ptr [rbp+108],0FFFFFFFF
       test      r15d,r15d
       jne       near ptr M28_L36
M28_L02:
       mov       ecx,47
M28_L03:
       mov       r14d,ecx
       test      [rsp],esp
       sub       rsp,310
       lea       r9,[rsp+40]
       mov       edx,0F
       test      ecx,ecx
       cmovne    edx,[rbp+108]
       mov       [rbp+108],edx
       xor       edx,edx
       mov       [rbp+0E8],edx
       mov       [rbp+0EC],edx
       mov       byte ptr [rbp+0F0],0
       mov       byte ptr [rbp+0F1],0
       mov       byte ptr [rbp+0F2],3
       mov       [rbp+0F8],r9
       mov       dword ptr [rbp+100],301
       cmp       dword ptr [rbp+100],0
       jbe       near ptr M28_L49
       mov       r9,[rbp+0F8]
       mov       byte ptr [r9],0
       vmovq     r9,xmm6
       shr       r9,3F
       mov       [rbp+0F0],r9b
       lea       r9,[rbp+0D8]
       lea       rdx,[rbp+108]
       mov       r8,rsi
       call      qword ptr [7FF977BA7138]; System.Number.GetFloatingPointMaxDigitsAndPrecision(Char, Int32 ByRef, System.Globalization.NumberFormatInfo, Boolean ByRef)
       mov       [rbp+0E4],eax
       vxorps    xmm0,xmm0,xmm0
       vucomisd  xmm6,xmm0
       jp        short M28_L04
       je        near ptr M28_L15
M28_L04:
       cmp       byte ptr [rbp+0D8],0
       je        near ptr M28_L47
       mov       r13d,[rbp+108]
       vmovq     rax,xmm6
       test      rax,rax
       jl        near ptr M28_L37
       vmovaps   xmm0,xmm6
M28_L05:
       cmp       r13d,0FFFFFFFF
       jne       near ptr M28_L46
       vmovq     rax,xmm0
       mov       rdx,0FFFFFFFFFFFFF
       and       rdx,rax
       shr       rax,34
       and       eax,7FF
       je        near ptr M28_L38
       mov       rcx,10000000000000
       or        rdx,rcx
       add       eax,0FFFFFBCD
M28_L06:
       lea       rcx,[rdx*2+1]
       lea       r8d,[rax-1]
       xor       r10d,r10d
       lzcnt     r10,rcx
       shlx      rcx,rcx,r10
       sub       r8d,r10d
       mov       r10,10000000000000
       cmp       rdx,r10
       je        near ptr M28_L39
       lea       r10,[rdx*2-1]
       lea       r9d,[rax-1]
M28_L07:
       sub       r9d,r8d
       shlx      r10,r10,r9
       xor       r9d,r9d
       lzcnt     r9,rdx
       shlx      rdx,rdx,r9
       sub       eax,r9d
       mov       r13,[rbp+0F8]
       mov       [rbp+10],r13
       mov       r12d,[rbp+100]
       mov       [rbp+34],r12d
       lea       r9d,[rax+40]
       neg       r9d
       add       r9d,3
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2sd xmm0,xmm0,r9d
       vmulsd    xmm0,xmm0,qword ptr [7FF9778A6950]
       vroundsd  xmm0,xmm0,xmm0,0A
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       r9d,7FFFFFFF
       vcvttsd2si r11d,xmm1
       vucomisd  xmm0,qword ptr [7FF9778A6958]
       cmovb     r9d,r11d
       add       r9d,15B
       mov       r11d,r9d
       sar       r11d,1F
       and       r11d,7
       add       r9d,r11d
       sar       r9d,3
       inc       r9d
       cmp       r9d,57
       jae       near ptr M28_L49
       mov       r11d,r9d
       mov       r12,7FF96DBD5658
       movsx     r12,word ptr [r12+r11*2]
       mov       [rbp+0A4],r12d
       mov       r11d,r9d
       mov       r12,7FF96DBC4128
       mov       r11,[r12+r11*8]
       mov       r12,7FF96DBCCF40
       movsx     r9,word ptr [r12+r9*2]
       mov       r12d,edx
       mov       r13,r11
       shr       r13,20
       mov       [rbp+88],r11d
       shr       rdx,20
       mov       r11d,edx
       mov       r13d,r13d
       imul      r11,r13
       mov       [rbp+80],r11
       mov       r11,r12
       imul      r11,r13
       mov       edx,edx
       mov       r15d,[rbp+88]
       imul      rdx,r15
       mov       [rbp+78],rdx
       imul      r12,r15
       shr       r12,20
       mov       edx,edx
       add       rdx,r12
       mov       r12d,r11d
       add       rdx,r12
       mov       r12d,80000000
       add       rdx,r12
       lea       eax,[rax+r9+40]
       mov       r12,[rbp+78]
       shr       r12,20
       add       r12,[rbp+80]
       shr       r11,20
       add       r11,r12
       shr       rdx,20
       add       rdx,r11
       mov       [rbp+90],rdx
       mov       [rbp+98],eax
       mov       eax,r10d
       shr       r10,20
       mov       edx,r10d
       imul      rdx,r13
       mov       r11,rax
       imul      r11,r13
       mov       r10d,r10d
       imul      r10,r15
       imul      rax,r15
       shr       rax,20
       mov       r12d,r10d
       add       rax,r12
       mov       r12d,r11d
       add       rax,r12
       mov       r12d,80000000
       add       rax,r12
       shr       r10,20
       add       rdx,r10
       shr       r11,20
       add       rdx,r11
       shr       rax,20
       add       rax,rdx
       mov       edx,ecx
       shr       rcx,20
       mov       r10d,ecx
       imul      r10,r13
       imul      r13,rdx
       mov       ecx,ecx
       imul      rcx,r15
       imul      rdx,r15
       shr       rdx,20
       mov       r11d,ecx
       add       rdx,r11
       mov       r11d,r13d
       add       rdx,r11
       add       rdx,r12
       lea       r8d,[r8+r9+40]
       shr       rcx,20
       add       rcx,r10
       shr       r13,20
       add       rcx,r13
       shr       rdx,20
       add       rdx,rcx
       mov       r13,[rbp+10]
       mov       r12d,[rbp+34]
       mov       qword ptr [rbp+70],1
       dec       rax
       inc       rdx
       mov       [rbp+60],rdx
       mov       [rbp+68],r8d
       mov       rcx,[rbp+60]
       sub       rcx,rax
       mov       eax,[rbp+98]
       neg       eax
       mov       r8d,eax
       and       r8d,3F
       mov       edx,1
       shlx      r10,rdx,r8
       mov       [rbp+28],r10
       shrx      r9,[rbp+60],r8
       lea       r11,[r10-1]
       and       r11,[rbp+60]
       neg       eax
       add       eax,41
       imul      eax,4D1
       sar       eax,0C
       cmp       eax,0A
       jae       near ptr M28_L49
       mov       edx,eax
       mov       r15,7FF96DBD36D0
       mov       r15d,[r15+rdx*4]
       cmp       r9d,r15d
       jae       short M28_L08
       dec       eax
       cmp       eax,0A
       jae       near ptr M28_L49
       mov       edx,eax
       mov       r15,7FF96DBD36D0
       mov       r15d,[r15+rdx*4]
M28_L08:
       lea       edx,[rax+1]
       xor       eax,eax
       mov       [rbp+0B0],eax
       mov       [rbp+8C],edx
       test      edx,edx
       jle       near ptr M28_L45
M28_L09:
       mov       eax,r9d
       xor       edx,edx
       div       r15d
       mov       edx,eax
       imul      edx,r15d
       sub       r9d,edx
       mov       edx,[rbp+0B0]
       cmp       edx,r12d
       jae       near ptr M28_L49
       add       eax,30
       mov       [rdx+r13],al
       mov       edx,[rbp+0B0]
       inc       edx
       mov       [rbp+0B0],edx
       mov       edx,[rbp+8C]
       dec       edx
       mov       eax,r9d
       shlx      rax,rax,r8
       add       rax,r11
       cmp       rax,rcx
       jae       near ptr M28_L40
       mov       r11d,[rbp+0B0]
       mov       r10,[rbp+60]
       sub       r10,[rbp+90]
       mov       r9d,r15d
       shlx      r8,r9,r8
       mov       r9,r10
       sub       r9,1
       inc       r10
M28_L10:
       cmp       rax,r9
       jb        near ptr M28_L42
M28_L11:
       cmp       rax,r10
       jae       short M28_L12
       mov       r9,rcx
       sub       r9,rax
       cmp       r9,r8
       jae       near ptr M28_L43
M28_L12:
       cmp       rax,2
       jb        near ptr M28_L44
       sub       rcx,4
       cmp       rcx,rax
       setae     al
       movzx     eax,al
M28_L13:
       sub       edx,[rbp+0A4]
       mov       [rbp+0A8],edx
       movzx     r15d,al
M28_L14:
       test      r15d,r15d
       je        near ptr M28_L47
       mov       r8d,[rbp+0B0]
       add       r8d,[rbp+0A8]
       mov       [rbp+0EC],r8d
       mov       r8d,[rbp+100]
       cmp       [rbp+0B0],r8d
       jae       near ptr M28_L49
       mov       r8,[rbp+0F8]
       mov       r9d,[rbp+0B0]
       mov       byte ptr [r8+r9],0
       mov       r8d,[rbp+0B0]
       mov       [rbp+0E8],r8d
M28_L15:
       test      r14d,r14d
       je        near ptr M28_L48
       cmp       dword ptr [rbp+108],0FFFFFFFF
       mov       r15d,[rbp+0E4]
       jne       short M28_L16
       mov       r15d,[rbp+0E8]
       mov       edx,11
       cmp       r15d,11
       cmovl     r15d,edx
M28_L16:
       mov       [rsp+20],rsi
       lea       rdx,[rbp+0E8]
       mov       rcx,rdi
       mov       r8d,r14d
       mov       r9d,r15d
       call      qword ptr [7FF977C25FB0]; System.Number.NumberToString[[System.Char, System.Private.CoreLib]](System.Collections.Generic.ValueListBuilder`1<Char> ByRef, NumberBuffer ByRef, Char, Int32, System.Globalization.NumberFormatInfo)
M28_L17:
       xor       eax,eax
       mov       r8,5275BDC81ABA
       cmp       [rbp+8],r8
       je        short M28_L18
       call      CORINFO_HELP_FAIL_FAST
M28_L18:
       nop
       vmovaps   xmm6,[rbp+110]
       lea       rsp,[rbp+128]
       pop       rbx
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       pop       rbp
       ret
M28_L19:
       vucomisd  xmm6,xmm6
       jp        short M28_L20
       je        short M28_L22
M28_L20:
       mov       rax,[rsi+58]
       mov       r8,5275BDC81ABA
       cmp       [rbp+8],r8
       je        short M28_L21
       call      CORINFO_HELP_FAIL_FAST
M28_L21:
       nop
       vmovaps   xmm6,[rbp+110]
       lea       rsp,[rbp+128]
       pop       rbx
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       pop       rbp
       ret
M28_L22:
       vmovq     rax,xmm6
       test      rax,rax
       jl        short M28_L24
       mov       rax,[rsi+60]
       mov       r8,5275BDC81ABA
       cmp       [rbp+8],r8
       je        short M28_L23
       call      CORINFO_HELP_FAIL_FAST
M28_L23:
       nop
       vmovaps   xmm6,[rbp+110]
       lea       rsp,[rbp+128]
       pop       rbx
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       pop       rbp
       ret
M28_L24:
       mov       rax,[rsi+68]
       mov       r8,5275BDC81ABA
       cmp       [rbp+8],r8
       je        short M28_L25
       call      CORINFO_HELP_FAIL_FAST
M28_L25:
       nop
       vmovaps   xmm6,[rbp+110]
       lea       rsp,[rbp+128]
       pop       rbx
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       pop       rbp
       ret
M28_L26:
       movzx     ecx,word ptr [r14]
       mov       eax,ecx
       or        eax,20
       add       eax,0FFFFFF9F
       cmp       eax,19
       ja        near ptr M28_L01
       cmp       r15d,1
       jne       short M28_L28
       mov       dword ptr [rbp+108],0FFFFFFFF
M28_L27:
       jmp       near ptr M28_L03
M28_L28:
       cmp       r15d,2
       jne       short M28_L29
       movzx     eax,word ptr [r14+2]
       add       eax,0FFFFFFD0
       cmp       eax,0A
       jae       short M28_L30
       mov       [rbp+108],eax
       jmp       short M28_L27
M28_L29:
       cmp       r15d,3
       jne       short M28_L30
       movzx     eax,word ptr [r14+2]
       add       eax,0FFFFFFD0
       movzx     edx,word ptr [r14+4]
       add       edx,0FFFFFFD0
       cmp       eax,0A
       jae       short M28_L30
       cmp       edx,0A
       jae       short M28_L30
       lea       eax,[rax+rax*4]
       lea       eax,[rdx+rax*2]
       mov       [rbp+108],eax
       jmp       short M28_L27
M28_L30:
       xor       r13d,r13d
       mov       eax,1
       jmp       short M28_L32
M28_L31:
       cmp       r13d,5F5E100
       jge       short M28_L33
       lea       edx,[rax+1]
       mov       eax,eax
       movzx     eax,word ptr [r14+rax*2]
       lea       r8d,[r13+r13*4]
       lea       r13d,[rax+r8*2-30]
       mov       eax,edx
M28_L32:
       cmp       eax,r15d
       jae       short M28_L35
       mov       edx,eax
       movzx     edx,word ptr [r14+rdx*2]
       add       edx,0FFFFFFD0
       cmp       edx,9
       jbe       short M28_L31
       jmp       short M28_L34
M28_L33:
       call      qword ptr [7FF977C2C2D0]
       int       3
M28_L34:
       mov       r9d,eax
       cmp       word ptr [r14+r9*2],0
       jne       near ptr M28_L01
M28_L35:
       mov       [rbp+108],r13d
       jmp       near ptr M28_L27
M28_L36:
       test      ecx,ecx
       je        near ptr M28_L02
       xor       ecx,ecx
       jmp       near ptr M28_L03
M28_L37:
       vxorps    xmm0,xmm6,[7FF9778A6960]
       jmp       near ptr M28_L05
M28_L38:
       mov       eax,0FFFFFBCE
       jmp       near ptr M28_L06
M28_L39:
       lea       r9d,[rax-2]
       mov       r10,3FFFFFFFFFFFFF
       jmp       near ptr M28_L07
M28_L40:
       mov       eax,0CCCCCCCD
       mov       r15d,r15d
       imul      r15,rax
       shr       r15,23
       test      edx,edx
       mov       [rbp+8C],edx
       jg        near ptr M28_L09
       jmp       short M28_L45
M28_L41:
       lea       r15d,[r11-1]
       cmp       r15d,r12d
       jae       near ptr M28_L49
       lea       r15d,[r11-1]
       add       r15,r13
       dec       byte ptr [r15]
       add       rax,r8
       jmp       near ptr M28_L10
M28_L42:
       mov       r15,rcx
       sub       r15,rax
       cmp       r15,r8
       jb        near ptr M28_L11
       lea       r15,[rax+r8]
       cmp       r15,r9
       jb        short M28_L41
       mov       r15,r9
       sub       r15,rax
       mov       [rbp],r15
       lea       r15,[rax+r8]
       sub       r15,r9
       cmp       [rbp],r15
       jae       short M28_L41
       jmp       near ptr M28_L11
M28_L43:
       lea       r9,[rax+r8]
       cmp       r9,r10
       jb        short M28_L44
       mov       r9,r10
       sub       r9,rax
       add       r8,rax
       sub       r8,r10
       cmp       r9,r8
       jbe       near ptr M28_L12
M28_L44:
       xor       eax,eax
       jmp       near ptr M28_L13
M28_L45:
       lea       r11,[r11+r11*4]
       add       r11,r11
       mov       r15,r11
       mov       rax,[rbp+70]
       lea       rax,[rax+rax*4]
       add       rax,rax
       mov       [rbp+70],rax
       lea       r9,[rcx+rcx*4]
       add       r9,r9
       mov       rcx,r9
       shrx      r11,r15,r8
       mov       eax,[rbp+0B0]
       cmp       eax,r12d
       jae       near ptr M28_L49
       add       r11d,30
       mov       [rax+r13],r11b
       mov       eax,[rbp+0B0]
       inc       eax
       mov       [rbp+0B0],eax
       mov       edx,[rbp+8C]
       dec       edx
       mov       [rbp+8C],edx
       lea       r11,[r10-1]
       and       r15,r11
       mov       [rbp+58],r15
       cmp       r15,rcx
       mov       [rbp+40],r9
       jae       near ptr M28_L50
       mov       r15d,[rbp+0B0]
       lea       rcx,[rbp+60]
       lea       rdx,[rbp+48]
       lea       r8,[rbp+90]
       call      qword ptr [7FF977C2CAC8]
       mov       [rbp+18],r13
       mov       [rbp+20],r12d
       mov       r13,[rbp+58]
       mov       [rsp+20],r13
       mov       r13,[rbp+28]
       mov       [rsp+28],r13
       mov       rax,[rbp+70]
       mov       [rsp+30],rax
       lea       rcx,[rbp+18]
       mov       r8,rax
       imul      r8,[rbp+48]
       mov       edx,r15d
       mov       r9,[rbp+40]
       call      qword ptr [7FF977C25F98]; System.Number+Grisu3.TryRoundWeedShortest(System.Span`1<Byte>, Int32, UInt64, UInt64, UInt64, UInt64, UInt64)
       mov       edx,[rbp+8C]
       jmp       near ptr M28_L13
M28_L46:
       lea       rdx,[rbp+38]
       call      qword ptr [7FF977BA7168]; System.Number.ExtractFractionAndBiasedExponent[[System.Double, System.Private.CoreLib]](Double, Int32 ByRef)
       mov       ecx,[rbp+38]
       mov       [rbp+0C8],rax
       mov       [rbp+0D0],ecx
       lea       rcx,[rbp+0C8]
       lea       rdx,[rbp+0B8]
       call      qword ptr [7FF977BA7180]; System.Number+DiyFp.Normalize()
       vmovdqu   xmm0,xmmword ptr [rbp+0F8]
       vmovdqu   xmmword ptr [rbp+18],xmm0
       lea       r8,[rbp+0A8]
       mov       [rsp+20],r8
       lea       r8,[rbp+18]
       lea       rcx,[rbp+0B8]
       lea       r9,[rbp+0B0]
       mov       edx,r13d
       call      qword ptr [7FF977BA7198]; System.Number+Grisu3.TryRunCounted(DiyFp ByRef, Int32, System.Span`1<Byte>, Int32 ByRef, Int32 ByRef)
       mov       r15d,eax
       jmp       near ptr M28_L14
M28_L47:
       movzx     r8d,byte ptr [rbp+0D8]
       lea       r9,[rbp+0E8]
       vmovaps   xmm0,xmm6
       mov       edx,[rbp+108]
       call      qword ptr [7FF977BA7240]; System.Number.Dragon4[[System.Double, System.Private.CoreLib]](Double, Int32, Boolean, NumberBuffer ByRef)
       jmp       near ptr M28_L15
M28_L48:
       lea       rdx,[rbp+0E8]
       mov       rcx,rdi
       mov       r8,rbx
       mov       r9,rsi
       call      qword ptr [7FF977BA7318]; System.Number.NumberToStringFormat[[System.Char, System.Private.CoreLib]](System.Collections.Generic.ValueListBuilder`1<Char> ByRef, NumberBuffer ByRef, System.ReadOnlySpan`1<Char>, System.Globalization.NumberFormatInfo)
       jmp       near ptr M28_L17
M28_L49:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
M28_L50:
       mov       r10,[rbp+28]
       mov       r11,[rbp+58]
       jmp       near ptr M28_L45
; Total bytes of code 2595
```
```assembly
; System.String.Ctor(System.ReadOnlySpan`1<Char>)
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,20
       mov       rbx,[rcx]
       mov       esi,[rcx+8]
       test      esi,esi
       je        short M29_L00
       mov       edx,esi
       mov       rcx,offset MT_System.String
       call      00007FF9D74C4E80
       mov       rdi,rax
       cmp       [rdi],dil
       lea       rcx,[rdi+0C]
       mov       r8d,esi
       add       r8,r8
       mov       rdx,rbx
       call      qword ptr [7FF977825818]; System.SpanHelpers.Memmove(Byte ByRef, Byte ByRef, UIntPtr)
       mov       rax,rdi
       add       rsp,20
       pop       rbx
       pop       rsi
       pop       rdi
       ret
M29_L00:
       mov       rax,0DA6EC60008
       add       rsp,20
       pop       rbx
       pop       rsi
       pop       rdi
       ret
; Total bytes of code 88
```
```assembly
; System.String.FastAllocateString(IntPtr)
       mov       rdx,rcx
       mov       rcx,offset MT_System.String
       jmp       near ptr 00007FF9D74C4E80
; Total bytes of code 18
```
```assembly
; System.Number.UInt32ToDecStrForKnownSmallNumber(UInt32)
       push      rsi
       push      rbx
       sub       rsp,28
       mov       ebx,ecx
       mov       rcx,0BA3C000A18
       mov       rcx,[rcx]
       cmp       ebx,12C
       jae       short M31_L01
       mov       eax,ebx
       mov       rsi,[rcx+rax*8+10]
       test      rsi,rsi
       je        short M31_L00
       mov       rcx,7FF977CF5580
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rax,rsi
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
M31_L00:
       mov       rcx,7FF977CF5584
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rcx,7FF977CF5580
       call      CORINFO_HELP_COUNTPROFILE32
       mov       ecx,ebx
       add       rsp,28
       pop       rbx
       pop       rsi
       jmp       qword ptr [7FF977BA55A8]; System.Number.<UInt32ToDecStrForKnownSmallNumber>g__CreateAndCacheString|50_0(UInt32)
M31_L01:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 116
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
       je        short M32_L00
       mov       rcx,rsi
       cmp       [r8],rcx
       je        short M32_L00
       mov       rdx,rax
       call      qword ptr [7FF96E839090]; Precode of System.Runtime.CompilerServices.CastHelpers.ChkCastAny(Void*, System.Object)
       mov       r8,rax
M32_L00:
       mov       rax,r8
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
; Total bytes of code 88
```
```assembly
; System.Runtime.CompilerServices.CastHelpers.ChkCastClassSpecial(Void*, System.Object)
       mov       rax,[rdx]
       mov       rax,[rax+10]
       cmp       rax,rcx
       jne       short M33_L01
M33_L00:
       mov       rax,rdx
       ret
M33_L01:
       test      rax,rax
       je        short M33_L04
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M33_L00
       jmp       short M33_L03
M33_L02:
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M33_L00
       jmp       short M33_L01
M33_L03:
       test      rax,rax
       je        short M33_L04
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M33_L00
       test      rax,rax
       je        short M33_L04
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M33_L00
       test      rax,rax
       jne       short M33_L02
M33_L04:
       lea       rax,[System.Reflection.CustomAttributeExtensions.GetCustomAttribute[[System.__Canon, System.Private.CoreLib]](System.Reflection.Assembly)]
       jmp       qword ptr [rax]
; Total bytes of code 86
```
```assembly
; AuroraScript.Runtime.ScriptDatum.FromString(System.String)
       push      rbp
       sub       rsp,20
       lea       rbp,[rsp+20]
       mov       [rbp+10],rcx
       mov       [rbp+18],rdx
       mov       rcx,[rbp+10]
       mov       rdx,[rbp+18]
       call      qword ptr [7FF9779FEA90]; AuroraScript.Runtime.ScriptDatum.CreateString(System.String)
       mov       rax,[rbp+10]
       add       rsp,20
       pop       rbp
       ret
; Total bytes of code 42
```
```assembly
; System.Object..ctor()
       ret
; Total bytes of code 1
```
```assembly
; AuroraScript.Runtime.Property.HiddenClass.CreateEnumerableKeys(AuroraScript.Runtime.Property.HiddenProperty[])
       push      rbp
       push      rdi
       push      rsi
       sub       rsp,80
       lea       rbp,[rsp+90]
       xor       eax,eax
       mov       [rbp-68],rax
       vxorps    xmm4,xmm4,xmm4
       vmovdqu   ymmword ptr [rbp-60],ymm4
       mov       [rbp-40],rax
       mov       [rbp+10],rcx
       mov       dword ptr [rbp-70],3E8
       xor       eax,eax
       mov       [rbp-3C],eax
       xor       eax,eax
       mov       [rbp-50],eax
       jmp       short M36_L02
M36_L00:
       mov       rax,[rbp+10]
       mov       ecx,[rbp-50]
       cmp       ecx,[rax+8]
       jae       near ptr M36_L09
       mov       edx,ecx
       imul      rdx,20
       lea       rax,[rax+rdx+10]
       lea       rcx,[rax+8]
       call      qword ptr [7FF9779FEAC0]; AuroraScript.Runtime.Property.PropertyMeta.get_Enumerable()
       test      eax,eax
       je        short M36_L01
       mov       rcx,7FF977AC1D90
       call      CORINFO_HELP_COUNTPROFILE32
       mov       eax,[rbp-3C]
       inc       eax
       mov       [rbp-3C],eax
M36_L01:
       mov       rcx,7FF977AC1D94
       call      CORINFO_HELP_COUNTPROFILE32
       mov       eax,[rbp-50]
       inc       eax
       mov       [rbp-50],eax
M36_L02:
       mov       eax,[rbp-70]
       dec       eax
       mov       [rbp-70],eax
       cmp       dword ptr [rbp-70],0
       jg        short M36_L03
       lea       rcx,[rbp-70]
       mov       edx,21
       call      CORINFO_HELP_PATCHPOINT
M36_L03:
       mov       rax,[rbp+10]
       mov       eax,[rax+8]
       cmp       eax,[rbp-50]
       jg        short M36_L00
       cmp       dword ptr [rbp-3C],0
       jne       short M36_L04
       mov       rcx,7FF977AC1D98
       call      CORINFO_HELP_COUNTPROFILE32
       call      qword ptr [7FF9779FE100]; System.Array.Empty[[AuroraScript.Runtime.ScriptDatum, AuroraScript]]()
       nop
       add       rsp,80
       pop       rsi
       pop       rdi
       pop       rbp
       ret
M36_L04:
       movsxd    rdx,dword ptr [rbp-3C]
       mov       rcx,offset MT_AuroraScript.Runtime.ScriptDatum[]
       call      CORINFO_HELP_NEWARR_1_VC
       mov       [rbp-48],rax
       xor       eax,eax
       mov       [rbp-4C],eax
       xor       eax,eax
       mov       [rbp-54],eax
       jmp       near ptr M36_L07
M36_L05:
       mov       rax,[rbp+10]
       mov       ecx,[rbp-54]
       cmp       ecx,[rax+8]
       jae       near ptr M36_L09
       mov       edx,ecx
       imul      rdx,20
       lea       rax,[rax+rdx+10]
       lea       rcx,[rax+8]
       call      qword ptr [7FF9779FEAC0]; AuroraScript.Runtime.Property.PropertyMeta.get_Enumerable()
       test      eax,eax
       je        short M36_L06
       mov       rcx,7FF977AC1D9C
       call      CORINFO_HELP_COUNTPROFILE32
       mov       eax,[rbp-4C]
       mov       [rbp-58],eax
       mov       eax,[rbp-4C]
       inc       eax
       mov       [rbp-4C],eax
       mov       rax,[rbp+10]
       mov       ecx,[rbp-54]
       cmp       ecx,[rax+8]
       jae       near ptr M36_L09
       mov       edx,ecx
       imul      rdx,20
       lea       rax,[rax+rdx+10]
       vmovdqu   xmm0,xmmword ptr [rax+10]
       vmovdqu   xmmword ptr [rbp-68],xmm0
       mov       rax,[rbp-48]
       mov       ecx,[rbp-58]
       cmp       ecx,[rax+8]
       jae       short M36_L09
       mov       edx,ecx
       imul      rdx,10
       lea       rax,[rax+rdx+10]
       mov       rdi,rax
       lea       rsi,[rbp-68]
       call      CORINFO_HELP_ASSIGN_BYREF
       movsq
M36_L06:
       mov       rcx,7FF977AC1DA0
       call      CORINFO_HELP_COUNTPROFILE32
       mov       eax,[rbp-54]
       inc       eax
       mov       [rbp-54],eax
M36_L07:
       mov       eax,[rbp-70]
       dec       eax
       mov       [rbp-70],eax
       cmp       dword ptr [rbp-70],0
       jg        short M36_L08
       lea       rcx,[rbp-70]
       mov       edx,70
       call      CORINFO_HELP_PATCHPOINT
M36_L08:
       mov       rax,[rbp+10]
       mov       eax,[rax+8]
       cmp       eax,[rbp-54]
       jg        near ptr M36_L05
       mov       rcx,7FF977AC1DA4
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rax,[rbp-48]
       add       rsp,80
       pop       rsi
       pop       rdi
       pop       rbp
       ret
M36_L09:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 511
```
```assembly
; System.Collections.Generic.Dictionary`2[[AuroraScript.Runtime.Property.TransitionKey, AuroraScript],[System.__Canon, System.Private.CoreLib]].Initialize(Int32)
       push      rbp
       sub       rsp,60
       lea       rbp,[rsp+60]
       xor       eax,eax
       mov       [rbp-18],rax
       mov       [rbp-20],rax
       mov       [rbp-8],rcx
       mov       [rbp+10],rcx
       mov       [rbp+18],edx
       mov       ecx,[rbp+18]
       call      qword ptr [7FF977825A88]; System.Collections.HashHelpers.GetPrime(Int32)
       mov       [rbp-0C],eax
       movsxd    rdx,dword ptr [rbp-0C]
       mov       rcx,offset MT_System.Int32[]
       call      CORINFO_HELP_NEWARR_1_VC
       mov       [rbp-18],rax
       mov       rax,[rbp+10]
       mov       rax,[rax]
       mov       [rbp-30],rax
       mov       rax,[rbp-30]
       mov       rax,[rax+30]
       mov       rax,[rax]
       mov       rax,[rax+18]
       mov       [rbp-38],rax
       cmp       qword ptr [rbp-38],0
       je        short M37_L00
       mov       rax,[rbp-38]
       mov       [rbp-28],rax
       jmp       short M37_L01
M37_L00:
       mov       rcx,[rbp-30]
       mov       rdx,7FF977AD07E0
       call      qword ptr [7FF97782F4B0]; System.Runtime.CompilerServices.GenericsHelpers.Class(IntPtr, IntPtr)
       mov       [rbp-28],rax
M37_L01:
       movsxd    rdx,dword ptr [rbp-0C]
       mov       rcx,[rbp-28]
       call      CORINFO_HELP_NEWARR_1_VC
       mov       [rbp-20],rax
       mov       rax,[rbp+10]
       mov       dword ptr [rax+3C],0FFFFFFFF
       mov       ecx,[rbp-0C]
       call      qword ptr [7FF97782F4C8]; System.Collections.HashHelpers.GetFastModMultiplier(UInt32)
       mov       rcx,[rbp+10]
       mov       [rcx+30],rax
       mov       rax,[rbp+10]
       lea       rcx,[rax+8]
       mov       rdx,[rbp-18]
       call      CORINFO_HELP_ASSIGN_REF
       mov       rax,[rbp+10]
       lea       rcx,[rax+10]
       mov       rdx,[rbp-20]
       call      CORINFO_HELP_ASSIGN_REF
       mov       eax,[rbp-0C]
       add       rsp,60
       pop       rbp
       ret
; Total bytes of code 225
```
```assembly
; System.Collections.Generic.EqualityComparer`1[[AuroraScript.Runtime.Property.TransitionKey, AuroraScript]].get_Default()
       push      rbp
       sub       rsp,20
       lea       rbp,[rsp+20]
       mov       rcx,offset MT_System.Collections.Generic.EqualityComparer<AuroraScript.Runtime.Property.TransitionKey>
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rax,0BA3C000468
       mov       rax,[rax]
       add       rsp,20
       pop       rbp
       ret
; Total bytes of code 45
```
```assembly
; AuroraScript.Runtime.Property.TransitionKey.GetHashCode()
       push      rbp
       sub       rsp,40
       lea       rbp,[rsp+40]
       vxorps    xmm4,xmm4,xmm4
       vmovdqu   ymmword ptr [rbp-20],ymm4
       mov       [rbp+10],rcx
       vxorps    ymm0,ymm0,ymm0
       vmovdqu   ymmword ptr [rbp-20],ymm0
       mov       rax,[rbp+10]
       mov       r8,[rax]
       lea       rcx,[rbp-20]
       mov       rdx,7FF977AC3268
       call      qword ptr [7FF9779FECA0]; System.HashCode.Add[[System.__Canon, System.Private.CoreLib]](System.__Canon)
       mov       rax,[rbp+10]
       movzx     edx,word ptr [rax+8]
       lea       rcx,[rbp-20]
       call      qword ptr [7FF9779FECD0]; System.HashCode.Add[[AuroraScript.Runtime.Property.PropertyFlags, AuroraScript]](AuroraScript.Runtime.Property.PropertyFlags)
       lea       rcx,[rbp-20]
       call      qword ptr [7FF9779FECE8]; System.HashCode.ToHashCode()
       nop
       add       rsp,40
       pop       rbp
       ret
; Total bytes of code 94
```
```assembly
; System.Collections.Generic.Dictionary`2[[AuroraScript.Runtime.Property.TransitionKey, AuroraScript],[System.__Canon, System.Private.CoreLib]].GetBucket(UInt32)
       push      rbp
       sub       rsp,30
       lea       rbp,[rsp+30]
       xor       eax,eax
       mov       [rbp-8],rax
       mov       [rbp+10],rcx
       mov       [rbp+18],edx
       mov       rax,[rbp+10]
       mov       rax,[rax+8]
       mov       [rbp-8],rax
       mov       rax,[rbp-8]
       mov       edx,[rax+8]
       mov       rax,[rbp+10]
       mov       r8,[rax+30]
       mov       ecx,[rbp+18]
       call      qword ptr [7FF97782F540]; System.Collections.HashHelpers.FastMod(UInt32, UInt32, UInt64)
       mov       rcx,[rbp-8]
       cmp       eax,[rcx+8]
       jae       short M40_L00
       mov       edx,eax
       lea       rcx,[rcx+rdx*4+10]
       mov       rax,rcx
       add       rsp,30
       pop       rbp
       ret
M40_L00:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 90
```
```assembly
; System.Runtime.InteropServices.MemoryMarshal.GetArrayDataReference[[AuroraScript.Runtime.ScriptDatum, AuroraScript]](AuroraScript.Runtime.ScriptDatum[])
       push      rbp
       mov       rbp,rsp
       mov       [rbp+10],rcx
       mov       rax,[rbp+10]
       cmp       [rax],al
       mov       rax,[rbp+10]
       xor       ecx,ecx
       imul      rdx,rcx,10
       lea       rax,[rax+rdx+10]
       pop       rbp
       ret
; Total bytes of code 31
```
```assembly
; System.Math.Min(Int32, Int32)
       cmp       ecx,edx
       mov       eax,edx
       cmovle    eax,ecx
       ret
; Total bytes of code 8
```
```assembly
; System.Buffer.Memmove[[AuroraScript.Runtime.ScriptDatum, AuroraScript]](AuroraScript.Runtime.ScriptDatum ByRef, AuroraScript.Runtime.ScriptDatum ByRef, UIntPtr)
       push      rbp
       sub       rsp,20
       lea       rbp,[rsp+20]
       mov       [rbp+10],rcx
       mov       [rbp+18],rdx
       mov       [rbp+20],r8
       mov       rax,[rbp+20]
       mov       r8,rax
       shl       r8,4
       mov       rcx,[rbp+10]
       mov       rdx,[rbp+18]
       call      qword ptr [7FF9778257A0]; System.Buffer.BulkMoveWithWriteBarrier(Byte ByRef, Byte ByRef, UIntPtr)
       nop
       add       rsp,20
       pop       rbp
       ret
; Total bytes of code 54
```

## .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3 (Job: Job-BZNPUE(IterationCount=5, IterationTime=200ms, LaunchCount=1, WarmupCount=3))

```assembly
; AuroraBenchmark.BoundaryOpsBenchmarks.ArraySpread()
       push      r15
       push      r14
       push      r13
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,40
       vxorps    xmm4,xmm4,xmm4
       vmovdqu   ymmword ptr [rsp+20],ymm4
       mov       rbx,rcx
       mov       rsi,[rbx+38]
       mov       edi,[rsi+34]
       test      edi,edi
       jle       near ptr M00_L17
       mov       rdx,[rsi+38]
       test      rdx,rdx
       je        near ptr M00_L13
       lea       rcx,[rdx+10]
       xor       eax,eax
       mov       r8,[rdx]
       cmp       dword ptr [r8+4],18
       jne       near ptr M00_L14
M00_L00:
       mov       r10d,eax
       neg       r10d
       test      eax,eax
       jg        near ptr M00_L15
       test      r10d,r10d
       jl        near ptr M00_L15
       lea       eax,[r10+rdi]
       cmp       eax,[rdx+8]
       ja        near ptr M00_L15
       movzx     edx,word ptr [r8]
       mov       eax,r10d
       imul      rax,rdx
       add       rcx,rax
       mov       eax,edi
       imul      rdx,rax
       test      dword ptr [r8],1000000
       je        near ptr M00_L16
       shr       rdx,3
       call      qword ptr [7FF977C45DB8]; System.SpanHelpers.ClearWithReferences(IntPtr ByRef, UIntPtr)
M00_L01:
       xor       edx,edx
       mov       [rsi+34],edx
       mov       rbp,[rbx+38]
       mov       r14,[rbx+58]
       mov       r15,[rbx+60]
       test      r14,r14
       je        near ptr M00_L18
       mov       rdx,0FBF3C00450
       cmp       r14,[rdx]
       je        near ptr M00_L08
       mov       rdx,0FBF3C00458
       cmp       r14,[rdx]
       je        near ptr M00_L08
       movsx     rdx,r15w
M00_L02:
       cmp       edx,18
       jne       near ptr M00_L08
       mov       rsi,r14
       test      rsi,rsi
       je        short M00_L03
       mov       rdx,offset MT_AuroraScript.Runtime.Types.ScriptArray
       cmp       [rsi],rdx
       jne       near ptr M00_L21
M00_L03:
       test      rsi,rsi
       je        near ptr M00_L22
M00_L04:
       test      rsi,rsi
       je        near ptr M00_L08
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptArray
       cmp       [rsi],rcx
       jne       near ptr M00_L08
       xor       r14d,r14d
       cmp       r14d,[rsi+34]
       jge       near ptr M00_L11
M00_L05:
       test      r14d,r14d
       jl        near ptr M00_L26
       mov       rcx,[rsi+38]
       cmp       r14d,[rcx+8]
       jae       near ptr M00_L30
       mov       rdx,r14
       shl       rdx,4
       lea       rcx,[rcx+rdx+10]
       mov       rdx,[rcx]
       mov       rdi,[rcx+8]
M00_L06:
       mov       r15d,[rbp+34]
       mov       rcx,[rbp+38]
       cmp       [rcx+8],r15d
       jbe       near ptr M00_L27
       cmp       r15d,[rcx+8]
       jae       near ptr M00_L30
       mov       eax,r15d
       shl       rax,4
       lea       r13,[rcx+rax+10]
       mov       rcx,r13
       call      CORINFO_HELP_ASSIGN_REF
       mov       [r13+8],rdi
       inc       r15d
       mov       [rbp+34],r15d
M00_L07:
       inc       r14d
       cmp       r14d,[rsi+34]
       jl        short M00_L05
       jmp       near ptr M00_L11
M00_L08:
       mov       rdx,r14
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptPackedArray
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r13,rax
       test      r13,r13
       je        near ptr M00_L12
       xor       r14d,r14d
       mov       rcx,r13
       mov       rax,[r13]
       mov       r15,[rax+58]
       call      qword ptr [r15+8]
       test      eax,eax
       jle       short M00_L11
M00_L09:
       lea       rdx,[rsp+30]
       mov       rcx,r13
       mov       r8d,r14d
       call      qword ptr [r15+10]
       mov       edx,[rbp+34]
       mov       rax,[rbp+38]
       cmp       [rax+8],edx
       jbe       near ptr M00_L28
       cmp       edx,[rax+8]
       jae       near ptr M00_L30
       mov       ecx,edx
       shl       rcx,4
       lea       rdi,[rax+rcx+10]
       lea       rsi,[rsp+30]
       call      CORINFO_HELP_ASSIGN_BYREF
       movsq
       inc       edx
       mov       [rbp+34],edx
M00_L10:
       inc       r14d
       mov       rcx,r13
       call      qword ptr [r15+8]
       cmp       eax,r14d
       jg        short M00_L09
M00_L11:
       mov       rax,[rbx+38]
       mov       eax,[rax+34]
       add       rsp,40
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r13
       pop       r14
       pop       r15
       ret
M00_L12:
       mov       esi,[rbp+34]
       mov       rcx,[rbp+38]
       cmp       [rcx+8],esi
       jbe       near ptr M00_L29
       cmp       esi,[rcx+8]
       jae       near ptr M00_L30
       mov       edx,esi
       shl       rdx,4
       lea       rdi,[rcx+rdx+10]
       mov       rcx,rdi
       mov       rdx,r14
       call      CORINFO_HELP_ASSIGN_REF
       mov       [rdi+8],r15
       inc       esi
       mov       [rbp+34],esi
       jmp       short M00_L11
M00_L13:
       mov       ecx,2
       call      qword ptr [7FF97784FD38]
       int       3
M00_L14:
       mov       r10d,[r8+4]
       add       r10d,0FFFFFFE8
       shr       r10d,3
       movsxd    rax,r10d
       mov       eax,[rcx+rax*4]
       shl       r10d,3
       movsxd    r10,r10d
       add       rcx,r10
       jmp       near ptr M00_L00
M00_L15:
       call      qword ptr [7FF977C47E40]
       int       3
M00_L16:
       call      qword ptr [7FF9778457E8]; System.SpanHelpers.ClearWithoutReferences(Byte ByRef, UIntPtr)
       jmp       near ptr M00_L01
M00_L17:
       test      edi,edi
       jge       near ptr M00_L01
       mov       rcx,rsi
       xor       edx,edx
       call      qword ptr [7FF977C45DA0]
       mov       r8d,[rsi+34]
       neg       r8d
       mov       edx,[rsi+34]
       mov       rcx,[rsi+38]
       call      qword ptr [7FF977C45D88]; System.Array.Clear(System.Array, Int32, Int32)
       jmp       near ptr M00_L01
M00_L18:
       test      r15,r15
       je        short M00_L20
       lea       rdx,[r15-1]
       mov       ecx,1
       mov       eax,2
       cmp       rdx,1
       cmova     ecx,eax
       mov       edx,ecx
M00_L19:
       jmp       near ptr M00_L02
M00_L20:
       xor       edx,edx
       jmp       short M00_L19
M00_L21:
       mov       rdx,r14
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rsi,rax
       jmp       near ptr M00_L03
M00_L22:
       mov       rdx,r14
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       short M00_L23
       xor       esi,esi
       jmp       near ptr M00_L04
M00_L23:
       cmp       dword ptr [rdi+8],0
       jne       short M00_L24
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0FBF3C01498
       mov       rsi,[rcx]
       jmp       short M00_L25
M00_L24:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      CORINFO_HELP_NEWSFAST
       mov       rsi,rax
       mov       rcx,rsi
       mov       rdx,rdi
       call      qword ptr [7FF977C47E88]
M00_L25:
       jmp       near ptr M00_L04
M00_L26:
       xor       edx,edx
       xor       edi,edi
       jmp       near ptr M00_L06
M00_L27:
       mov       [rsp+20],rdx
       mov       [rsp+28],rdi
       lea       rdx,[rsp+20]
       mov       rcx,rbp
       call      qword ptr [7FF977C45EC0]
       jmp       near ptr M00_L07
M00_L28:
       lea       rdx,[rsp+30]
       mov       rcx,rbp
       call      qword ptr [7FF977C45EC0]
       jmp       near ptr M00_L10
M00_L29:
       mov       [rsp+20],r14
       mov       [rsp+28],r15
       lea       rdx,[rsp+20]
       mov       rcx,rbp
       call      qword ptr [7FF977C45EC0]
       jmp       near ptr M00_L11
M00_L30:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 1004
```
```assembly
; System.SpanHelpers.ClearWithReferences(IntPtr ByRef, UIntPtr)
       cmp       rdx,8
       jb        short M01_L01
M01_L00:
       xor       eax,eax
       mov       [rcx+rdx*8-8],rax
       mov       [rcx+rdx*8-10],rax
       mov       [rcx+rdx*8-18],rax
       mov       [rcx+rdx*8-20],rax
       mov       [rcx+rdx*8-28],rax
       mov       [rcx+rdx*8-30],rax
       mov       [rcx+rdx*8-38],rax
       mov       [rcx+rdx*8-40],rax
       add       rdx,0FFFFFFFFFFFFFFF8
       cmp       rdx,8
       jae       short M01_L00
M01_L01:
       cmp       rdx,4
       jae       short M01_L03
       cmp       rdx,2
       jae       short M01_L04
       test      rdx,rdx
       jne       short M01_L05
M01_L02:
       ret
M01_L03:
       xor       eax,eax
       mov       [rcx+10],rax
       mov       [rcx+18],rax
       mov       [rcx+rdx*8-18],rax
       mov       [rcx+rdx*8-10],rax
M01_L04:
       xor       eax,eax
       mov       [rcx+8],rax
       mov       [rcx+rdx*8-8],rax
M01_L05:
       xor       eax,eax
       mov       [rcx],rax
       jmp       short M01_L02
; Total bytes of code 114
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
; System.SpanHelpers.ClearWithoutReferences(Byte ByRef, UIntPtr)
       push      rbx
       test      rdx,rdx
       je        near ptr M03_L04
       lea       rbx,[rcx+rdx]
       cmp       rdx,10
       jbe       near ptr M03_L07
       cmp       rdx,40
       jbe       short M03_L02
       cmp       rdx,400
       ja        near ptr M03_L05
       cmp       rdx,100
       jb        short M03_L00
       mov       rax,rcx
       and       rax,3F
       neg       rax
       add       rax,40
       xorps     xmm0,xmm0
       movups    [rcx],xmm0
       movups    [rcx+10],xmm0
       movups    [rcx+20],xmm0
       movups    [rcx+30],xmm0
       add       rcx,rax
       sub       rdx,rax
M03_L00:
       mov       rax,rdx
       shr       rax,6
M03_L01:
       xorps     xmm0,xmm0
       movups    [rcx],xmm0
       movups    [rcx+10],xmm0
       movups    [rcx+20],xmm0
       movups    [rcx+30],xmm0
       add       rcx,40
       dec       rax
       jne       short M03_L01
       and       rdx,3F
       cmp       rdx,10
       jbe       short M03_L03
M03_L02:
       xorps     xmm0,xmm0
       movups    [rcx],xmm0
       cmp       rdx,20
       jbe       short M03_L03
       xorps     xmm0,xmm0
       movups    [rcx+10],xmm0
       cmp       rdx,30
       ja        short M03_L06
M03_L03:
       xorps     xmm0,xmm0
       movups    [rbx-10],xmm0
M03_L04:
       pop       rbx
       ret
M03_L05:
       cmp       [rcx],cl
       lea       rax,[System.Reflection.CustomAttributeExtensions.GetCustomAttribute[[System.__Canon, System.Private.CoreLib]](System.Reflection.Assembly)]
       pop       rbx
       jmp       qword ptr [rax]
M03_L06:
       xorps     xmm0,xmm0
       movups    [rcx+20],xmm0
       jmp       short M03_L03
M03_L07:
       test      dl,18
       je        short M03_L08
       xor       edx,edx
       mov       [rcx],rdx
       mov       [rbx-8],rdx
       jmp       short M03_L04
M03_L08:
       test      dl,4
       je        short M03_L09
       xor       edx,edx
       mov       [rcx],edx
       mov       [rbx-4],edx
       jmp       short M03_L04
M03_L09:
       mov       byte ptr [rcx],0
       test      dl,2
       je        short M03_L04
       mov       word ptr [rbx-2],0
       jmp       short M03_L04
; Total bytes of code 236
```
```assembly
; System.Array.Clear(System.Array, Int32, Int32)
       sub       rsp,28
       test      rcx,rcx
       je        short M04_L01
       lea       rax,[rcx+10]
       xor       r10d,r10d
       mov       r9,[rcx]
       cmp       dword ptr [r9+4],18
       jne       short M04_L02
M04_L00:
       mov       r11d,edx
       sub       r11d,r10d
       cmp       edx,r10d
       jl        short M04_L03
       mov       edx,r11d
       or        edx,r8d
       jl        short M04_L03
       lea       edx,[r11+r8]
       cmp       edx,[rcx+8]
       ja        short M04_L03
       movzx     edx,word ptr [r9]
       mov       ecx,r11d
       imul      rcx,rdx
       add       rcx,rax
       mov       eax,r8d
       imul      rdx,rax
       test      dword ptr [r9],1000000
       je        short M04_L04
       shr       rdx,3
       add       rsp,28
       jmp       qword ptr [7FF977C45DB8]; System.SpanHelpers.ClearWithReferences(IntPtr ByRef, UIntPtr)
M04_L01:
       mov       ecx,2
       call      qword ptr [7FF97784FD38]
       int       3
M04_L02:
       mov       r11d,[r9+4]
       add       r11d,0FFFFFFE8
       shr       r11d,3
       movsxd    r10,r11d
       mov       r10d,[rax+r10*4]
       shl       r11d,3
       movsxd    r11,r11d
       add       rax,r11
       jmp       short M04_L00
M04_L03:
       call      qword ptr [7FF977C47E40]
       int       3
M04_L04:
       call      qword ptr [7FF9778457E8]; System.SpanHelpers.ClearWithoutReferences(Byte ByRef, UIntPtr)
       nop
       add       rsp,28
       ret
; Total bytes of code 160
```
```assembly
; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rax,[rcx+20]
       mov       rax,[rax-18]
       mov       rdx,rax
       test      dl,1
       jne       short M05_L00
       ret
M05_L00:
       jmp       qword ptr [7FF977845C38]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBaseSlow(System.Runtime.CompilerServices.MethodTable*)
; Total bytes of code 23
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
; AuroraBenchmark.BoundaryOpsBenchmarks.ArgumentSpread()
       push      r15
       push      r14
       push      r13
       push      r12
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,38
       xor       eax,eax
       mov       [rsp+20],rax
       mov       [rsp+28],rax
       mov       rbx,rcx
       xor       ebp,ebp
       mov       r14,[rbx+40]
       mov       r15,[rbx+58]
       mov       r13,[rbx+60]
       test      r15,r15
       je        near ptr M00_L12
       mov       rdx,0CE39000450
       cmp       r15,[rdx]
       je        near ptr M00_L06
       mov       rdx,0CE39000458
       cmp       r15,[rdx]
       je        near ptr M00_L06
       movsx     rdx,r13w
M00_L00:
       cmp       edx,18
       jne       near ptr M00_L06
       mov       rsi,r15
       test      rsi,rsi
       je        short M00_L01
       mov       rdx,offset MT_AuroraScript.Runtime.Types.ScriptArray
       cmp       [rsi],rdx
       jne       near ptr M00_L15
M00_L01:
       test      rsi,rsi
       je        near ptr M00_L16
M00_L02:
       test      rsi,rsi
       je        near ptr M00_L06
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptArray
       cmp       [rsi],rcx
       jne       near ptr M00_L06
       mov       edx,[rsi+34]
       cmp       [r14+8],edx
       jl        near ptr M00_L20
M00_L03:
       xor       edi,edi
       cmp       dword ptr [rsi+34],0
       jle       near ptr M00_L22
M00_L04:
       lea       ecx,[rbp+1]
       mov       r15d,ecx
       test      edi,edi
       jl        near ptr M00_L21
       cmp       edi,[rsi+34]
       jge       near ptr M00_L21
       mov       rcx,[rsi+38]
       cmp       edi,[rcx+8]
       jae       near ptr M00_L23
       mov       rdx,rdi
       shl       rdx,4
       lea       rcx,[rcx+rdx+10]
       mov       rdx,[rcx]
       mov       r13,[rcx+8]
M00_L05:
       cmp       ebp,[r14+8]
       jae       near ptr M00_L23
       mov       ecx,ebp
       shl       rcx,4
       lea       rbp,[r14+rcx+10]
       mov       rcx,rbp
       call      CORINFO_HELP_ASSIGN_REF
       mov       [rbp+8],r13
       inc       edi
       cmp       edi,[rsi+34]
       jge       near ptr M00_L10
       mov       ebp,r15d
       jmp       short M00_L04
M00_L06:
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptPackedArray
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r12,rax
       test      r12,r12
       je        short M00_L07
       mov       rcx,r12
       mov       rax,[r12]
       mov       rax,[rax+58]
       call      qword ptr [rax+8]
       mov       edx,eax
       mov       rcx,r14
       xor       r8d,r8d
       call      qword ptr [7FF977C55D70]; AuroraScript.Runtime.CallOps.EnsureCapacity(AuroraScript.Runtime.ScriptDatum[], Int32, Int32)
       mov       r14,rax
       xor       r15d,r15d
       mov       rcx,r12
       mov       rax,[r12]
       mov       r13,[rax+58]
       call      qword ptr [r13+8]
       test      eax,eax
       jg        short M00_L09
       mov       r15d,ebp
       jmp       near ptr M00_L10
M00_L07:
       mov       rcx,r14
       mov       edx,1
       xor       r8d,r8d
       call      qword ptr [7FF977C55D70]; AuroraScript.Runtime.CallOps.EnsureCapacity(AuroraScript.Runtime.ScriptDatum[], Int32, Int32)
       mov       rsi,rax
       mov       ebp,1
       cmp       dword ptr [rsi+8],0
       jbe       near ptr M00_L23
       lea       rdi,[rsi+10]
       mov       rcx,rdi
       mov       rdx,r15
       call      CORINFO_HELP_ASSIGN_REF
       mov       [rdi+8],r13
       mov       rdx,rsi
       mov       r15d,ebp
       jmp       short M00_L11
M00_L08:
       mov       ebp,[rsp+34]
M00_L09:
       lea       edx,[rbp+1]
       mov       [rsp+34],edx
       lea       rdx,[rsp+20]
       mov       rcx,r12
       mov       r8d,r15d
       call      qword ptr [r13+10]
       cmp       ebp,[r14+8]
       jae       near ptr M00_L23
       mov       eax,ebp
       shl       rax,4
       lea       rdi,[r14+rax+10]
       lea       rsi,[rsp+20]
       call      CORINFO_HELP_ASSIGN_BYREF
       movsq
       inc       r15d
       mov       rcx,r12
       call      qword ptr [r13+8]
       cmp       eax,r15d
       jg        short M00_L08
       mov       r15d,[rsp+34]
M00_L10:
       mov       rdx,r14
M00_L11:
       lea       rcx,[rbx+40]
       call      CORINFO_HELP_ASSIGN_REF
       mov       eax,r15d
       add       rsp,38
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       ret
M00_L12:
       test      r13,r13
       je        short M00_L14
       lea       rdx,[r13-1]
       mov       ecx,1
       mov       eax,2
       cmp       rdx,1
       cmova     ecx,eax
       mov       edx,ecx
M00_L13:
       jmp       near ptr M00_L00
M00_L14:
       xor       edx,edx
       jmp       short M00_L13
M00_L15:
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rsi,rax
       jmp       near ptr M00_L01
M00_L16:
       mov       rdx,r15
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       short M00_L17
       xor       esi,esi
       jmp       near ptr M00_L02
M00_L17:
       cmp       dword ptr [rdi+8],0
       jne       short M00_L18
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0CE39001498
       mov       rsi,[rcx]
       jmp       short M00_L19
M00_L18:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      CORINFO_HELP_NEWSFAST
       mov       rsi,rax
       mov       rcx,rsi
       mov       rdx,rdi
       call      qword ptr [7FF977C57D50]
M00_L19:
       jmp       near ptr M00_L02
M00_L20:
       mov       rcx,0CE39000B38
       mov       rcx,[rcx]
       call      qword ptr [7FF977C22DB8]; Precode of System.Buffers.SharedArrayPool`1[[AuroraScript.Runtime.ScriptDatum, AuroraScript]].Rent(Int32)
       mov       r15,rax
       mov       rcx,r14
       mov       rdx,r15
       xor       r8d,r8d
       call      qword ptr [7FF97785F588]; System.Array.Copy(System.Array, System.Array, Int32)
       mov       rcx,0CE39000B38
       mov       rcx,[rcx]
       mov       rdx,r14
       xor       r8d,r8d
       call      qword ptr [7FF977C22DC0]; System.Buffers.SharedArrayPool`1[[AuroraScript.Runtime.ScriptDatum, AuroraScript]].Return(AuroraScript.Runtime.ScriptDatum[], Boolean)
       mov       r14,r15
       jmp       near ptr M00_L03
M00_L21:
       xor       edx,edx
       xor       r13d,r13d
       jmp       near ptr M00_L05
M00_L22:
       mov       r15d,ebp
       jmp       near ptr M00_L10
M00_L23:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 819
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
; AuroraScript.Runtime.CallOps.EnsureCapacity(AuroraScript.Runtime.ScriptDatum[], Int32, Int32)
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,20
       mov       rbx,rcx
       mov       esi,r8d
       cmp       [rbx+8],edx
       jl        short M02_L00
       mov       rax,rbx
       add       rsp,20
       pop       rbx
       pop       rsi
       pop       rdi
       ret
M02_L00:
       mov       rcx,0CE39000B38
       mov       rcx,[rcx]
       call      qword ptr [7FF977C22DB8]; Precode of System.Buffers.SharedArrayPool`1[[AuroraScript.Runtime.ScriptDatum, AuroraScript]].Rent(Int32)
       mov       rdi,rax
       mov       rcx,rbx
       mov       rdx,rdi
       mov       r8d,esi
       call      qword ptr [7FF97785F588]; System.Array.Copy(System.Array, System.Array, Int32)
       test      esi,esi
       jle       short M02_L01
       mov       rcx,rbx
       mov       r8d,esi
       xor       edx,edx
       call      qword ptr [7FF977C57DE0]; System.Array.Clear(System.Array, Int32, Int32)
M02_L01:
       mov       rcx,0CE39000B38
       mov       rcx,[rcx]
       mov       rdx,rbx
       xor       r8d,r8d
       call      qword ptr [7FF977C22DC0]; System.Buffers.SharedArrayPool`1[[AuroraScript.Runtime.ScriptDatum, AuroraScript]].Return(AuroraScript.Runtime.ScriptDatum[], Boolean)
       mov       rax,rdi
       add       rsp,20
       pop       rbx
       pop       rsi
       pop       rdi
       ret
; Total bytes of code 120
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
       jmp       qword ptr [7FF977855C38]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBaseSlow(System.Runtime.CompilerServices.MethodTable*)
; Total bytes of code 23
```
```assembly
; System.Array.Copy(System.Array, System.Array, Int32)
       push      r14
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,30
       mov       rsi,rcx
       mov       rbx,rdx
       mov       edi,r8d
       test      rsi,rsi
       jne       short M04_L01
       xor       ebp,ebp
M04_L00:
       test      rbx,rbx
       jne       short M04_L03
       xor       eax,eax
       jmp       short M04_L04
M04_L01:
       test      rbx,rbx
       je        short M04_L02
       mov       r14,[rsi]
       cmp       r14,[rbx]
       jne       short M04_L02
       cmp       dword ptr [r14+4],18
       jne       short M04_L02
       cmp       edi,[rsi+8]
       ja        short M04_L02
       cmp       edi,[rbx+8]
       jbe       short M04_L06
M04_L02:
       mov       rcx,rsi
       xor       edx,edx
       call      qword ptr [7FF96E84A390]; Precode of System.Array.GetLowerBound(Int32)
       mov       ebp,eax
       jmp       short M04_L00
M04_L03:
       mov       rcx,rbx
       xor       edx,edx
       call      qword ptr [7FF96E84A390]; Precode of System.Array.GetLowerBound(Int32)
M04_L04:
       mov       [rsp+20],edi
       xor       ecx,ecx
       mov       [rsp+28],ecx
       mov       rcx,rsi
       mov       edx,ebp
       mov       r8,rbx
       mov       r9d,eax
       call      qword ptr [7FF96E84A328]
M04_L05:
       nop
       add       rsp,30
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       ret
M04_L06:
       mov       r8d,edi
       movzx     ecx,word ptr [r14]
       imul      r8,rcx
       lea       rdx,[rsi+10]
       lea       rcx,[rbx+10]
       test      dword ptr [r14],1000000
       je        short M04_L08
       cmp       r8,4000
       ja        short M04_L07
       call      qword ptr [7FF96E84A630]
       mov       rax,[System.Reflection.CustomAttributeExtensions.GetCustomAttribute[[System.__Canon, System.Private.CoreLib]](System.Reflection.Assembly)]
       cmp       dword ptr [rax],0
       je        short M04_L05
       call      qword ptr [7FF96E839040]; CORINFO_HELP_POLL_GC
       jmp       short M04_L05
M04_L07:
       lea       rax,[System.Reflection.CustomAttributeExtensions.GetCustomAttribute[[System.__Canon, System.Private.CoreLib]](System.Reflection.Assembly)]
       add       rsp,30
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       jmp       qword ptr [rax]
M04_L08:
       cmp       r8,19
       jne       short M04_L09
       movups    xmm0,[rdx]
       movups    xmm1,[rdx+9]
       movups    [rcx],xmm0
       movups    [rcx+9],xmm1
       jmp       short M04_L05
M04_L09:
       call      qword ptr [7FF96E84D928]; Precode of System.SpanHelpers.Memmove(Byte ByRef, Byte ByRef, UIntPtr)
       jmp       short M04_L05
; Total bytes of code 243
```
```assembly
; System.Buffers.SharedArrayPool`1[[AuroraScript.Runtime.ScriptDatum, AuroraScript]].Return(AuroraScript.Runtime.ScriptDatum[], Boolean)
       push      rbp
       push      rdi
       push      rsi
       sub       rsp,130
       lea       rbp,[rsp+140]
       vxorps    xmm4,xmm4,xmm4
       vmovdqa   xmmword ptr [rbp-100],xmm4
       vmovdqa   xmmword ptr [rbp-0F0],xmm4
       mov       rax,0FFFFFFFFFFFFFF40
M05_L00:
       vmovdqa   xmmword ptr [rbp+rax-20],xmm4
       vmovdqa   xmmword ptr [rbp+rax-10],xmm4
       vmovdqa   xmmword ptr [rax+rbp],xmm4
       add       rax,30
       jne       short M05_L00
       mov       [rbp-20],rax
       mov       [rbp+10],rcx
       mov       [rbp+18],rdx
       mov       [rbp+20],r8d
       cmp       qword ptr [rbp+18],0
       jne       short M05_L01
       mov       ecx,2
       call      qword ptr [7FF97785FD38]
M05_L01:
       mov       rax,[rbp+18]
       mov       ecx,[rax+8]
       call      qword ptr [7FF977BDCB28]; System.Buffers.Utilities.SelectBucketIndex(Int32)
       mov       [rbp-14],eax
       mov       ecx,5
       call      qword ptr [7FF977BDCB40]; System.Runtime.CompilerServices.StaticsHelpers.GetOptimizedGCThreadStaticBase(Int32)
       mov       rax,[rax+10]
       mov       [rbp-40],rax
       mov       rax,[rbp-40]
       mov       [rbp-48],rax
       cmp       qword ptr [rbp-40],0
       jne       short M05_L02
       mov       rcx,[rbp+10]
       call      qword ptr [7FF977D2D9F8]; System.Buffers.SharedArrayPool`1[[AuroraScript.Runtime.ScriptDatum, AuroraScript]].InitializeTlsBucketsAndTrimming()
       mov       [rbp-48],rax
M05_L02:
       mov       rax,[rbp-48]
       mov       [rbp-20],rax
       xor       eax,eax
       mov       [rbp-24],eax
       mov       dword ptr [rbp-28],1
       mov       rax,[rbp-20]
       mov       eax,[rax+8]
       cmp       eax,[rbp-14]
       jbe       near ptr M05_L06
       mov       dword ptr [rbp-24],1
       movzx     eax,byte ptr [rbp+20]
       test      eax,eax
       je        short M05_L03
       mov       rcx,[rbp+18]
       call      qword ptr [7FF977C5D410]
M05_L03:
       mov       rax,[rbp+18]
       mov       eax,[rax+8]
       mov       [rbp-0C0],eax
       mov       ecx,[rbp-14]
       call      qword ptr [7FF977BDCBB8]; System.Buffers.Utilities.GetMaxSizeForBucket(Int32)
       cmp       eax,[rbp-0C0]
       je        short M05_L04
       mov       rcx,offset MT_System.ArgumentException
       call      CORINFO_HELP_NEWSFAST
       mov       [rbp-0F0],rax
       call      qword ptr [7FF977D26100]
       mov       [rbp-0F8],rax
       mov       ecx,29B
       mov       rdx,7FF977794000
       call      qword ptr [7FF97785F210]
       mov       [rbp-100],rax
       mov       rdx,[rbp-0F8]
       mov       r8,[rbp-100]
       mov       rcx,[rbp-0F0]
       call      qword ptr [7FF977B4D128]
       mov       rcx,[rbp-0F0]
       call      CORINFO_HELP_THROW
       int       3
M05_L04:
       mov       rax,[rbp-20]
       mov       ecx,[rbp-14]
       cmp       ecx,[rax+8]
       jae       near ptr M05_L12
       mov       edx,ecx
       imul      rdx,10
       lea       rax,[rax+rdx+10]
       mov       [rbp-0C8],rax
       mov       rax,[rbp-0C8]
       mov       rax,[rax]
       mov       [rbp-38],rax
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rbp-0D8],xmm0
       lea       rcx,[rbp-0D8]
       mov       rdx,[rbp+18]
       call      qword ptr [7FF977D26118]; System.Buffers.SharedArrayPoolThreadLocalArray..ctor(System.Array)
       mov       rdi,[rbp-0C8]
       lea       rsi,[rbp-0D8]
       call      CORINFO_HELP_ASSIGN_BYREF
       movsq
       cmp       qword ptr [rbp-38],0
       je        short M05_L06
       mov       rax,[rbp+10]
       mov       rax,[rax+10]
       mov       ecx,[rbp-14]
       cmp       ecx,[rax+8]
       jae       near ptr M05_L12
       mov       edx,ecx
       lea       rax,[rax+rdx*8+10]
       mov       rax,[rax]
       mov       [rbp-0E0],rax
       mov       rax,[rbp-0E0]
       mov       [rbp-0E8],rax
       cmp       qword ptr [rbp-0E0],0
       jne       short M05_L05
       mov       rcx,[rbp+10]
       mov       edx,[rbp-14]
       call      qword ptr [7FF977D2DA10]
       mov       [rbp-0E8],rax
M05_L05:
       mov       rcx,[rbp-0E8]
       mov       rdx,[rbp-38]
       cmp       [rcx],ecx
       call      qword ptr [7FF977D26148]
       mov       [rbp-28],eax
M05_L06:
       mov       rax,0CE390002B8
       mov       rax,[rax]
       mov       [rbp-30],rax
       mov       rcx,[rbp-30]
       cmp       [rcx],ecx
       call      qword ptr [7FF977BDCB58]; System.Diagnostics.Tracing.EventSource.IsEnabled()
       test      eax,eax
       je        near ptr M05_L11
       mov       rax,[rbp+18]
       cmp       dword ptr [rax+8],0
       je        near ptr M05_L11
       mov       rcx,[rbp+18]
       mov       rax,[rbp+18]
       mov       rax,[rax]
       mov       rax,[rax+40]
       call      qword ptr [rax+18]
       mov       [rbp-4C],eax
       mov       rax,[rbp+18]
       mov       eax,[rax+8]
       mov       [rbp-50],eax
       mov       rcx,[rbp+10]
       call      qword ptr [7FF977BDCB70]
       mov       [rbp-104],eax
       mov       r9d,[rbp-104]
       mov       edx,[rbp-4C]
       mov       r8d,[rbp-50]
       mov       rcx,[rbp-30]
       cmp       [rcx],ecx
       call      qword ptr [7FF977D26160]
       mov       eax,[rbp-24]
       and       eax,[rbp-28]
       test      eax,eax
       jne       near ptr M05_L11
       mov       rcx,[rbp+18]
       mov       rax,[rbp+18]
       mov       rax,[rax]
       mov       rax,[rax+40]
       call      qword ptr [rax+18]
       mov       [rbp-54],eax
       mov       rax,[rbp+18]
       mov       eax,[rax+8]
       mov       [rbp-58],eax
       mov       rax,[rbp-30]
       mov       [rbp-60],rax
       mov       eax,[rbp-54]
       mov       [rbp-64],eax
       mov       eax,[rbp-58]
       mov       [rbp-68],eax
       mov       rcx,[rbp+10]
       call      qword ptr [7FF977BDCB70]
       mov       [rbp-6C],eax
       cmp       dword ptr [rbp-24],0
       jne       short M05_L07
       mov       rax,[rbp-60]
       mov       [rbp-78],rax
       mov       eax,[rbp-64]
       mov       [rbp-7C],eax
       mov       eax,[rbp-68]
       mov       [rbp-80],eax
       mov       eax,[rbp-6C]
       mov       [rbp-84],eax
       mov       dword ptr [rbp-88],0FFFFFFFF
       jmp       short M05_L08
M05_L07:
       mov       rax,[rbp-60]
       mov       [rbp-78],rax
       mov       eax,[rbp-64]
       mov       [rbp-7C],eax
       mov       eax,[rbp-68]
       mov       [rbp-80],eax
       mov       eax,[rbp-6C]
       mov       [rbp-84],eax
       mov       eax,[rbp-14]
       mov       [rbp-88],eax
M05_L08:
       mov       rax,[rbp-78]
       mov       [rbp-90],rax
       mov       eax,[rbp-7C]
       mov       [rbp-94],eax
       mov       eax,[rbp-80]
       mov       [rbp-98],eax
       mov       eax,[rbp-84]
       mov       [rbp-9C],eax
       mov       eax,[rbp-88]
       mov       [rbp-0A0],eax
       cmp       dword ptr [rbp-24],0
       jne       short M05_L09
       mov       rax,[rbp-90]
       mov       [rbp-0A8],rax
       mov       eax,[rbp-94]
       mov       [rbp-0AC],eax
       mov       eax,[rbp-98]
       mov       [rbp-0B0],eax
       mov       eax,[rbp-9C]
       mov       [rbp-0B4],eax
       mov       eax,[rbp-0A0]
       mov       [rbp-0B8],eax
       mov       dword ptr [rbp-0BC],1
       jmp       short M05_L10
M05_L09:
       mov       rax,[rbp-90]
       mov       [rbp-0A8],rax
       mov       eax,[rbp-94]
       mov       [rbp-0AC],eax
       mov       eax,[rbp-98]
       mov       [rbp-0B0],eax
       mov       eax,[rbp-9C]
       mov       [rbp-0B4],eax
       mov       eax,[rbp-0A0]
       mov       [rbp-0B8],eax
       xor       eax,eax
       mov       [rbp-0BC],eax
M05_L10:
       mov       eax,[rbp-0B8]
       mov       [rsp+20],eax
       mov       eax,[rbp-0BC]
       mov       [rsp+28],eax
       mov       rcx,[rbp-0A8]
       mov       edx,[rbp-0AC]
       mov       r8d,[rbp-0B0]
       mov       r9d,[rbp-0B4]
       cmp       [rcx],ecx
       call      qword ptr [7FF977D26178]
M05_L11:
       nop
       add       rsp,130
       pop       rsi
       pop       rdi
       pop       rbp
       ret
M05_L12:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 1134
```
```assembly
; System.Array.Clear(System.Array, Int32, Int32)
       sub       rsp,28
       test      rcx,rcx
       je        near ptr M06_L02
       lea       rax,[rcx+10]
       xor       r10d,r10d
       mov       r9,[rcx]
       mov       r11d,[r9+4]
       cmp       r11d,18
       je        short M06_L00
       add       r11d,0FFFFFFE8
       shr       r11d,3
       movsxd    r10,r11d
       mov       r10d,[rax+r10*4]
       shl       r11d,3
       movsxd    r11,r11d
       add       rax,r11
M06_L00:
       mov       r11d,edx
       sub       r11d,r10d
       cmp       edx,r10d
       jl        short M06_L03
       mov       edx,r11d
       or        edx,r8d
       jl        short M06_L03
       lea       edx,[r11+r8]
       cmp       edx,[rcx+8]
       ja        short M06_L03
       movzx     edx,word ptr [r9]
       mov       ecx,r11d
       imul      rcx,rdx
       add       rcx,rax
       mov       eax,r8d
       imul      rdx,rax
       test      dword ptr [r9],1000000
       jne       short M06_L01
       call      qword ptr [7FF96E84D930]; Precode of System.SpanHelpers.ClearWithoutReferences(Byte ByRef, UIntPtr)
       nop
       add       rsp,28
       ret
M06_L01:
       shr       rdx,3
       lea       rax,[System.Reflection.CustomAttributeExtensions.GetCustomAttribute[[System.__Canon, System.Private.CoreLib]](System.Reflection.Assembly)]
       add       rsp,28
       jmp       qword ptr [rax]
M06_L02:
       mov       ecx,2
       call      qword ptr [7FF96E84F3C8]
       int       3
M06_L03:
       call      qword ptr [7FF96E84F2B0]
       int       3
; Total bytes of code 165
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
       je        short M08_L00
       mov       rcx,rsi
       cmp       [r8],rcx
       je        short M08_L00
       mov       rdx,rax
       call      qword ptr [7FF96E839090]; Precode of System.Runtime.CompilerServices.CastHelpers.ChkCastAny(Void*, System.Object)
       mov       r8,rax
M08_L00:
       mov       rax,r8
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
; Total bytes of code 88
```
```assembly
; System.Buffers.Utilities.SelectBucketIndex(Int32)
       dec       ecx
       or        ecx,0F
       bsr       eax,ecx
       add       eax,0FFFFFFFD
       ret
; Total bytes of code 12
```
```assembly
; System.Runtime.CompilerServices.StaticsHelpers.GetOptimizedGCThreadStaticBase(Int32)
       push      rbx
       sub       rsp,20
       mov       ebx,ecx
       call      qword ptr [7FF96E851D18]; Precode of System.Threading.Thread.GetThreadStaticsBase()
       mov       ecx,ebx
       and       ecx,0FFFFFF
       mov       edx,ecx
       mov       r8d,ebx
       sar       r8d,18
       jne       short M10_L01
       cmp       [rax],ecx
       jle       short M10_L03
       mov       rax,[rax+8]
       cmp       [rax],al
       add       edx,0FFFFFFFE
       movsxd    rcx,edx
       mov       rax,[rax+rcx*8+10]
       test      rax,rax
       je        short M10_L03
M10_L00:
       add       rsp,20
       pop       rbx
       ret
M10_L01:
       mov       ecx,ebx
       sar       ecx,18
       cmp       ecx,2
       jne       short M10_L02
       movsxd    rcx,edx
       add       rax,rcx
       jmp       short M10_L00
M10_L02:
       cmp       [rax+4],edx
       jle       short M10_L03
       mov       rcx,[rax+10]
       movsxd    rax,edx
       mov       rcx,[rcx+rax*8]
       test      rcx,rcx
       je        short M10_L03
       mov       rax,[rcx]
       test      rax,rax
       je        short M10_L03
       jmp       short M10_L00
M10_L03:
       mov       ecx,ebx
       lea       rax,[System.Reflection.CustomAttributeExtensions.GetCustomAttribute[[System.__Canon, System.Private.CoreLib]](System.Reflection.Assembly)]
       add       rsp,20
       pop       rbx
       jmp       qword ptr [rax]
; Total bytes of code 130
```
```assembly
; System.Buffers.SharedArrayPool`1[[AuroraScript.Runtime.ScriptDatum, AuroraScript]].InitializeTlsBucketsAndTrimming()
       push      rbp
       sub       rsp,50
       lea       rbp,[rsp+50]
       xor       eax,eax
       mov       [rbp-28],rax
       vxorps    xmm4,xmm4,xmm4
       vmovdqu   ymmword ptr [rbp-20],ymm4
       mov       [rbp+10],rcx
       mov       rcx,offset MT_System.Buffers.SharedArrayPoolThreadLocalArray[]
       mov       edx,1B
       call      CORINFO_HELP_NEWARR_1_VC
       mov       [rbp-8],rax
       mov       ecx,5
       call      qword ptr [7FF977BDCB40]; System.Runtime.CompilerServices.StaticsHelpers.GetOptimizedGCThreadStaticBase(Int32)
       lea       rcx,[rax+10]
       mov       rdx,[rbp-8]
       call      CORINFO_HELP_ASSIGN_REF
       mov       rax,[rbp+10]
       mov       rcx,[rax+8]
       mov       rdx,[rbp-8]
       xor       r8d,r8d
       cmp       [rcx],ecx
       call      qword ptr [7FF977A25F80]; System.Runtime.CompilerServices.ConditionalWeakTable`2[[System.__Canon, System.Private.CoreLib],[System.__Canon, System.Private.CoreLib]].Add(System.__Canon, System.__Canon)
       mov       rax,[rbp+10]
       cmp       [rax],al
       mov       rax,[rbp+10]
       add       rax,18
       mov       ecx,1
       xchg      cl,[rax]
       movzx     ecx,cl
       test      ecx,ecx
       jne       near ptr M11_L01
       mov       rcx,offset MT_System.Buffers.SharedArrayPool<AuroraScript.Runtime.ScriptDatum>+<>c
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rax,0CE390016E8
       mov       rax,[rax]
       mov       [rbp-10],rax
       mov       rax,[rbp-10]
       mov       [rbp-18],rax
       cmp       qword ptr [rbp-10],0
       jne       short M11_L00
       mov       rcx,offset MT_System.Func<System.Object, System.Boolean>
       call      CORINFO_HELP_NEWSFAST
       mov       [rbp-20],rax
       mov       rcx,offset MT_System.Buffers.SharedArrayPool<AuroraScript.Runtime.ScriptDatum>+<>c
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rax,0CE390016E0
       mov       rax,[rax]
       mov       [rbp-28],rax
       mov       rdx,[rbp-28]
       mov       rcx,[rbp-20]
       mov       r8,7FF977D29A28
       call      qword ptr [7FF977856BB0]; System.MulticastDelegate.CtorClosed(System.Object, IntPtr)
       mov       rcx,offset MT_System.Buffers.SharedArrayPool<AuroraScript.Runtime.ScriptDatum>+<>c
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rdx,[rbp-20]
       mov       rcx,0CE390016E8
       call      CORINFO_HELP_ASSIGN_REF
       mov       rax,[rbp-20]
       mov       [rbp-18],rax
M11_L00:
       mov       rcx,[rbp-18]
       mov       rdx,[rbp+10]
       call      qword ptr [7FF977D261C0]; System.Gen2GcCallback.Register(System.Func`2<System.Object,Boolean>, System.Object)
M11_L01:
       mov       rax,[rbp-8]
       add       rsp,50
       pop       rbp
       ret
; Total bytes of code 320
```
```assembly
; System.Buffers.Utilities.GetMaxSizeForBucket(Int32)
       mov       eax,10
       shl       eax,cl
       ret
; Total bytes of code 8
```
```assembly
; System.Buffers.SharedArrayPoolThreadLocalArray..ctor(System.Array)
       push      rbx
       mov       rbx,rcx
       mov       rcx,rbx
       call      qword ptr [7FF96E838FF0]; CORINFO_HELP_CHECKED_ASSIGN_REF
       xor       eax,eax
       mov       [rbx+8],eax
       pop       rbx
       ret
; Total bytes of code 20
```
```assembly
; System.Diagnostics.Tracing.EventSource.IsEnabled()
       movzx     eax,byte ptr [rcx+9D]
       ret
; Total bytes of code 8
```
```assembly
; System.Runtime.CompilerServices.ConditionalWeakTable`2[[System.__Canon, System.Private.CoreLib],[System.__Canon, System.Private.CoreLib]].Add(System.__Canon, System.__Canon)
       push      rbp
       push      r14
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,40
       lea       rbp,[rsp+60]
       xor       eax,eax
       mov       [rbp-30],rax
       mov       rbx,rcx
       mov       rsi,rdx
       mov       rdi,r8
       test      rsi,rsi
       je        near ptr M15_L06
       mov       rcx,[rbx+8]
       mov       [rbp-38],rcx
       xor       eax,eax
       mov       [rbp-24],eax
       test      rcx,rcx
       je        short M15_L02
       call      qword ptr [7FF96E851BD8]
       test      eax,eax
       jne       short M15_L00
       mov       rcx,[rbp-38]
       call      qword ptr [7FF96E851BE8]
M15_L00:
       mov       dword ptr [rbp-24],1
       mov       rcx,[rbx+10]
       lea       r8,[rbp-30]
       mov       rdx,rsi
       cmp       [rcx],ecx
       call      qword ptr [7FF96E85F8F8]; Precode of System.Runtime.CompilerServices.ConditionalWeakTable`2+Container[[System.__Canon, System.Private.CoreLib],[System.__Canon, System.Private.CoreLib]].FindEntry(System.__Canon, System.Object ByRef)
       cmp       eax,0FFFFFFFF
       jne       short M15_L03
       mov       r14,[rbx+10]
       mov       ecx,[r14+28]
       mov       rax,[r14+18]
       cmp       ecx,[rax+8]
       jge       short M15_L04
M15_L01:
       mov       rcx,r14
       mov       rdx,rsi
       mov       r8,rdi
       cmp       [rcx],ecx
       call      qword ptr [7FF96E85F8E8]; Precode of System.Runtime.CompilerServices.ConditionalWeakTable`2+Container[[System.__Canon, System.Private.CoreLib],[System.__Canon, System.Private.CoreLib]].CreateEntryNoResize(System.__Canon, System.__Canon)
       jmp       short M15_L05
M15_L02:
       xor       ecx,ecx
       call      qword ptr [7FF96E84C210]
       int       3
M15_L03:
       mov       ecx,36
       call      qword ptr [7FF96E84F3A8]
       int       3
M15_L04:
       mov       rcx,r14
       call      qword ptr [7FF96E85F928]
       mov       r14,rax
       lea       rcx,[rbx+10]
       mov       rdx,r14
       call      qword ptr [7FF96E838FE8]; CORINFO_HELP_ASSIGN_REF
       jmp       short M15_L01
M15_L05:
       mov       rcx,[rbp-38]
       call      qword ptr [7FF96E851C38]; Precode of System.Threading.Monitor.Exit(System.Object)
       nop
       add       rsp,40
       pop       rbx
       pop       rsi
       pop       rdi
       pop       r14
       pop       rbp
       ret
M15_L06:
       mov       ecx,4
       call      qword ptr [7FF96E84F3C8]
       int       3
       sub       rsp,28
       cmp       dword ptr [rbp-24],0
       je        short M15_L07
       mov       rcx,[rbp-38]
       call      qword ptr [7FF96E851C38]; Precode of System.Threading.Monitor.Exit(System.Object)
M15_L07:
       nop
       add       rsp,28
       ret
; Total bytes of code 252
```
```assembly
; System.MulticastDelegate.CtorClosed(System.Object, IntPtr)
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,20
       mov       rbx,rcx
       mov       rsi,rdx
       mov       rdi,r8
       test      rsi,rsi
       je        short M16_L00
       mov       rcx,7FF977CE1A6C
       call      CORINFO_HELP_COUNTPROFILE32
       lea       rcx,[rbx+8]
       mov       rdx,rsi
       call      CORINFO_HELP_ASSIGN_REF
       mov       [rbx+18],rdi
       add       rsp,20
       pop       rbx
       pop       rsi
       pop       rdi
       ret
M16_L00:
       mov       rcx,7FF977CE1A68
       call      CORINFO_HELP_COUNTPROFILE32
       call      qword ptr [7FF977C5F018]
       int       3
; Total bytes of code 82
```
```assembly
; System.Gen2GcCallback.Register(System.Func`2<System.Object,Boolean>, System.Object)
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,30
       mov       rsi,rcx
       mov       rbx,rdx
       call      qword ptr [7FF96E843658]
       mov       rdi,rax
       lea       rcx,[rdi+10]
       mov       rdx,rsi
       call      qword ptr [7FF96E838FE8]; CORINFO_HELP_ASSIGN_REF
       mov       rcx,rbx
       xor       edx,edx
       call      qword ptr [7FF96E854550]
       test      rax,rax
       je        short M17_L01
M17_L00:
       mov       [rsp+28],rax
       mov       rax,[rsp+28]
       mov       [rdi+18],rax
       add       rsp,30
       pop       rbx
       pop       rsi
       pop       rdi
       ret
M17_L01:
       mov       rcx,rbx
       xor       edx,edx
       call      qword ptr [7FF96E854558]
       jmp       short M17_L00
; Total bytes of code 86
```

## .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3 (Job: Job-BZNPUE(IterationCount=5, IterationTime=200ms, LaunchCount=1, WarmupCount=3))

```assembly
; AuroraBenchmark.BoundaryOpsBenchmarks.CheckUInt32Number()
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,28
       mov       rbx,rcx
       xor       esi,esi
       xor       edi,edi
       jmp       short M00_L04
M00_L00:
       mov       eax,esi
       add       rsp,28
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       ret
M00_L01:
       vmovq     rax,xmm0
       test      rax,rax
       jl        short M00_L05
M00_L02:
       vxorps    xmm1,xmm1,xmm1
       vmaxsd    xmm1,xmm0,xmm1
       mov       eax,0FFFFFFFF
       vcvttsd2si rcx,xmm1
       vucomisd  xmm0,qword ptr [7FF97789FAA8]
       cmovb     eax,ecx
       mov       ebp,eax
M00_L03:
       xor       esi,ebp
       inc       edi
       cmp       edi,200
       jge       short M00_L00
M00_L04:
       mov       rax,[rbx+10]
       cmp       edi,[rax+8]
       jae       short M00_L06
       vmovsd    xmm0,qword ptr [rax+rdi*8+10]
       vxorps    xmm1,xmm1,xmm1
       vucomisd  xmm0,xmm1
       jb        short M00_L05
       vmovsd    xmm1,qword ptr [7FF97789FAB0]
       vucomisd  xmm1,xmm0
       jb        short M00_L05
       vroundsd  xmm1,xmm1,xmm0,0B
       vucomisd  xmm1,xmm0
       jp        short M00_L05
       jne       short M00_L05
       vxorps    xmm1,xmm1,xmm1
       vucomisd  xmm0,xmm1
       jp        short M00_L02
       jne       short M00_L02
       jmp       short M00_L01
M00_L05:
       call      qword ptr [7FF977C25D10]
       mov       ebp,eax
       jmp       short M00_L03
M00_L06:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 164
```

## .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3 (Job: Job-BZNPUE(IterationCount=5, IterationTime=200ms, LaunchCount=1, WarmupCount=3))

```assembly
; AuroraBenchmark.BoundaryOpsBenchmarks.IsUInt32()
       sub       rsp,28
       xor       eax,eax
       mov       rcx,[rcx+18]
       xor       edx,edx
       jmp       short M00_L02
M00_L00:
       inc       eax
M00_L01:
       inc       edx
       cmp       edx,200
       jge       short M00_L03
M00_L02:
       mov       r8,rcx
       cmp       edx,[r8+8]
       jae       short M00_L04
       vmovsd    xmm0,qword ptr [r8+rdx*8+10]
       vxorps    xmm1,xmm1,xmm1
       vucomisd  xmm0,xmm1
       jb        short M00_L01
       vmovsd    xmm1,qword ptr [7FF97789F8D8]
       vucomisd  xmm1,xmm0
       jb        short M00_L01
       vroundsd  xmm1,xmm1,xmm0,0B
       vucomisd  xmm1,xmm0
       jp        short M00_L01
       jne       short M00_L01
       vxorps    xmm1,xmm1,xmm1
       vucomisd  xmm0,xmm1
       jp        short M00_L00
       jne       short M00_L00
       vmovq     r8,xmm0
       test      r8,r8
       jl        short M00_L01
       jmp       short M00_L00
M00_L03:
       add       rsp,28
       ret
M00_L04:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 115
```

## .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3 (Job: Job-BZNPUE(IterationCount=5, IterationTime=200ms, LaunchCount=1, WarmupCount=3))

```assembly
; AuroraBenchmark.BoundaryOpsBenchmarks.CheckInt64Datum()
       push      r15
       push      r14
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,48
       xor       eax,eax
       mov       [rsp+28],rax
       vxorps    xmm4,xmm4,xmm4
       vmovdqa   xmmword ptr [rsp+30],xmm4
       mov       [rsp+40],rax
       mov       rbx,rcx
       xor       esi,esi
       xor       edi,edi
M00_L00:
       mov       rcx,[rbx+20]
       cmp       edi,[rcx+8]
       jae       near ptr M00_L19
       mov       rax,rdi
       shl       rax,4
       lea       rcx,[rcx+rax+10]
       mov       rbp,[rcx]
       mov       r14,[rcx+8]
       test      rbp,rbp
       jne       near ptr M00_L06
       test      r14,r14
       je        near ptr M00_L09
       lea       rcx,[r14-1]
       cmp       rcx,1
       jbe       near ptr M00_L10
       mov       r15d,2
M00_L01:
       cmp       r15d,2
       jne       near ptr M00_L11
       mov       r8,8007FFFFFFFFFFFF
       add       r8,r14
       mov       rcx,r8
       cmp       rcx,3
       jbe       near ptr M00_L13
       vmovq     xmm0,r14
M00_L02:
       vucomisd  xmm0,qword ptr [7FF9778A04A0]
       jb        near ptr M00_L16
       vmovsd    xmm1,qword ptr [7FF9778A04A8]
       vucomisd  xmm1,xmm0
       jbe       near ptr M00_L16
       vroundsd  xmm1,xmm1,xmm0,0B
       vucomisd  xmm1,xmm0
       jp        near ptr M00_L16
       jne       near ptr M00_L16
       vxorps    xmm1,xmm1,xmm1
       vucomisd  xmm0,xmm1
       jp        short M00_L03
       je        near ptr M00_L15
M00_L03:
       cmp       r8,3
       jbe       near ptr M00_L17
       vmovq     xmm0,r14
M00_L04:
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       r8,7FFFFFFFFFFFFFFF
       vcvttsd2si rcx,xmm1
       vucomisd  xmm0,qword ptr [7FF9778A04A8]
       cmovb     r8,rcx
       mov       r14,r8
M00_L05:
       xor       rsi,r14
       inc       edi
       cmp       edi,200
       jl        near ptr M00_L00
       mov       rax,rsi
       add       rsp,48
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       pop       r15
       ret
M00_L06:
       mov       rcx,offset MT_AuroraScript.Runtime.ScriptDatum
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001400450
       cmp       rbp,[rcx]
       jne       short M00_L07
       mov       r15d,1000
       jmp       near ptr M00_L01
M00_L07:
       mov       rcx,offset MT_AuroraScript.Runtime.ScriptDatum
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       r8,8001400458
       cmp       rbp,[r8]
       jne       short M00_L08
       mov       r15d,2000
       jmp       near ptr M00_L01
M00_L08:
       movsx     r15,r14w
       jmp       near ptr M00_L01
M00_L09:
       xor       r15d,r15d
       jmp       near ptr M00_L01
M00_L10:
       mov       r15d,1
       jmp       near ptr M00_L01
M00_L11:
       cmp       r15d,1000
       je        short M00_L12
       cmp       r15d,2000
       jne       short M00_L16
       test      r14,r14
       jl        short M00_L16
M00_L12:
       jmp       near ptr M00_L05
M00_L13:
       cmp       rcx,2
       jbe       short M00_L14
       vmovsd    xmm0,qword ptr [7FF9778A04B0]
       jmp       near ptr M00_L02
M00_L14:
       vmovq     xmm0,rcx
       jmp       near ptr M00_L02
M00_L15:
       vmovq     rcx,xmm0
       not       rcx
       shr       rcx,3F
       test      ecx,ecx
       jne       near ptr M00_L03
M00_L16:
       mov       [rsp+28],rbp
       mov       [rsp+30],r14
       lea       r8,[rsp+28]
       lea       rcx,[rsp+38]
       mov       edx,13
       call      qword ptr [7FF977C25DA0]
       xor       r14d,r14d
       jmp       near ptr M00_L05
M00_L17:
       cmp       r8,2
       jbe       short M00_L18
       vmovsd    xmm0,qword ptr [7FF9778A04B0]
       jmp       near ptr M00_L04
M00_L18:
       vmovq     xmm0,r8
       jmp       near ptr M00_L04
M00_L19:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 572
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
; AuroraBenchmark.BoundaryOpsBenchmarks.CheckUInt64Datum()
       push      r15
       push      r14
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,48
       xor       eax,eax
       mov       [rsp+28],rax
       vxorps    xmm4,xmm4,xmm4
       vmovdqa   xmmword ptr [rsp+30],xmm4
       mov       [rsp+40],rax
       mov       rbx,rcx
       xor       esi,esi
       xor       edi,edi
M00_L00:
       mov       rcx,[rbx+20]
       cmp       edi,[rcx+8]
       jae       near ptr M00_L20
       mov       rax,rdi
       shl       rax,4
       lea       rcx,[rcx+rax+10]
       mov       rbp,[rcx]
       mov       r14,[rcx+8]
       test      rbp,rbp
       jne       near ptr M00_L06
       test      r14,r14
       je        near ptr M00_L09
       lea       rcx,[r14-1]
       cmp       rcx,1
       jbe       near ptr M00_L10
       mov       r15d,2
M00_L01:
       cmp       r15d,2
       jne       near ptr M00_L11
       mov       r8,8007FFFFFFFFFFFF
       add       r8,r14
       mov       rcx,r8
       cmp       rcx,3
       jbe       near ptr M00_L14
       vmovq     xmm0,r14
M00_L02:
       vxorps    xmm1,xmm1,xmm1
       vucomisd  xmm0,xmm1
       jb        near ptr M00_L17
       vmovsd    xmm1,qword ptr [7FF9778C0518]
       vucomisd  xmm1,xmm0
       jbe       near ptr M00_L17
       vroundsd  xmm1,xmm1,xmm0,0B
       vucomisd  xmm1,xmm0
       jp        near ptr M00_L17
       jne       near ptr M00_L17
       vxorps    xmm1,xmm1,xmm1
       vucomisd  xmm0,xmm1
       jp        short M00_L03
       je        near ptr M00_L16
M00_L03:
       cmp       r8,3
       jbe       near ptr M00_L18
       vmovq     xmm0,r14
M00_L04:
       vxorps    xmm1,xmm1,xmm1
       vmaxsd    xmm1,xmm0,xmm1
       vmovsd    xmm2,qword ptr [7FF9778C0518]
       vsubsd    xmm3,xmm0,xmm2
       vcvttsd2si r8,xmm1
       vcvttsd2si rcx,xmm3
       mov       rdx,r8
       sar       rdx,3F
       and       rcx,rdx
       mov       rdx,0FFFFFFFFFFFFFFFF
       or        r8,rcx
       vucomisd  xmm0,xmm2
       cmovb     rdx,r8
       mov       r14,rdx
M00_L05:
       xor       rsi,r14
       inc       edi
       cmp       edi,200
       jl        near ptr M00_L00
       mov       rax,rsi
       add       rsp,48
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       pop       r15
       ret
M00_L06:
       mov       rcx,offset MT_AuroraScript.Runtime.ScriptDatum
       call      qword ptr [7FF977845728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001400450
       cmp       rbp,[rcx]
       jne       short M00_L07
       mov       r15d,1000
       jmp       near ptr M00_L01
M00_L07:
       mov       rcx,offset MT_AuroraScript.Runtime.ScriptDatum
       call      qword ptr [7FF977845728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       r8,8001400458
       cmp       rbp,[r8]
       jne       short M00_L08
       mov       r15d,2000
       jmp       near ptr M00_L01
M00_L08:
       movsx     r15,r14w
       jmp       near ptr M00_L01
M00_L09:
       xor       r15d,r15d
       jmp       near ptr M00_L01
M00_L10:
       mov       r15d,1
       jmp       near ptr M00_L01
M00_L11:
       cmp       r15d,1000
       je        short M00_L12
       cmp       r15d,2000
       jne       short M00_L17
       jmp       short M00_L13
M00_L12:
       test      r14,r14
       jl        short M00_L17
M00_L13:
       jmp       near ptr M00_L05
M00_L14:
       cmp       rcx,2
       jbe       short M00_L15
       vmovsd    xmm0,qword ptr [7FF9778C0520]
       jmp       near ptr M00_L02
M00_L15:
       vmovq     xmm0,rcx
       jmp       near ptr M00_L02
M00_L16:
       vmovq     rcx,xmm0
       not       rcx
       shr       rcx,3F
       test      ecx,ecx
       jne       near ptr M00_L03
M00_L17:
       mov       [rsp+28],rbp
       mov       [rsp+30],r14
       lea       r8,[rsp+28]
       lea       rcx,[rsp+38]
       mov       edx,14
       call      qword ptr [7FF977C45D88]
       xor       r14d,r14d
       jmp       near ptr M00_L05
M00_L18:
       cmp       r8,2
       jbe       short M00_L19
       vmovsd    xmm0,qword ptr [7FF9778C0520]
       jmp       near ptr M00_L04
M00_L19:
       vmovq     xmm0,r8
       jmp       near ptr M00_L04
M00_L20:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 599
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

