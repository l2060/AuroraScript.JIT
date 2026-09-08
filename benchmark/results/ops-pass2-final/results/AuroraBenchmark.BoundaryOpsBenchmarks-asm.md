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
       jmp       near ptr M00_L08
M00_L01:
       mov       rdx,offset MT_AuroraScript.Runtime.Types.ScriptArray
       cmp       [r14],rdx
       jne       near ptr M00_L12
M00_L02:
       test      r14,r14
       je        near ptr M00_L17
       cmp       qword ptr [rsp+70],0
       jne       near ptr M00_L14
       cmp       qword ptr [rsp+78],2
       jbe       near ptr M00_L14
       mov       rcx,[rsp+78]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       near ptr M00_L10
       vmovq     xmm0,rcx
M00_L03:
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       rcx,7FFFFFFFFFFFFFFF
       vcvttsd2si rdx,xmm1
       vucomisd  xmm0,qword ptr [7FF9778C1DA8]
       cmovb     rcx,rdx
       mov       [rsp+68],rcx
M00_L04:
       mov       r8,offset MT_AuroraScript.Runtime.Types.ScriptArray
       cmp       [r14],r8
       jne       near ptr M00_L16
       mov       edx,[rsp+68]
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rsp+58],xmm0
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rsp+58],xmm0
       cmp       edx,[r14+34]
       jae       near ptr M00_L15
       mov       r8,[r14+38]
       cmp       edx,[r8+8]
       jae       near ptr M00_L102
       mov       ecx,edx
       shl       rcx,4
       vmovdqu   xmm0,xmmword ptr [r8+rcx+10]
       vmovdqu   xmmword ptr [rsp+58],xmm0
M00_L05:
       mov       r15,[rsp+60]
M00_L06:
       mov       rax,8007FFFFFFFFFFFF
       add       rax,r15
       cmp       rax,3
       jbe       near ptr M00_L11
       vmovq     xmm0,r15
M00_L07:
       vaddsd    xmm6,xmm6,xmm0
       inc       esi
       cmp       esi,200
       jge       short M00_L09
M00_L08:
       mov       rdi,[rbx+48]
       mov       rbp,[rbx+50]
       mov       rdx,[rbx+8]
       mov       ecx,esi
       and       ecx,1F
       cmp       ecx,[rdx+8]
       jae       near ptr M00_L102
       shl       rcx,4
       vmovdqu   xmm0,xmmword ptr [rdx+rcx+10]
       vmovdqu   xmmword ptr [rsp+70],xmm0
       xor       edx,edx
       mov       [rsp+68],rdx
       mov       r14,rdi
       test      r14,r14
       je        near ptr M00_L02
       jmp       near ptr M00_L01
M00_L09:
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
M00_L10:
       cmp       rdx,2
       ja        short M00_L13
       vmovq     xmm0,rdx
       jmp       near ptr M00_L03
M00_L11:
       cmp       rax,2
       ja        near ptr M00_L101
       vmovq     xmm0,rax
       jmp       near ptr M00_L07
M00_L12:
       mov       rdx,rdi
       mov       rcx,offset MT_AuroraScript.Runtime.Types.IAuroraNativeIndexer
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfInterface(Void*, System.Object)
       mov       r14,rax
       jmp       near ptr M00_L02
M00_L13:
       vmovsd    xmm0,qword ptr [7FF9778C1DB0]
       jmp       near ptr M00_L03
M00_L14:
       lea       rcx,[rsp+70]
       lea       rdx,[rsp+68]
       call      qword ptr [7FF977C45DB8]
       test      eax,eax
       je        short M00_L17
       jmp       near ptr M00_L04
M00_L15:
       lea       r8,[rsp+58]
       mov       rcx,r14
       call      qword ptr [7FF977C45E18]
       jmp       near ptr M00_L05
M00_L16:
       lea       rdx,[rsp+80]
       mov       rcx,r14
       mov       r8d,[rsp+68]
       mov       r11,7FF9777904E0
       call      qword ptr [r11]
       jmp       near ptr M00_L100
M00_L17:
       mov       rdx,rdi
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptPackedArray
       call      qword ptr [7FF977846850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r15,rax
       test      r15,r15
       je        near ptr M00_L70
       cmp       qword ptr [rsp+70],0
       jne       short M00_L21
       cmp       qword ptr [rsp+78],2
       jbe       short M00_L21
       mov       rcx,[rsp+78]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L18
       vmovq     xmm0,rcx
       jmp       short M00_L20
M00_L18:
       cmp       rdx,2
       jbe       short M00_L19
       vmovsd    xmm0,qword ptr [7FF9778C1DB0]
       jmp       short M00_L20
M00_L19:
       vmovq     xmm0,rdx
M00_L20:
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       rcx,7FFFFFFFFFFFFFFF
       vcvttsd2si rdx,xmm1
       vucomisd  xmm0,qword ptr [7FF9778C1DA8]
       cmovb     rcx,rdx
       mov       [rsp+68],rcx
       jmp       short M00_L22
M00_L21:
       lea       rcx,[rsp+70]
       lea       rdx,[rsp+68]
       call      qword ptr [7FF977C45DB8]
       test      eax,eax
       je        near ptr M00_L70
M00_L22:
       mov       edi,[rsp+68]
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt32Array
       call      qword ptr [7FF977846850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       near ptr M00_L23
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt8Array
       call      qword ptr [7FF977846850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       near ptr M00_L27
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptFloat32Array
       call      qword ptr [7FF977846850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       near ptr M00_L31
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptFloat64Array
       call      qword ptr [7FF977846850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       near ptr M00_L37
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptBooleanArray
       call      qword ptr [7FF977846850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       near ptr M00_L43
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptUInt8Array
       call      qword ptr [7FF977846850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       near ptr M00_L47
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt16Array
       call      qword ptr [7FF977846850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       near ptr M00_L51
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptUInt16Array
       call      qword ptr [7FF977846850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       near ptr M00_L55
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptUInt32Array
       call      qword ptr [7FF977846850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       near ptr M00_L59
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt64Array
       call      qword ptr [7FF977846850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       near ptr M00_L63
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptUInt64Array
       call      qword ptr [7FF977846850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       near ptr M00_L65
       mov       rcx,r15
       mov       rax,[r15]
       mov       rax,[rax+58]
       call      qword ptr [rax+8]
       cmp       eax,edi
       ja        near ptr M00_L68
       jmp       near ptr M00_L67
M00_L23:
       mov       rdx,[rbp+38]
       cmp       [rdx+8],edi
       ja        short M00_L24
       mov       rdx,[rbp+38]
       mov       edx,[rdx+8]
       mov       ecx,edi
       call      qword ptr [7FF977C4C5A0]
M00_L24:
       mov       rcx,[rbp+38]
       cmp       edi,[rcx+8]
       jae       near ptr M00_L102
       mov       eax,edi
       mov       ecx,[rcx+rax*4+10]
       test      ecx,ecx
       je        short M00_L25
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2sd xmm0,xmm0,ecx
       vmovq     r15,xmm0
       jmp       short M00_L26
M00_L25:
       mov       r15,7FF8000000000001
M00_L26:
       jmp       near ptr M00_L69
M00_L27:
       mov       rdx,[rbp+38]
       cmp       [rdx+8],edi
       ja        short M00_L28
       mov       rdx,[rbp+38]
       mov       edx,[rdx+8]
       mov       ecx,edi
       call      qword ptr [7FF977C4C5A0]
M00_L28:
       mov       rdx,[rbp+38]
       cmp       edi,[rdx+8]
       jae       near ptr M00_L102
       mov       ecx,edi
       movsx     rdx,byte ptr [rdx+rcx+10]
       test      edx,edx
       je        short M00_L29
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2sd xmm0,xmm0,edx
       vmovq     r15,xmm0
       jmp       short M00_L30
M00_L29:
       mov       r15,7FF8000000000001
M00_L30:
       jmp       near ptr M00_L69
M00_L31:
       mov       rdx,[rbp+38]
       cmp       [rdx+8],edi
       ja        short M00_L32
       mov       rdx,[rbp+38]
       mov       edx,[rdx+8]
       mov       ecx,edi
       call      qword ptr [7FF977C4C5A0]
M00_L32:
       mov       rdx,[rbp+38]
       cmp       edi,[rdx+8]
       jae       near ptr M00_L102
       mov       ecx,edi
       vcvtss2sd xmm0,xmm0,dword ptr [rdx+rcx*4+10]
       vucomisd  xmm0,xmm0
       jp        short M00_L33
       je        short M00_L34
M00_L33:
       mov       r15,7FF8000000000004
       jmp       short M00_L36
M00_L34:
       vmovq     r15,xmm0
       cmp       r15,2
       jbe       short M00_L35
       jmp       short M00_L36
M00_L35:
       mov       rdx,7FF8000000000001
       add       r15,rdx
M00_L36:
       jmp       near ptr M00_L69
M00_L37:
       mov       rdx,[rbp+38]
       cmp       [rdx+8],edi
       ja        short M00_L38
       mov       rdx,[rbp+38]
       mov       edx,[rdx+8]
       mov       ecx,edi
       call      qword ptr [7FF977C4C5A0]
M00_L38:
       mov       rdx,[rbp+38]
       cmp       edi,[rdx+8]
       jae       near ptr M00_L102
       mov       ecx,edi
       vmovsd    xmm0,qword ptr [rdx+rcx*8+10]
       vucomisd  xmm0,xmm0
       jp        short M00_L39
       je        short M00_L40
M00_L39:
       mov       r15,7FF8000000000004
       jmp       short M00_L42
M00_L40:
       vmovq     r15,xmm0
       cmp       r15,2
       jbe       short M00_L41
       jmp       short M00_L42
M00_L41:
       mov       rdx,7FF8000000000001
       add       r15,rdx
M00_L42:
       jmp       near ptr M00_L69
M00_L43:
       mov       rdx,[rbp+38]
       cmp       [rdx+8],edi
       ja        short M00_L44
       mov       rdx,[rbp+38]
       mov       edx,[rdx+8]
       mov       ecx,edi
       call      qword ptr [7FF977C4C5A0]
M00_L44:
       mov       rdx,[rbp+38]
       cmp       edi,[rdx+8]
       jae       near ptr M00_L102
       mov       ecx,edi
       movzx     edx,byte ptr [rdx+rcx+10]
       test      edx,edx
       jne       short M00_L45
       mov       r15d,1
       jmp       short M00_L46
M00_L45:
       mov       r15d,2
M00_L46:
       jmp       near ptr M00_L69
M00_L47:
       mov       rdx,[rbp+38]
       cmp       [rdx+8],edi
       ja        short M00_L48
       mov       rdx,[rbp+38]
       mov       edx,[rdx+8]
       mov       ecx,edi
       call      qword ptr [7FF977C4C5A0]
M00_L48:
       mov       rdx,[rbp+38]
       cmp       edi,[rdx+8]
       jae       near ptr M00_L102
       mov       ecx,edi
       movzx     edx,byte ptr [rdx+rcx+10]
       test      edx,edx
       je        short M00_L49
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2sd xmm0,xmm0,edx
       vmovq     r15,xmm0
       jmp       short M00_L50
M00_L49:
       mov       r15,7FF8000000000001
M00_L50:
       jmp       near ptr M00_L69
M00_L51:
       mov       rdx,[rbp+38]
       cmp       [rdx+8],edi
       ja        short M00_L52
       mov       rdx,[rbp+38]
       mov       edx,[rdx+8]
       mov       ecx,edi
       call      qword ptr [7FF977C4C5A0]
M00_L52:
       mov       rdx,[rbp+38]
       cmp       edi,[rdx+8]
       jae       near ptr M00_L102
       mov       ecx,edi
       movsx     rdx,word ptr [rdx+rcx*2+10]
       test      edx,edx
       je        short M00_L53
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2sd xmm0,xmm0,edx
       vmovq     r15,xmm0
       jmp       short M00_L54
M00_L53:
       mov       r15,7FF8000000000001
M00_L54:
       jmp       near ptr M00_L69
M00_L55:
       mov       rdx,[rbp+38]
       cmp       [rdx+8],edi
       ja        short M00_L56
       mov       rdx,[rbp+38]
       mov       edx,[rdx+8]
       mov       ecx,edi
       call      qword ptr [7FF977C4C5A0]
M00_L56:
       mov       rdx,[rbp+38]
       cmp       edi,[rdx+8]
       jae       near ptr M00_L102
       mov       ecx,edi
       movzx     edx,word ptr [rdx+rcx*2+10]
       test      edx,edx
       je        short M00_L57
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2sd xmm0,xmm0,edx
       vmovq     r15,xmm0
       jmp       short M00_L58
M00_L57:
       mov       r15,7FF8000000000001
M00_L58:
       jmp       near ptr M00_L69
M00_L59:
       mov       rdx,[rbp+38]
       cmp       [rdx+8],edi
       ja        short M00_L60
       mov       rdx,[rbp+38]
       mov       edx,[rdx+8]
       mov       ecx,edi
       call      qword ptr [7FF977C4C5A0]
M00_L60:
       mov       rdx,[rbp+38]
       cmp       edi,[rdx+8]
       jae       near ptr M00_L102
       mov       ecx,edi
       mov       edx,[rdx+rcx*4+10]
       test      edx,edx
       je        short M00_L61
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2sd xmm0,xmm0,rdx
       vmovq     r15,xmm0
       jmp       short M00_L62
M00_L61:
       mov       r15,7FF8000000000001
M00_L62:
       jmp       near ptr M00_L69
M00_L63:
       mov       rdx,[rbp+38]
       cmp       [rdx+8],edi
       ja        short M00_L64
       mov       rdx,[rbp+38]
       mov       edx,[rdx+8]
       mov       ecx,edi
       call      qword ptr [7FF977C4C5A0]
M00_L64:
       mov       rcx,[rbp+38]
       cmp       edi,[rcx+8]
       jae       near ptr M00_L102
       mov       eax,edi
       mov       r15,[rcx+rax*8+10]
       mov       rcx,offset MT_AuroraScript.Runtime.ScriptDatum
       call      qword ptr [7FF977845728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       jmp       short M00_L69
M00_L65:
       mov       rdx,[rbp+38]
       cmp       [rdx+8],edi
       ja        short M00_L66
       mov       rdx,[rbp+38]
       mov       edx,[rdx+8]
       mov       ecx,edi
       call      qword ptr [7FF977C4C5A0]
M00_L66:
       mov       rcx,[rbp+38]
       cmp       edi,[rcx+8]
       jae       near ptr M00_L102
       mov       eax,edi
       mov       r15,[rcx+rax*8+10]
       mov       rcx,offset MT_AuroraScript.Runtime.ScriptDatum
       call      qword ptr [7FF977845728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       jmp       short M00_L69
M00_L67:
       mov       rcx,r15
       mov       rax,[r15]
       mov       rax,[rax+58]
       call      qword ptr [rax+8]
       mov       edx,eax
       mov       ecx,edi
       call      qword ptr [7FF977C4C5A0]
M00_L68:
       lea       rdx,[rsp+48]
       mov       rcx,r15
       mov       r8d,edi
       mov       rax,[r15]
       mov       rax,[rax+58]
       call      qword ptr [rax+10]
       mov       r15,[rsp+50]
M00_L69:
       jmp       near ptr M00_L06
M00_L70:
       mov       [rsp+38],rdi
       mov       [rsp+40],rbp
       cmp       qword ptr [rsp+38],0
       je        short M00_L73
       mov       r15,[rsp+38]
       mov       rcx,offset MT_AuroraScript.Runtime.ScriptDatum
       call      qword ptr [7FF977845728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0E88CC00450
       cmp       r15,[rcx]
       jne       short M00_L71
       mov       eax,1000
       jmp       short M00_L76
M00_L71:
       mov       rdi,[rsp+38]
       mov       rcx,offset MT_AuroraScript.Runtime.ScriptDatum
       call      qword ptr [7FF977845728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0E88CC00458
       cmp       rdi,[rcx]
       jne       short M00_L72
       mov       eax,2000
       jmp       short M00_L76
M00_L72:
       movsx     rax,word ptr [rsp+40]
       jmp       short M00_L76
M00_L73:
       mov       rax,[rsp+40]
       test      rax,rax
       je        short M00_L74
       dec       rax
       cmp       rax,1
       jbe       short M00_L75
       mov       eax,2
       jmp       short M00_L76
M00_L74:
       xor       eax,eax
       jmp       short M00_L76
M00_L75:
       mov       eax,1
M00_L76:
       cmp       eax,4
       ja        short M00_L77
       mov       ecx,eax
       lea       rax,[7FF9778C1DB8]
       mov       eax,[rax+rcx*4]
       lea       rdx,[M00_L00]
       add       rax,rdx
       jmp       rax
M00_L77:
       cmp       eax,1000
       je        near ptr M00_L97
       cmp       eax,2000
       jne       near ptr M00_L98
       mov       rcx,offset MT_AuroraScript.Runtime.Types.UInt64Value
       call      CORINFO_HELP_NEWSFAST
       mov       rdi,rax
       mov       rcx,rdi
       mov       rdx,[rsp+40]
       call      qword ptr [7FF977C4C5B8]
       jmp       near ptr M00_L99
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       call      qword ptr [7FF977845728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0E88CC00410
       mov       rdi,[rcx]
       jmp       near ptr M00_L99
       cmp       qword ptr [rsp+40],2
       je        short M00_L78
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BooleanValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0E88CC01500
       mov       rdi,[rcx]
       jmp       short M00_L79
M00_L78:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BooleanValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0E88CC014F8
       mov       rdi,[rcx]
M00_L79:
       jmp       near ptr M00_L99
       mov       rcx,[rsp+40]
       mov       rax,8007FFFFFFFFFFFF
       add       rax,rcx
       cmp       rax,3
       jbe       short M00_L80
       vmovq     xmm1,rcx
       jmp       short M00_L82
M00_L80:
       cmp       rax,2
       jbe       short M00_L81
       vmovsd    xmm1,qword ptr [7FF9778C1DB0]
       jmp       short M00_L82
M00_L81:
       vmovq     xmm1,rax
M00_L82:
       vucomisd  xmm1,xmm1
       jp        short M00_L83
       je        short M00_L84
M00_L83:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0E88CC014E8
       mov       rdi,[rcx]
       jmp       near ptr M00_L96
M00_L84:
       vxorps    xmm0,xmm0,xmm0
       vucomisd  xmm1,xmm0
       jp        short M00_L85
       jne       short M00_L85
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0E88CC01498
       mov       rdi,[rcx]
       jmp       near ptr M00_L96
M00_L85:
       vucomisd  xmm1,qword ptr [7FF9778C1DD0]
       jp        short M00_L86
       jne       short M00_L86
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0E88CC014A0
       mov       rdi,[rcx]
       jmp       near ptr M00_L96
M00_L86:
       vucomisd  xmm1,qword ptr [7FF9778C1DD8]
       jp        short M00_L87
       jne       short M00_L87
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0E88CC014A8
       mov       rdi,[rcx]
       jmp       near ptr M00_L96
M00_L87:
       vucomisd  xmm1,qword ptr [7FF9778C1DE0]
       jp        short M00_L88
       jne       short M00_L88
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0E88CC014B0
       mov       rdi,[rcx]
       jmp       near ptr M00_L96
M00_L88:
       vucomisd  xmm1,qword ptr [7FF9778C1DE8]
       jp        short M00_L89
       jne       short M00_L89
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0E88CC014B8
       mov       rdi,[rcx]
       jmp       near ptr M00_L96
M00_L89:
       vucomisd  xmm1,qword ptr [7FF9778C1DF0]
       jp        short M00_L90
       jne       short M00_L90
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0E88CC014C0
       mov       rdi,[rcx]
       jmp       near ptr M00_L96
M00_L90:
       vucomisd  xmm1,qword ptr [7FF9778C1DF8]
       jp        short M00_L91
       jne       short M00_L91
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0E88CC014C8
       mov       rdi,[rcx]
       jmp       near ptr M00_L96
M00_L91:
       vucomisd  xmm1,qword ptr [7FF9778C1E00]
       jp        short M00_L92
       jne       short M00_L92
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0E88CC014D0
       mov       rdi,[rcx]
       jmp       near ptr M00_L96
M00_L92:
       vucomisd  xmm1,qword ptr [7FF9778C1E08]
       jp        short M00_L93
       jne       short M00_L93
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0E88CC014D8
       mov       rdi,[rcx]
       jmp       near ptr M00_L96
M00_L93:
       vucomisd  xmm1,qword ptr [7FF9778C1E10]
       jp        short M00_L94
       jne       short M00_L94
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0E88CC014E0
       mov       rdi,[rcx]
       jmp       short M00_L96
M00_L94:
       vmovsd    qword ptr [rsp+30],xmm1
       vucomisd  xmm1,qword ptr [7FF9778C1E18]
       jp        short M00_L95
       jne       short M00_L95
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0E88CC01490
       mov       rdi,[rcx]
       jmp       short M00_L96
M00_L95:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      CORINFO_HELP_NEWSFAST
       mov       rdi,rax
       mov       rcx,rdi
       vmovsd    xmm1,qword ptr [rsp+30]
       call      qword ptr [7FF977C4C5D0]
M00_L96:
       jmp       short M00_L99
M00_L97:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.Int64Value
       call      CORINFO_HELP_NEWSFAST
       mov       rdi,rax
       mov       rcx,rdi
       mov       rdx,[rsp+40]
       call      qword ptr [7FF977C4C5E8]
       jmp       short M00_L99
       lea       rcx,[rsp+38]
       call      qword ptr [7FF977C4C600]
       mov       rdi,rax
       jmp       short M00_L99
M00_L98:
       lea       rcx,[rsp+38]
       call      qword ptr [7FF977C4C618]
       mov       rdi,rax
M00_L99:
       vmovdqu   xmm0,xmmword ptr [rsp+70]
       vmovdqu   xmmword ptr [rsp+20],xmm0
       lea       rcx,[rsp+20]
       call      qword ptr [7FF977C45DA0]
       mov       r9,rax
       lea       rdx,[rsp+80]
       mov       rcx,rdi
       xor       r8d,r8d
       mov       rax,[rdi]
       mov       rax,[rax+50]
       call      qword ptr [rax+10]
M00_L100:
       mov       r15,[rsp+88]
       jmp       near ptr M00_L06
M00_L101:
       vmovsd    xmm0,qword ptr [7FF9778C1DB0]
       jmp       near ptr M00_L07
M00_L102:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 3142
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
       jmp       qword ptr [7FF977BC5488]; System.Runtime.CompilerServices.CastHelpers.IsInstance_Helper(Void*, System.Object)
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
       jmp       qword ptr [7FF977845C38]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBaseSlow(System.Runtime.CompilerServices.MethodTable*)
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
       jne       short M04_L00
       mov       rcx,7FF977CA8A24
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rax,rsi
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
M04_L00:
       mov       rcx,7FF977CA8A20
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rcx,rbx
       add       rsp,28
       pop       rbx
       pop       rsi
       jmp       qword ptr [7FF977A1FD68]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBaseSlow(System.Runtime.CompilerServices.MethodTable*)
; Total bytes of code 80
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
       sub       rsp,150
       vxorps    xmm4,xmm4,xmm4
       vmovdqa   xmmword ptr [rsp+20],xmm4
       mov       rax,0FFFFFFFFFFFFFEE0
M00_L00:
       vmovdqa   xmmword ptr [rsp+rax+150],xmm4
       vmovdqa   xmmword ptr [rsp+rax+160],xmm4
       vmovdqa   xmmword ptr [rsp+rax+170],xmm4
       add       rax,30
       jne       short M00_L00
       mov       rbx,rcx
M00_L01:
       xor       esi,esi
       jmp       near ptr M00_L07
M00_L02:
       mov       rdx,offset MT_AuroraScript.Runtime.Types.ScriptArray
       cmp       [r13],rdx
       jne       near ptr M00_L59
M00_L03:
       test      r13,r13
       je        near ptr M00_L10
       cmp       qword ptr [rsp+140],0
       jne       near ptr M00_L61
       cmp       qword ptr [rsp+148],2
       jbe       near ptr M00_L61
       mov       rcx,[rsp+148]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       near ptr M00_L09
       vmovq     xmm0,rcx
M00_L04:
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       rcx,7FFFFFFFFFFFFFFF
       vcvttsd2si rdx,xmm1
       vucomisd  xmm0,qword ptr [7FF9778A39C8]
       cmovb     rcx,rdx
       mov       [rsp+138],rcx
M00_L05:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptArray
       cmp       [r13],rcx
       jne       near ptr M00_L63
       mov       rcx,r13
       mov       edx,[rsp+138]
       mov       [rsp+128],r14
       mov       [rsp+130],r15
       cmp       edx,[rcx+34]
       jae       near ptr M00_L62
       mov       rcx,[rcx+38]
       cmp       edx,[rcx+8]
       jae       near ptr M00_L265
       shl       rdx,4
       lea       r14,[rcx+rdx+10]
       mov       rdx,[rsp+128]
       mov       rcx,r14
       call      CORINFO_HELP_ASSIGN_REF
       mov       r8,[rsp+130]
       mov       [r14+8],r8
M00_L06:
       inc       esi
       cmp       esi,200
       jge       short M00_L08
M00_L07:
       mov       rdi,[rbx+48]
       mov       rbp,[rbx+50]
       mov       rdx,[rbx+8]
       mov       rcx,rdx
       mov       eax,esi
       and       eax,1F
       mov       r8d,[rcx+8]
       cmp       eax,r8d
       jae       near ptr M00_L265
       shl       rax,4
       vmovdqu   xmm0,xmmword ptr [rcx+rax+10]
       vmovdqu   xmmword ptr [rsp+140],xmm0
       lea       ecx,[rsi+1]
       and       ecx,1F
       cmp       ecx,r8d
       jae       near ptr M00_L265
       shl       rcx,4
       lea       rdx,[rdx+rcx+10]
       mov       r14,[rdx]
       mov       r15,[rdx+8]
       xor       edx,edx
       mov       [rsp+138],rdx
       mov       r13,rdi
       test      r13,r13
       je        near ptr M00_L03
       jmp       near ptr M00_L02
M00_L08:
       add       rsp,150
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r13
       pop       r14
       pop       r15
       ret
M00_L09:
       cmp       rdx,2
       ja        near ptr M00_L60
       vmovq     xmm0,rdx
       jmp       near ptr M00_L04
M00_L10:
       mov       rdx,rdi
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptPackedArray
       call      qword ptr [7FF977826850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r13,rax
       test      r13,r13
       jne       near ptr M00_L64
M00_L11:
       test      rdi,rdi
       je        short M00_L15
       test      byte ptr [7FF977A69DD0],1
       je        near ptr M00_L231
M00_L12:
       mov       rcx,8001400448
       cmp       rdi,[rcx]
       je        short M00_L14
       mov       rcx,8001400450
       cmp       rdi,[rcx]
       jne       short M00_L13
       mov       eax,2000
       jmp       short M00_L18
M00_L13:
       movsx     rax,bp
       jmp       short M00_L18
M00_L14:
       mov       eax,1000
       jmp       short M00_L18
M00_L15:
       test      rbp,rbp
       je        short M00_L17
       lea       rax,[rbp-1]
       cmp       rax,1
       jbe       short M00_L16
       mov       eax,2
       jmp       short M00_L18
M00_L16:
       mov       eax,1
       jmp       short M00_L18
M00_L17:
       xor       eax,eax
M00_L18:
       cmp       eax,4
       ja        near ptr M00_L48
       mov       ecx,eax
       lea       rax,[7FF9778A39D0]
       mov       eax,[rax+rcx*4]
       lea       rdx,[M00_L01]
       add       rax,rdx
       jmp       rax
       test      rdi,rdi
       je        near ptr M00_L23
       mov       rcx,offset MT_System.String
       cmp       [rdi],rcx
       jne       short M00_L23
       cmp       dword ptr [rdi+8],0
       jne       short M00_L21
       test      byte ptr [7FF977BDE998],1
       je        near ptr M00_L261
M00_L19:
       mov       rcx,8001401520
       mov       r13,[rcx]
M00_L20:
       jmp       short M00_L25
M00_L21:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      CORINFO_HELP_NEWSFAST
       mov       r13,rax
       test      byte ptr [7FF977C71E78],1
       je        near ptr M00_L262
M00_L22:
       mov       rdx,8001401530
       mov       rdx,[rdx]
       mov       rcx,r13
       mov       r8d,1
       call      qword ptr [7FF977C2C600]
       or        dword ptr [r13+30],1
       lea       rcx,[r13+38]
       mov       rdx,rdi
       call      CORINFO_HELP_ASSIGN_REF
       jmp       short M00_L20
M00_L23:
       test      rdi,rdi
       je        short M00_L24
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       cmp       [rdi],rcx
       jne       short M00_L24
       mov       r13,rdi
       jmp       short M00_L25
M00_L24:
       xor       r13d,r13d
M00_L25:
       jmp       near ptr M00_L54
M00_L26:
       mov       r13,rax
       jmp       near ptr M00_L54
       mov       rcx,8007FFFFFFFFFFFF
       add       rcx,rbp
       cmp       rcx,3
       jbe       near ptr M00_L236
       vmovq     xmm1,rbp
M00_L27:
       vucomisd  xmm1,xmm1
       jp        near ptr M00_L40
       jne       near ptr M00_L40
       vxorps    xmm0,xmm0,xmm0
       vucomisd  xmm1,xmm0
       jp        short M00_L28
       je        near ptr M00_L38
M00_L28:
       vucomisd  xmm1,qword ptr [7FF9778A39E8]
       jp        short M00_L29
       je        short M00_L36
M00_L29:
       vucomisd  xmm1,qword ptr [7FF9778A39F0]
       jp        short M00_L30
       je        near ptr M00_L241
M00_L30:
       vucomisd  xmm1,qword ptr [7FF9778A39F8]
       jp        short M00_L31
       je        near ptr M00_L243
M00_L31:
       vucomisd  xmm1,qword ptr [7FF9778A3A00]
       jp        short M00_L32
       je        near ptr M00_L245
M00_L32:
       vucomisd  xmm1,qword ptr [7FF9778A3A08]
       jp        short M00_L33
       je        near ptr M00_L247
M00_L33:
       vucomisd  xmm1,qword ptr [7FF9778A3A10]
       jp        short M00_L34
       je        near ptr M00_L249
M00_L34:
       vucomisd  xmm1,qword ptr [7FF9778A3A18]
       jp        short M00_L35
       je        near ptr M00_L251
M00_L35:
       vucomisd  xmm1,qword ptr [7FF9778A3A20]
       jp        near ptr M00_L255
       jne       near ptr M00_L255
       jmp       near ptr M00_L253
M00_L36:
       test      byte ptr [7FF977BDD4B0],1
       je        near ptr M00_L240
M00_L37:
       mov       rcx,8001401498
       mov       r13,[rcx]
       jmp       short M00_L42
M00_L38:
       test      byte ptr [7FF977BDD4B0],1
       je        near ptr M00_L239
M00_L39:
       mov       rcx,8001401490
       mov       r13,[rcx]
       jmp       short M00_L42
M00_L40:
       test      byte ptr [7FF977BDD4B0],1
       je        near ptr M00_L238
M00_L41:
       mov       rcx,80014014E0
       mov       r13,[rcx]
M00_L42:
       jmp       near ptr M00_L54
       cmp       rbp,2
       je        short M00_L45
       test      byte ptr [7FF977BDDDB8],1
       je        near ptr M00_L234
M00_L43:
       mov       rcx,80014014F8
       mov       r13,[rcx]
M00_L44:
       jmp       near ptr M00_L54
M00_L45:
       test      byte ptr [7FF977BDDDB8],1
       je        near ptr M00_L235
M00_L46:
       mov       rcx,80014014F0
       mov       r13,[rcx]
       jmp       short M00_L44
       test      byte ptr [7FF977AABD00],1
       je        near ptr M00_L233
M00_L47:
       mov       rdx,8001400408
       mov       r13,[rdx]
       jmp       near ptr M00_L54
M00_L48:
       cmp       eax,1000
       je        near ptr M00_L57
       cmp       eax,2000
       je        near ptr M00_L55
       mov       rdx,rdi
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       call      qword ptr [7FF977826850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rax,rax
       jne       near ptr M00_L26
       test      rdi,rdi
       je        near ptr M00_L53
       mov       rcx,offset MT_System.String
       cmp       [rdi],rcx
       jne       short M00_L53
       mov       r13,rdi
       cmp       dword ptr [r13+8],0
       jne       short M00_L51
       test      byte ptr [7FF977BDE998],1
       je        near ptr M00_L263
M00_L49:
       mov       rcx,8001401520
       mov       r13,[rcx]
M00_L50:
       jmp       short M00_L54
M00_L51:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      CORINFO_HELP_NEWSFAST
       mov       rbp,rax
       test      byte ptr [7FF977C71E78],1
       je        near ptr M00_L264
M00_L52:
       mov       rdx,8001401530
       mov       rdx,[rdx]
       mov       rcx,rbp
       mov       r8d,1
       call      qword ptr [7FF977C2C600]
       or        dword ptr [rbp+30],1
       lea       rcx,[rbp+38]
       mov       rdx,r13
       call      CORINFO_HELP_ASSIGN_REF
       mov       r13,rbp
       jmp       short M00_L50
M00_L53:
       xor       r13d,r13d
M00_L54:
       vmovdqu   xmm0,xmmword ptr [rsp+140]
       vmovdqu   xmmword ptr [rsp+20],xmm0
       lea       rcx,[rsp+20]
       call      qword ptr [7FF977C25D58]
       mov       r8,rax
       mov       [rsp+20],r14
       mov       [rsp+28],r15
       lea       r9,[rsp+20]
       mov       rcx,r13
       xor       edx,edx
       mov       rax,[r13]
       mov       rax,[rax+50]
       call      qword ptr [rax+20]
       jmp       near ptr M00_L06
M00_L55:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.UInt64Value
       call      CORINFO_HELP_NEWSFAST
       mov       r13,rax
       test      byte ptr [7FF977C71DB8],1
       je        near ptr M00_L232
M00_L56:
       mov       rdx,8001401510
       mov       rdx,[rdx]
       mov       rcx,r13
       mov       r8d,1
       call      qword ptr [7FF977C2C600]
       or        dword ptr [r13+30],1
       mov       [r13+38],rbp
       jmp       near ptr M00_L54
M00_L57:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.Int64Value
       call      CORINFO_HELP_NEWSFAST
       mov       r13,rax
       test      byte ptr [7FF977C71CF8],1
       je        near ptr M00_L260
M00_L58:
       mov       rdx,8001401508
       mov       rdx,[rdx]
       mov       rcx,r13
       mov       r8d,1
       call      qword ptr [7FF977C2C600]
       or        dword ptr [r13+30],1
       mov       [r13+38],rbp
       jmp       near ptr M00_L54
M00_L59:
       mov       rdx,rdi
       mov       rcx,offset MT_AuroraScript.Runtime.Types.IAuroraNativeIndexer
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfInterface(Void*, System.Object)
       mov       r13,rax
       jmp       near ptr M00_L03
M00_L60:
       vmovsd    xmm0,qword ptr [7FF9778A3A28]
       jmp       near ptr M00_L04
M00_L61:
       lea       rcx,[rsp+140]
       lea       rdx,[rsp+138]
       call      qword ptr [7FF977C25DA0]
       test      eax,eax
       je        near ptr M00_L10
       jmp       near ptr M00_L05
M00_L62:
       lea       r8,[rsp+128]
       call      qword ptr [7FF977BAC108]
       jmp       near ptr M00_L06
M00_L63:
       mov       edx,[rsp+138]
       mov       [rsp+20],r14
       mov       [rsp+28],r15
       lea       r8,[rsp+20]
       mov       rcx,r13
       mov       r11,7FF9777704E0
       call      qword ptr [r11]
       jmp       near ptr M00_L06
M00_L64:
       cmp       qword ptr [rsp+140],0
       jne       short M00_L68
       cmp       qword ptr [rsp+148],2
       jbe       short M00_L68
       mov       rcx,[rsp+148]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L65
       vmovq     xmm0,rcx
       jmp       short M00_L67
M00_L65:
       cmp       rdx,2
       jbe       short M00_L66
       vmovsd    xmm0,qword ptr [7FF9778A3A28]
       jmp       short M00_L67
M00_L66:
       vmovq     xmm0,rdx
M00_L67:
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       rcx,7FFFFFFFFFFFFFFF
       vcvttsd2si rdx,xmm1
       vucomisd  xmm0,qword ptr [7FF9778A39C8]
       cmovb     rcx,rdx
       mov       [rsp+138],rcx
       jmp       short M00_L69
M00_L68:
       lea       rcx,[rsp+140]
       lea       rdx,[rsp+138]
       call      qword ptr [7FF977C25DA0]
       test      eax,eax
       je        near ptr M00_L11
M00_L69:
       mov       edi,[rsp+138]
       mov       rdx,r13
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt32Array
       call      qword ptr [7FF977826850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       near ptr M00_L70
       mov       rdx,r13
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt8Array
       call      qword ptr [7FF977826850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       near ptr M00_L84
       mov       rdx,r13
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptFloat32Array
       call      qword ptr [7FF977826850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       near ptr M00_L98
       mov       rdx,r13
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptFloat64Array
       call      qword ptr [7FF977826850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       near ptr M00_L112
       mov       rdx,r13
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptBooleanArray
       call      qword ptr [7FF977826850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       near ptr M00_L126
       mov       rdx,r13
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptUInt8Array
       call      qword ptr [7FF977826850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       near ptr M00_L140
       mov       rdx,r13
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt16Array
       call      qword ptr [7FF977826850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       near ptr M00_L154
       mov       rdx,r13
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptUInt16Array
       call      qword ptr [7FF977826850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       near ptr M00_L168
       mov       rdx,r13
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptUInt32Array
       call      qword ptr [7FF977826850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       near ptr M00_L182
       mov       rdx,r13
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt64Array
       call      qword ptr [7FF977826850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       near ptr M00_L196
       mov       rdx,r13
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptUInt64Array
       call      qword ptr [7FF977826850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       near ptr M00_L212
       mov       rcx,r13
       mov       rax,[r13]
       mov       rax,[rax+58]
       call      qword ptr [rax+8]
       cmp       eax,edi
       ja        near ptr M00_L230
       jmp       near ptr M00_L229
M00_L70:
       mov       rdx,[rbp+38]
       cmp       [rdx+8],edi
       ja        short M00_L71
       mov       rdx,[rbp+38]
       mov       edx,[rdx+8]
       mov       ecx,edi
       call      qword ptr [7FF977C2C540]
M00_L71:
       mov       rbp,[rbp+38]
       xor       ecx,ecx
       mov       [rsp+120],rcx
       mov       [rsp+110],r14
       mov       [rsp+118],r15
       cmp       qword ptr [rsp+110],0
       je        short M00_L72
       mov       r14,[rsp+110]
       mov       rcx,offset MT_AuroraScript.Runtime.ScriptDatum
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001400448
       cmp       r14,[rcx]
       je        short M00_L76
       mov       rcx,[rsp+110]
       mov       rdx,8001400450
       cmp       rcx,[rdx]
       je        short M00_L76
       movsx     rcx,word ptr [rsp+118]
       jmp       short M00_L75
M00_L72:
       mov       rcx,[rsp+118]
       test      rcx,rcx
       je        short M00_L73
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L74
       mov       ecx,2
       jmp       short M00_L75
M00_L73:
       xor       ecx,ecx
       jmp       short M00_L75
M00_L74:
       mov       ecx,1
M00_L75:
       test      ecx,ecx
       jne       short M00_L76
       xor       ecx,ecx
       mov       [rsp+120],rcx
       jmp       near ptr M00_L82
M00_L76:
       cmp       qword ptr [rsp+110],0
       jne       short M00_L80
       cmp       qword ptr [rsp+118],2
       jbe       short M00_L80
       mov       rcx,[rsp+118]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L77
       vmovq     xmm0,rcx
       jmp       short M00_L79
M00_L77:
       cmp       rdx,2
       jbe       short M00_L78
       vmovsd    xmm0,qword ptr [7FF9778A3A28]
       jmp       short M00_L79
M00_L78:
       vmovq     xmm0,rdx
M00_L79:
       vmovsd    qword ptr [rsp+120],xmm0
       mov       r15d,1
       jmp       short M00_L81
M00_L80:
       lea       rcx,[rsp+110]
       lea       rdx,[rsp+120]
       call      qword ptr [7FF977C2C558]
       mov       r15d,eax
M00_L81:
       test      r15d,r15d
       jne       short M00_L82
       vmovsd    xmm0,qword ptr [7FF9778A3A28]
       jmp       short M00_L83
M00_L82:
       vmovsd    xmm0,qword ptr [rsp+120]
M00_L83:
       cmp       edi,[rbp+8]
       jae       near ptr M00_L265
       mov       ecx,edi
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       eax,7FFFFFFF
       vcvttsd2si edx,xmm1
       vucomisd  xmm0,qword ptr [7FF9778A3A30]
       cmovb     eax,edx
       mov       [rbp+rcx*4+10],eax
       jmp       near ptr M00_L06
M00_L84:
       mov       rdx,[rbp+38]
       cmp       [rdx+8],edi
       ja        short M00_L85
       mov       rdx,[rbp+38]
       mov       edx,[rdx+8]
       mov       ecx,edi
       call      qword ptr [7FF977C2C540]
M00_L85:
       mov       rbp,[rbp+38]
       xor       ecx,ecx
       mov       [rsp+108],rcx
       mov       [rsp+0F8],r14
       mov       [rsp+100],r15
       cmp       qword ptr [rsp+0F8],0
       je        short M00_L86
       mov       r14,[rsp+0F8]
       mov       rcx,offset MT_AuroraScript.Runtime.ScriptDatum
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001400448
       cmp       r14,[rcx]
       je        short M00_L90
       mov       rcx,[rsp+0F8]
       mov       rdx,8001400450
       cmp       rcx,[rdx]
       je        short M00_L90
       movsx     rcx,word ptr [rsp+100]
       jmp       short M00_L89
M00_L86:
       mov       rcx,[rsp+100]
       test      rcx,rcx
       je        short M00_L87
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L88
       mov       ecx,2
       jmp       short M00_L89
M00_L87:
       xor       ecx,ecx
       jmp       short M00_L89
M00_L88:
       mov       ecx,1
M00_L89:
       test      ecx,ecx
       jne       short M00_L90
       xor       ecx,ecx
       mov       [rsp+108],rcx
       jmp       near ptr M00_L96
M00_L90:
       cmp       qword ptr [rsp+0F8],0
       jne       short M00_L94
       cmp       qword ptr [rsp+100],2
       jbe       short M00_L94
       mov       rcx,[rsp+100]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L91
       vmovq     xmm0,rcx
       jmp       short M00_L93
M00_L91:
       cmp       rdx,2
       jbe       short M00_L92
       vmovsd    xmm0,qword ptr [7FF9778A3A28]
       jmp       short M00_L93
M00_L92:
       vmovq     xmm0,rdx
M00_L93:
       vmovsd    qword ptr [rsp+108],xmm0
       mov       r15d,1
       jmp       short M00_L95
M00_L94:
       lea       rcx,[rsp+0F8]
       lea       rdx,[rsp+108]
       call      qword ptr [7FF977C2C558]
       mov       r15d,eax
M00_L95:
       test      r15d,r15d
       jne       short M00_L96
       vmovsd    xmm0,qword ptr [7FF9778A3A28]
       jmp       short M00_L97
M00_L96:
       vmovsd    xmm0,qword ptr [rsp+108]
M00_L97:
       cmp       edi,[rbp+8]
       jae       near ptr M00_L265
       mov       edx,edi
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       ecx,7FFFFFFF
       vcvttsd2si eax,xmm1
       vucomisd  xmm0,qword ptr [7FF9778A3A30]
       cmovb     ecx,eax
       mov       [rbp+rdx+10],cl
       jmp       near ptr M00_L06
M00_L98:
       mov       rdx,[rbp+38]
       cmp       [rdx+8],edi
       ja        short M00_L99
       mov       rdx,[rbp+38]
       mov       edx,[rdx+8]
       mov       ecx,edi
       call      qword ptr [7FF977C2C540]
M00_L99:
       mov       rbp,[rbp+38]
       xor       ecx,ecx
       mov       [rsp+0F0],rcx
       mov       [rsp+0E0],r14
       mov       [rsp+0E8],r15
       cmp       qword ptr [rsp+0E0],0
       je        short M00_L100
       mov       r14,[rsp+0E0]
       mov       rcx,offset MT_AuroraScript.Runtime.ScriptDatum
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001400448
       cmp       r14,[rcx]
       je        short M00_L104
       mov       rcx,[rsp+0E0]
       mov       rdx,8001400450
       cmp       rcx,[rdx]
       je        short M00_L104
       movsx     rcx,word ptr [rsp+0E8]
       jmp       short M00_L103
M00_L100:
       mov       rcx,[rsp+0E8]
       test      rcx,rcx
       je        short M00_L101
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L102
       mov       ecx,2
       jmp       short M00_L103
M00_L101:
       xor       ecx,ecx
       jmp       short M00_L103
M00_L102:
       mov       ecx,1
M00_L103:
       test      ecx,ecx
       jne       short M00_L104
       xor       ecx,ecx
       mov       [rsp+0F0],rcx
       jmp       near ptr M00_L110
M00_L104:
       cmp       qword ptr [rsp+0E0],0
       jne       short M00_L108
       cmp       qword ptr [rsp+0E8],2
       jbe       short M00_L108
       mov       rcx,[rsp+0E8]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L105
       vmovq     xmm0,rcx
       jmp       short M00_L107
M00_L105:
       cmp       rdx,2
       jbe       short M00_L106
       vmovsd    xmm0,qword ptr [7FF9778A3A28]
       jmp       short M00_L107
M00_L106:
       vmovq     xmm0,rdx
M00_L107:
       vmovsd    qword ptr [rsp+0F0],xmm0
       mov       r15d,1
       jmp       short M00_L109
M00_L108:
       lea       rcx,[rsp+0E0]
       lea       rdx,[rsp+0F0]
       call      qword ptr [7FF977C2C558]
       mov       r15d,eax
M00_L109:
       test      r15d,r15d
       jne       short M00_L110
       vmovsd    xmm0,qword ptr [7FF9778A3A28]
       jmp       short M00_L111
M00_L110:
       vmovsd    xmm0,qword ptr [rsp+0F0]
M00_L111:
       cmp       edi,[rbp+8]
       jae       near ptr M00_L265
       mov       edx,edi
       vcvtsd2ss xmm0,xmm0,xmm0
       vmovss    dword ptr [rbp+rdx*4+10],xmm0
       jmp       near ptr M00_L06
M00_L112:
       mov       rdx,[rbp+38]
       cmp       [rdx+8],edi
       ja        short M00_L113
       mov       rdx,[rbp+38]
       mov       edx,[rdx+8]
       mov       ecx,edi
       call      qword ptr [7FF977C2C540]
M00_L113:
       mov       rbp,[rbp+38]
       xor       ecx,ecx
       mov       [rsp+0D8],rcx
       mov       [rsp+0C8],r14
       mov       [rsp+0D0],r15
       cmp       qword ptr [rsp+0C8],0
       je        short M00_L114
       mov       r14,[rsp+0C8]
       mov       rcx,offset MT_AuroraScript.Runtime.ScriptDatum
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001400448
       cmp       r14,[rcx]
       je        short M00_L118
       mov       rcx,[rsp+0C8]
       mov       rdx,8001400450
       cmp       rcx,[rdx]
       je        short M00_L118
       movsx     rcx,word ptr [rsp+0D0]
       jmp       short M00_L117
M00_L114:
       mov       rcx,[rsp+0D0]
       test      rcx,rcx
       je        short M00_L115
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L116
       mov       ecx,2
       jmp       short M00_L117
M00_L115:
       xor       ecx,ecx
       jmp       short M00_L117
M00_L116:
       mov       ecx,1
M00_L117:
       test      ecx,ecx
       jne       short M00_L118
       xor       ecx,ecx
       mov       [rsp+0D8],rcx
       jmp       near ptr M00_L124
M00_L118:
       cmp       qword ptr [rsp+0C8],0
       jne       short M00_L122
       cmp       qword ptr [rsp+0D0],2
       jbe       short M00_L122
       mov       rcx,[rsp+0D0]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L119
       vmovq     xmm0,rcx
       jmp       short M00_L121
M00_L119:
       cmp       rdx,2
       jbe       short M00_L120
       vmovsd    xmm0,qword ptr [7FF9778A3A28]
       jmp       short M00_L121
M00_L120:
       vmovq     xmm0,rdx
M00_L121:
       vmovsd    qword ptr [rsp+0D8],xmm0
       mov       r15d,1
       jmp       short M00_L123
M00_L122:
       lea       rcx,[rsp+0C8]
       lea       rdx,[rsp+0D8]
       call      qword ptr [7FF977C2C558]
       mov       r15d,eax
M00_L123:
       test      r15d,r15d
       jne       short M00_L124
       vmovsd    xmm0,qword ptr [7FF9778A3A28]
       jmp       short M00_L125
M00_L124:
       vmovsd    xmm0,qword ptr [rsp+0D8]
M00_L125:
       cmp       edi,[rbp+8]
       jae       near ptr M00_L265
       mov       edx,edi
       vmovsd    qword ptr [rbp+rdx*8+10],xmm0
       jmp       near ptr M00_L06
M00_L126:
       mov       rdx,[rbp+38]
       cmp       [rdx+8],edi
       ja        short M00_L127
       mov       rdx,[rbp+38]
       mov       edx,[rdx+8]
       mov       ecx,edi
       call      qword ptr [7FF977C2C540]
M00_L127:
       mov       rbp,[rbp+38]
       mov       [rsp+0B8],r14
       mov       [rsp+0C0],r15
       cmp       qword ptr [rsp+0B8],0
       je        short M00_L130
       mov       r14,[rsp+0B8]
       mov       rcx,offset MT_AuroraScript.Runtime.ScriptDatum
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rdx,8001400448
       cmp       r14,[rdx]
       jne       short M00_L128
       mov       eax,1000
       jmp       short M00_L133
M00_L128:
       mov       rdx,[rsp+0B8]
       mov       rcx,8001400450
       cmp       rdx,[rcx]
       jne       short M00_L129
       mov       eax,2000
       jmp       short M00_L133
M00_L129:
       movsx     rax,word ptr [rsp+0C0]
       jmp       short M00_L133
M00_L130:
       mov       rax,[rsp+0C0]
       test      rax,rax
       je        short M00_L131
       dec       rax
       cmp       rax,1
       jbe       short M00_L132
       mov       eax,2
       jmp       short M00_L133
M00_L131:
       xor       eax,eax
       jmp       short M00_L133
M00_L132:
       mov       eax,1
M00_L133:
       cmp       eax,4
       ja        short M00_L134
       mov       edx,eax
       lea       rcx,[7FF9778A3A38]
       mov       ecx,[rcx+rdx*4]
       lea       rax,[M00_L01]
       add       rcx,rax
       jmp       rcx
M00_L134:
       cmp       eax,1000
       je        near ptr M00_L136
       cmp       eax,2000
       je        near ptr M00_L136
       lea       rcx,[rsp+0B8]
       call      qword ptr [7FF977C2C570]
       mov       r15,rax
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rdx,8001400408
       cmp       r15,[rdx]
       setne     r15b
       movzx     r15d,r15b
       jmp       near ptr M00_L139
       cmp       qword ptr [rsp+0C0],2
       sete      r15b
       movzx     r15d,r15b
       jmp       near ptr M00_L139
       mov       rcx,7FF8000000000001
       cmp       [rsp+0C0],rcx
       je        short M00_L135
       mov       rcx,8000000000000000
       cmp       [rsp+0C0],rcx
       je        short M00_L135
       mov       rcx,7FF8000000000004
       cmp       [rsp+0C0],rcx
       setne     r15b
       movzx     r15d,r15b
       jmp       short M00_L139
M00_L135:
       xor       r15d,r15d
       jmp       short M00_L139
M00_L136:
       cmp       qword ptr [rsp+0C0],0
       setne     r15b
       movzx     r15d,r15b
       jmp       short M00_L139
       mov       rdx,[rsp+0B8]
       mov       rcx,offset MT_System.String
       call      qword ptr [7FF977826850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rax,rax
       je        short M00_L137
       cmp       dword ptr [rax+8],0
       sete      cl
       movzx     ecx,cl
       jmp       short M00_L138
M00_L137:
       mov       ecx,1
M00_L138:
       test      ecx,ecx
       sete      r15b
       movzx     r15d,r15b
M00_L139:
       cmp       edi,[rbp+8]
       jae       near ptr M00_L265
       mov       edx,edi
       mov       [rbp+rdx+10],r15b
       jmp       near ptr M00_L06
M00_L140:
       mov       rdx,[rbp+38]
       cmp       [rdx+8],edi
       ja        short M00_L141
       mov       rdx,[rbp+38]
       mov       edx,[rdx+8]
       mov       ecx,edi
       call      qword ptr [7FF977C2C540]
M00_L141:
       mov       rbp,[rbp+38]
       xor       ecx,ecx
       mov       [rsp+0B0],rcx
       mov       [rsp+0A0],r14
       mov       [rsp+0A8],r15
       cmp       qword ptr [rsp+0A0],0
       je        short M00_L142
       mov       r14,[rsp+0A0]
       mov       rcx,offset MT_AuroraScript.Runtime.ScriptDatum
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001400448
       cmp       r14,[rcx]
       je        short M00_L146
       mov       rcx,[rsp+0A0]
       mov       rdx,8001400450
       cmp       rcx,[rdx]
       je        short M00_L146
       movsx     rcx,word ptr [rsp+0A8]
       jmp       short M00_L145
M00_L142:
       mov       rcx,[rsp+0A8]
       test      rcx,rcx
       je        short M00_L143
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L144
       mov       ecx,2
       jmp       short M00_L145
M00_L143:
       xor       ecx,ecx
       jmp       short M00_L145
M00_L144:
       mov       ecx,1
M00_L145:
       test      ecx,ecx
       jne       short M00_L146
       xor       ecx,ecx
       mov       [rsp+0B0],rcx
       jmp       near ptr M00_L152
M00_L146:
       cmp       qword ptr [rsp+0A0],0
       jne       short M00_L150
       cmp       qword ptr [rsp+0A8],2
       jbe       short M00_L150
       mov       rcx,[rsp+0A8]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L147
       vmovq     xmm0,rcx
       jmp       short M00_L149
M00_L147:
       cmp       rdx,2
       jbe       short M00_L148
       vmovsd    xmm0,qword ptr [7FF9778A3A28]
       jmp       short M00_L149
M00_L148:
       vmovq     xmm0,rdx
M00_L149:
       vmovsd    qword ptr [rsp+0B0],xmm0
       mov       r15d,1
       jmp       short M00_L151
M00_L150:
       lea       rcx,[rsp+0A0]
       lea       rdx,[rsp+0B0]
       call      qword ptr [7FF977C2C558]
       mov       r15d,eax
M00_L151:
       test      r15d,r15d
       jne       short M00_L152
       vmovsd    xmm0,qword ptr [7FF9778A3A28]
       jmp       short M00_L153
M00_L152:
       vmovsd    xmm0,qword ptr [rsp+0B0]
M00_L153:
       cmp       edi,[rbp+8]
       jae       near ptr M00_L265
       mov       edx,edi
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       ecx,7FFFFFFF
       vcvttsd2si eax,xmm1
       vucomisd  xmm0,qword ptr [7FF9778A3A30]
       cmovb     ecx,eax
       mov       [rbp+rdx+10],cl
       jmp       near ptr M00_L06
M00_L154:
       mov       rdx,[rbp+38]
       cmp       [rdx+8],edi
       ja        short M00_L155
       mov       rdx,[rbp+38]
       mov       edx,[rdx+8]
       mov       ecx,edi
       call      qword ptr [7FF977C2C540]
M00_L155:
       mov       rbp,[rbp+38]
       xor       ecx,ecx
       mov       [rsp+98],rcx
       mov       [rsp+88],r14
       mov       [rsp+90],r15
       cmp       qword ptr [rsp+88],0
       je        short M00_L156
       mov       r14,[rsp+88]
       mov       rcx,offset MT_AuroraScript.Runtime.ScriptDatum
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001400448
       cmp       r14,[rcx]
       je        short M00_L160
       mov       rcx,[rsp+88]
       mov       rdx,8001400450
       cmp       rcx,[rdx]
       je        short M00_L160
       movsx     rcx,word ptr [rsp+90]
       jmp       short M00_L159
M00_L156:
       mov       rcx,[rsp+90]
       test      rcx,rcx
       je        short M00_L157
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L158
       mov       ecx,2
       jmp       short M00_L159
M00_L157:
       xor       ecx,ecx
       jmp       short M00_L159
M00_L158:
       mov       ecx,1
M00_L159:
       test      ecx,ecx
       jne       short M00_L160
       xor       ecx,ecx
       mov       [rsp+98],rcx
       jmp       near ptr M00_L166
M00_L160:
       cmp       qword ptr [rsp+88],0
       jne       short M00_L164
       cmp       qword ptr [rsp+90],2
       jbe       short M00_L164
       mov       rcx,[rsp+90]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L161
       vmovq     xmm0,rcx
       jmp       short M00_L163
M00_L161:
       cmp       rdx,2
       jbe       short M00_L162
       vmovsd    xmm0,qword ptr [7FF9778A3A28]
       jmp       short M00_L163
M00_L162:
       vmovq     xmm0,rdx
M00_L163:
       vmovsd    qword ptr [rsp+98],xmm0
       mov       r15d,1
       jmp       short M00_L165
M00_L164:
       lea       rcx,[rsp+88]
       lea       rdx,[rsp+98]
       call      qword ptr [7FF977C2C558]
       mov       r15d,eax
M00_L165:
       test      r15d,r15d
       jne       short M00_L166
       vmovsd    xmm0,qword ptr [7FF9778A3A28]
       jmp       short M00_L167
M00_L166:
       vmovsd    xmm0,qword ptr [rsp+98]
M00_L167:
       cmp       edi,[rbp+8]
       jae       near ptr M00_L265
       mov       edx,edi
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       ecx,7FFFFFFF
       vcvttsd2si eax,xmm1
       vucomisd  xmm0,qword ptr [7FF9778A3A30]
       cmovb     ecx,eax
       mov       [rbp+rdx*2+10],cx
       jmp       near ptr M00_L06
M00_L168:
       mov       rdx,[rbp+38]
       cmp       [rdx+8],edi
       ja        short M00_L169
       mov       rdx,[rbp+38]
       mov       edx,[rdx+8]
       mov       ecx,edi
       call      qword ptr [7FF977C2C540]
M00_L169:
       mov       rbp,[rbp+38]
       xor       ecx,ecx
       mov       [rsp+80],rcx
       mov       [rsp+70],r14
       mov       [rsp+78],r15
       cmp       qword ptr [rsp+70],0
       je        short M00_L170
       mov       r14,[rsp+70]
       mov       rcx,offset MT_AuroraScript.Runtime.ScriptDatum
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001400448
       cmp       r14,[rcx]
       je        short M00_L174
       mov       rcx,[rsp+70]
       mov       rdx,8001400450
       cmp       rcx,[rdx]
       je        short M00_L174
       movsx     rcx,word ptr [rsp+78]
       jmp       short M00_L173
M00_L170:
       mov       rcx,[rsp+78]
       test      rcx,rcx
       je        short M00_L171
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L172
       mov       ecx,2
       jmp       short M00_L173
M00_L171:
       xor       ecx,ecx
       jmp       short M00_L173
M00_L172:
       mov       ecx,1
M00_L173:
       test      ecx,ecx
       jne       short M00_L174
       xor       ecx,ecx
       mov       [rsp+80],rcx
       jmp       short M00_L180
M00_L174:
       cmp       qword ptr [rsp+70],0
       jne       short M00_L178
       cmp       qword ptr [rsp+78],2
       jbe       short M00_L178
       mov       rcx,[rsp+78]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L175
       vmovq     xmm0,rcx
       jmp       short M00_L177
M00_L175:
       cmp       rdx,2
       jbe       short M00_L176
       vmovsd    xmm0,qword ptr [7FF9778A3A28]
       jmp       short M00_L177
M00_L176:
       vmovq     xmm0,rdx
M00_L177:
       vmovsd    qword ptr [rsp+80],xmm0
       mov       r15d,1
       jmp       short M00_L179
M00_L178:
       lea       rcx,[rsp+70]
       lea       rdx,[rsp+80]
       call      qword ptr [7FF977C2C558]
       mov       r15d,eax
M00_L179:
       test      r15d,r15d
       jne       short M00_L180
       vmovsd    xmm0,qword ptr [7FF9778A3A28]
       jmp       short M00_L181
M00_L180:
       vmovsd    xmm0,qword ptr [rsp+80]
M00_L181:
       cmp       edi,[rbp+8]
       jae       near ptr M00_L265
       mov       edx,edi
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       ecx,7FFFFFFF
       vcvttsd2si eax,xmm1
       vucomisd  xmm0,qword ptr [7FF9778A3A30]
       cmovb     ecx,eax
       mov       [rbp+rdx*2+10],cx
       jmp       near ptr M00_L06
M00_L182:
       mov       rdx,[rbp+38]
       cmp       [rdx+8],edi
       ja        short M00_L183
       mov       rdx,[rbp+38]
       mov       edx,[rdx+8]
       mov       ecx,edi
       call      qword ptr [7FF977C2C540]
M00_L183:
       mov       rbp,[rbp+38]
       xor       ecx,ecx
       mov       [rsp+68],rcx
       mov       [rsp+58],r14
       mov       [rsp+60],r15
       cmp       qword ptr [rsp+58],0
       je        short M00_L184
       mov       r14,[rsp+58]
       mov       rcx,offset MT_AuroraScript.Runtime.ScriptDatum
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001400448
       cmp       r14,[rcx]
       je        short M00_L188
       mov       rcx,[rsp+58]
       mov       rdx,8001400450
       cmp       rcx,[rdx]
       je        short M00_L188
       movsx     rcx,word ptr [rsp+60]
       jmp       short M00_L187
M00_L184:
       mov       rcx,[rsp+60]
       test      rcx,rcx
       je        short M00_L185
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L186
       mov       ecx,2
       jmp       short M00_L187
M00_L185:
       xor       ecx,ecx
       jmp       short M00_L187
M00_L186:
       mov       ecx,1
M00_L187:
       test      ecx,ecx
       jne       short M00_L188
       xor       ecx,ecx
       mov       [rsp+68],rcx
       jmp       short M00_L194
M00_L188:
       cmp       qword ptr [rsp+58],0
       jne       short M00_L192
       cmp       qword ptr [rsp+60],2
       jbe       short M00_L192
       mov       rcx,[rsp+60]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L189
       vmovq     xmm0,rcx
       jmp       short M00_L191
M00_L189:
       cmp       rdx,2
       jbe       short M00_L190
       vmovsd    xmm0,qword ptr [7FF9778A3A28]
       jmp       short M00_L191
M00_L190:
       vmovq     xmm0,rdx
M00_L191:
       vmovsd    qword ptr [rsp+68],xmm0
       mov       r15d,1
       jmp       short M00_L193
M00_L192:
       lea       rcx,[rsp+58]
       lea       rdx,[rsp+68]
       call      qword ptr [7FF977C2C558]
       mov       r15d,eax
M00_L193:
       test      r15d,r15d
       jne       short M00_L194
       vmovsd    xmm0,qword ptr [7FF9778A3A28]
       jmp       short M00_L195
M00_L194:
       vmovsd    xmm0,qword ptr [rsp+68]
M00_L195:
       cmp       edi,[rbp+8]
       jae       near ptr M00_L265
       mov       edx,edi
       vxorps    xmm1,xmm1,xmm1
       vmaxsd    xmm1,xmm0,xmm1
       mov       ecx,0FFFFFFFF
       vcvttsd2si rax,xmm1
       vucomisd  xmm0,qword ptr [7FF9778A3A50]
       cmovb     ecx,eax
       mov       [rbp+rdx*4+10],ecx
       jmp       near ptr M00_L06
M00_L196:
       mov       rdx,[rbp+38]
       cmp       [rdx+8],edi
       ja        short M00_L197
       mov       rdx,[rbp+38]
       mov       edx,[rdx+8]
       mov       ecx,edi
       call      qword ptr [7FF977C2C540]
M00_L197:
       mov       rbp,[rbp+38]
       test      r14,r14
       je        short M00_L200
       mov       rcx,offset MT_AuroraScript.Runtime.ScriptDatum
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       r8,8001400448
       cmp       r14,[r8]
       jne       short M00_L198
       mov       r8d,1000
       jmp       short M00_L203
M00_L198:
       mov       r8,8001400450
       cmp       r14,[r8]
       jne       short M00_L199
       mov       r8d,2000
       jmp       short M00_L203
M00_L199:
       movsx     r8,r15w
       jmp       short M00_L203
M00_L200:
       test      r15,r15
       je        short M00_L201
       lea       r8,[r15-1]
       cmp       r8,1
       jbe       short M00_L202
       mov       r8d,2
       jmp       short M00_L203
M00_L201:
       xor       r8d,r8d
       jmp       short M00_L203
M00_L202:
       mov       r8d,1
M00_L203:
       cmp       r8d,2
       je        short M00_L205
       cmp       r8d,1000
       je        short M00_L204
       cmp       r8d,2000
       jne       near ptr M00_L210
       test      r15,r15
       jl        near ptr M00_L210
M00_L204:
       mov       rax,r15
       jmp       near ptr M00_L211
M00_L205:
       mov       rax,8007FFFFFFFFFFFF
       add       rax,r15
       cmp       rax,3
       jbe       short M00_L206
       vmovq     xmm0,r15
       jmp       short M00_L208
M00_L206:
       cmp       rax,2
       jbe       short M00_L207
       vmovsd    xmm0,qword ptr [7FF9778A3A28]
       jmp       short M00_L208
M00_L207:
       vmovq     xmm0,rax
M00_L208:
       vucomisd  xmm0,qword ptr [7FF9778A3A58]
       jb        short M00_L210
       vmovsd    xmm1,qword ptr [7FF9778A39C8]
       vucomisd  xmm1,xmm0
       jbe       short M00_L210
       vroundsd  xmm1,xmm1,xmm0,0B
       vucomisd  xmm1,xmm0
       jp        short M00_L210
       jne       short M00_L210
       vxorps    xmm1,xmm1,xmm1
       vucomisd  xmm0,xmm1
       jp        short M00_L209
       jne       short M00_L209
       vmovq     r8,xmm0
       not       r8
       shr       r8,3F
       test      r8d,r8d
       je        short M00_L210
M00_L209:
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       r8,7FFFFFFFFFFFFFFF
       vcvttsd2si rcx,xmm1
       vucomisd  xmm0,qword ptr [7FF9778A39C8]
       cmovb     r8,rcx
       mov       rax,r8
       jmp       short M00_L211
M00_L210:
       mov       [rsp+20],r14
       mov       [rsp+28],r15
       lea       r8,[rsp+20]
       lea       rcx,[rsp+48]
       mov       edx,13
       call      qword ptr [7FF977C2C588]
       xor       eax,eax
M00_L211:
       cmp       edi,[rbp+8]
       jae       near ptr M00_L265
       mov       edx,edi
       mov       [rbp+rdx*8+10],rax
       jmp       near ptr M00_L06
M00_L212:
       mov       rdx,[rbp+38]
       cmp       [rdx+8],edi
       ja        short M00_L213
       mov       rdx,[rbp+38]
       mov       edx,[rdx+8]
       mov       ecx,edi
       call      qword ptr [7FF977C2C540]
M00_L213:
       mov       rbp,[rbp+38]
       test      r14,r14
       je        short M00_L216
       mov       rcx,offset MT_AuroraScript.Runtime.ScriptDatum
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       r8,8001400448
       cmp       r14,[r8]
       jne       short M00_L214
       mov       r8d,1000
       jmp       short M00_L219
M00_L214:
       mov       r8,8001400450
       cmp       r14,[r8]
       jne       short M00_L215
       mov       r8d,2000
       jmp       short M00_L219
M00_L215:
       movsx     r8,r15w
       jmp       short M00_L219
M00_L216:
       test      r15,r15
       je        short M00_L217
       lea       r8,[r15-1]
       cmp       r8,1
       jbe       short M00_L218
       mov       r8d,2
       jmp       short M00_L219
M00_L217:
       xor       r8d,r8d
       jmp       short M00_L219
M00_L218:
       mov       r8d,1
M00_L219:
       cmp       r8d,2
       je        short M00_L222
       cmp       r8d,1000
       je        short M00_L220
       cmp       r8d,2000
       jne       near ptr M00_L227
       jmp       short M00_L221
M00_L220:
       test      r15,r15
       jl        near ptr M00_L227
M00_L221:
       jmp       near ptr M00_L228
M00_L222:
       mov       r8,8007FFFFFFFFFFFF
       add       r8,r15
       cmp       r8,3
       jbe       short M00_L223
       vmovq     xmm0,r15
       jmp       short M00_L225
M00_L223:
       cmp       r8,2
       jbe       short M00_L224
       vmovsd    xmm0,qword ptr [7FF9778A3A28]
       jmp       short M00_L225
M00_L224:
       vmovq     xmm0,r8
M00_L225:
       vxorps    xmm1,xmm1,xmm1
       vucomisd  xmm0,xmm1
       jb        short M00_L227
       vmovsd    xmm1,qword ptr [7FF9778A3A60]
       vucomisd  xmm1,xmm0
       jbe       short M00_L227
       vroundsd  xmm1,xmm1,xmm0,0B
       vucomisd  xmm1,xmm0
       jp        short M00_L227
       jne       short M00_L227
       vxorps    xmm1,xmm1,xmm1
       vucomisd  xmm0,xmm1
       jp        short M00_L226
       jne       short M00_L226
       vmovq     r8,xmm0
       not       r8
       shr       r8,3F
       test      r8d,r8d
       je        short M00_L227
M00_L226:
       vxorps    xmm1,xmm1,xmm1
       vmaxsd    xmm1,xmm0,xmm1
       vmovsd    xmm2,qword ptr [7FF9778A3A60]
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
       jmp       short M00_L228
M00_L227:
       mov       [rsp+20],r14
       mov       [rsp+28],r15
       lea       r8,[rsp+20]
       lea       rcx,[rsp+38]
       mov       edx,14
       call      qword ptr [7FF977C2C588]
       xor       r15d,r15d
M00_L228:
       cmp       edi,[rbp+8]
       jae       near ptr M00_L265
       mov       edx,edi
       mov       [rbp+rdx*8+10],r15
       jmp       near ptr M00_L06
M00_L229:
       mov       rcx,r13
       mov       rax,[r13]
       mov       rax,[rax+58]
       call      qword ptr [rax+8]
       mov       edx,eax
       mov       ecx,edi
       call      qword ptr [7FF977C2C540]
M00_L230:
       mov       [rsp+20],r14
       mov       [rsp+28],r15
       lea       r8,[rsp+20]
       mov       edx,edi
       mov       rcx,r13
       mov       rax,[r13]
       mov       rax,[rax+58]
       call      qword ptr [rax+18]
       jmp       near ptr M00_L06
M00_L231:
       mov       rcx,offset MT_AuroraScript.Runtime.ScriptDatum
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       jmp       near ptr M00_L12
M00_L232:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.UInt64Value+NativePrototypeHolder
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       jmp       near ptr M00_L56
M00_L233:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       jmp       near ptr M00_L47
M00_L234:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BooleanValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       jmp       near ptr M00_L43
M00_L235:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BooleanValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       jmp       near ptr M00_L46
M00_L236:
       cmp       rcx,2
       jbe       short M00_L237
       vmovsd    xmm1,qword ptr [7FF9778A3A28]
       jmp       near ptr M00_L27
M00_L237:
       vmovq     xmm1,rcx
       jmp       near ptr M00_L27
M00_L238:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       jmp       near ptr M00_L41
M00_L239:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       jmp       near ptr M00_L39
M00_L240:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       jmp       near ptr M00_L37
M00_L241:
       test      byte ptr [7FF977BDD4B0],1
       jne       short M00_L242
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
M00_L242:
       mov       rcx,80014014A0
       mov       r13,[rcx]
       jmp       near ptr M00_L42
M00_L243:
       test      byte ptr [7FF977BDD4B0],1
       jne       short M00_L244
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
M00_L244:
       mov       rcx,80014014A8
       mov       r13,[rcx]
       jmp       near ptr M00_L42
M00_L245:
       test      byte ptr [7FF977BDD4B0],1
       jne       short M00_L246
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
M00_L246:
       mov       rcx,80014014B0
       mov       r13,[rcx]
       jmp       near ptr M00_L42
M00_L247:
       test      byte ptr [7FF977BDD4B0],1
       jne       short M00_L248
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
M00_L248:
       mov       rcx,80014014B8
       mov       r13,[rcx]
       jmp       near ptr M00_L42
M00_L249:
       test      byte ptr [7FF977BDD4B0],1
       jne       short M00_L250
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
M00_L250:
       mov       rcx,80014014C0
       mov       r13,[rcx]
       jmp       near ptr M00_L42
M00_L251:
       test      byte ptr [7FF977BDD4B0],1
       jne       short M00_L252
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
M00_L252:
       mov       rcx,80014014C8
       mov       r13,[rcx]
       jmp       near ptr M00_L42
M00_L253:
       test      byte ptr [7FF977BDD4B0],1
       jne       short M00_L254
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
M00_L254:
       mov       rcx,80014014D0
       mov       r13,[rcx]
       jmp       near ptr M00_L42
M00_L255:
       vucomisd  xmm1,qword ptr [7FF9778A3A68]
       jp        short M00_L257
       jne       short M00_L257
       test      byte ptr [7FF977BDD4B0],1
       jne       short M00_L256
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
M00_L256:
       mov       rcx,80014014D8
       mov       r13,[rcx]
       jmp       near ptr M00_L42
M00_L257:
       vmovsd    qword ptr [rsp+30],xmm1
       vucomisd  xmm1,qword ptr [7FF9778A3A70]
       jp        short M00_L259
       jne       short M00_L259
       test      byte ptr [7FF977BDD4B0],1
       jne       short M00_L258
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
M00_L258:
       mov       rcx,8001401488
       mov       r13,[rcx]
       jmp       near ptr M00_L42
M00_L259:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      CORINFO_HELP_NEWSFAST
       mov       r13,rax
       mov       rcx,r13
       vmovsd    xmm1,qword ptr [rsp+30]
       call      qword ptr [7FF977C2C5B8]
       jmp       near ptr M00_L42
M00_L260:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.Int64Value+NativePrototypeHolder
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       jmp       near ptr M00_L58
M00_L261:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       jmp       near ptr M00_L19
M00_L262:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue+NativePrototypeHolder
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       jmp       near ptr M00_L22
M00_L263:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       jmp       near ptr M00_L49
M00_L264:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue+NativePrototypeHolder
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       jmp       near ptr M00_L52
M00_L265:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 7336
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
; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfInterface(Void*, System.Object)
       test      rdx,rdx
       je        short M02_L01
       mov       rax,[rdx]
       movzx     r8d,word ptr [rax+0E]
       test      r8,r8
       je        short M02_L04
       mov       r10,[rax+38]
       cmp       r8,4
       jge       short M02_L02
M02_L00:
       cmp       [r10],rcx
       jne       short M02_L03
M02_L01:
       mov       rax,rdx
       ret
M02_L02:
       cmp       [r10],rcx
       je        short M02_L01
       cmp       [r10+8],rcx
       je        short M02_L01
       cmp       [r10+10],rcx
       je        short M02_L01
       cmp       [r10+18],rcx
       je        short M02_L01
       add       r10,20
       add       r8,0FFFFFFFFFFFFFFFC
       cmp       r8,4
       jge       short M02_L02
       test      r8,r8
       je        short M02_L04
       jmp       short M02_L00
M02_L03:
       add       r10,8
       dec       r8
       test      r8,r8
       jg        short M02_L00
M02_L04:
       test      dword ptr [rax],500C0000
       jne       short M02_L05
       xor       edx,edx
       jmp       short M02_L01
M02_L05:
       jmp       qword ptr [7FF977BA5470]; System.Runtime.CompilerServices.CastHelpers.IsInstance_Helper(Void*, System.Object)
; Total bytes of code 111
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
; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       push      rsi
       push      rbx
       sub       rsp,28
       mov       rbx,rcx
       mov       rcx,[rbx+20]
       mov       rsi,[rcx-10]
       mov       rcx,rsi
       test      cl,1
       jne       short M04_L00
       mov       rcx,7FF977C88F94
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rax,rsi
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
M04_L00:
       mov       rcx,7FF977C88F90
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rcx,rbx
       add       rsp,28
       pop       rbx
       pop       rsi
       jmp       qword ptr [7FF9779FFD50]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBaseSlow(System.Runtime.CompilerServices.MethodTable*)
; Total bytes of code 80
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
; AuroraBenchmark.BoundaryOpsBenchmarks.ReadNumberProperty()
       push      r14
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,70
       vmovaps   [rsp+60],xmm6
       vmovaps   [rsp+50],xmm7
       vxorps    xmm4,xmm4,xmm4
       vmovdqu   ymmword ptr [rsp+20],ymm4
       vmovdqa   xmmword ptr [rsp+40],xmm4
       mov       rbx,rcx
M00_L00:
       vxorps    xmm6,xmm6,xmm6
       mov       esi,200
       jmp       near ptr M00_L06
M00_L01:
       mov       ecx,r14d
       lea       rax,[7FF9778DA628]
       mov       eax,[rax+rcx*4]
       lea       rdx,[M00_L00]
       add       rax,rdx
       jmp       rax
       lea       rcx,[rsp+20]
       call      qword ptr [7FF977C55E60]
       mov       rdi,rax
       jmp       near ptr M00_L11
       mov       rcx,[rsp+28]
       mov       rax,8007FFFFFFFFFFFF
       add       rax,rcx
       cmp       rax,3
       jbe       near ptr M00_L78
       jmp       near ptr M00_L77
       cmp       qword ptr [rsp+28],2
       je        near ptr M00_L75
       jmp       near ptr M00_L74
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001400410
       mov       rdi,[rcx]
       jmp       near ptr M00_L11
M00_L02:
       mov       rdx,0A033E51518
       mov       rax,[rcx]
       mov       rax,[rax+50]
       call      qword ptr [rax]
       mov       r8,rax
       test      r8,r8
       je        short M00_L03
       mov       rcx,offset MT_System.Globalization.NumberFormatInfo
       cmp       [r8],rcx
       jne       near ptr M00_L103
M00_L03:
       vmovsd    xmm0,qword ptr [7FF9778DA640]
       xor       edx,edx
       call      qword ptr [7FF977BD7120]; System.Number.FormatFloat[[System.Double, System.Private.CoreLib]](Double, System.String, System.Globalization.NumberFormatInfo)
       mov       rdx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       cmp       [rdi],rdx
       jne       near ptr M00_L104
       lea       rdx,[rsp+40]
       mov       rcx,rdi
       mov       r9,rax
       xor       r8d,r8d
       call      qword ptr [7FF977C55F68]; AuroraScript.Runtime.Types.ScriptObject.InternalGetPropertyDatum(AuroraScript.Runtime.ScriptContext, System.String)
M00_L04:
       mov       rdx,[rsp+48]
       mov       rcx,8007FFFFFFFFFFFF
       add       rcx,rdx
       cmp       rcx,3
       jbe       near ptr M00_L105
       vmovq     xmm0,rdx
M00_L05:
       vaddsd    xmm6,xmm6,xmm0
       dec       esi
       je        near ptr M00_L15
M00_L06:
       mov       rdi,[rbx+68]
       mov       rbp,[rbx+70]
       mov       rcx,rdi
       test      rcx,rcx
       je        short M00_L07
       mov       rdx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       cmp       [rcx],rdx
       jne       near ptr M00_L16
       xor       ecx,ecx
M00_L07:
       test      rcx,rcx
       jne       near ptr M00_L17
       mov       r14,rdi
       test      r14,r14
       je        short M00_L08
       mov       rdx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       cmp       [r14],rdx
       jne       near ptr M00_L18
       xor       r14d,r14d
M00_L08:
       test      r14,r14
       jne       near ptr M00_L19
       mov       [rsp+20],rdi
       mov       [rsp+28],rbp
       cmp       qword ptr [rsp+20],0
       je        near ptr M00_L69
       mov       rcx,[rsp+20]
       mov       rax,8001400450
       cmp       rcx,[rax]
       je        near ptr M00_L67
       mov       rcx,[rsp+20]
       mov       rax,8001400458
       cmp       rcx,[rax]
       je        near ptr M00_L68
       movsx     r14,word ptr [rsp+28]
M00_L09:
       cmp       r14d,4
       jbe       near ptr M00_L01
       cmp       r14d,1000
       je        near ptr M00_L95
       cmp       r14d,2000
       je        near ptr M00_L96
       mov       rdx,[rsp+20]
       mov       rdi,rdx
       test      rdi,rdi
       je        short M00_L10
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       cmp       [rdi],rcx
       jne       near ptr M00_L73
M00_L10:
       test      rdi,rdi
       je        near ptr M00_L97
M00_L11:
       mov       rcx,gs:[58]
       mov       rcx,[rcx+48]
       cmp       dword ptr [rcx+238],2
       jle       near ptr M00_L101
       mov       rcx,[rcx+240]
       mov       rax,[rcx+10]
       test      rax,rax
       je        near ptr M00_L101
M00_L12:
       mov       rcx,[rax+10]
       test      rcx,rcx
       jne       short M00_L14
       mov       rcx,8001400190
       mov       rcx,[rcx]
       test      rcx,rcx
       jne       short M00_L14
       mov       rcx,8001400170
       mov       rcx,[rcx]
       test      rcx,rcx
       jne       short M00_L14
       mov       rcx,80014001B8
       mov       rcx,[rcx]
       test      rcx,rcx
       je        near ptr M00_L102
       call      qword ptr [7FF97785D4E8]; System.Globalization.CultureInfo.GetCultureByName(System.String)
M00_L13:
       mov       rdx,rax
       mov       rcx,8001400170
       xor       r8d,r8d
       call      00007FF9D7471F00
       mov       rcx,8001400170
       mov       rcx,[rcx]
M00_L14:
       cmp       byte ptr [rcx+61],0
       jne       near ptr M00_L02
       mov       r8,[rcx+18]
       test      r8,r8
       je        near ptr M00_L02
       jmp       near ptr M00_L03
M00_L15:
       vmovaps   xmm0,xmm6
       vmovaps   xmm6,[rsp+60]
       vmovaps   xmm7,[rsp+50]
       add       rsp,70
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       ret
M00_L16:
       mov       rdx,rdi
       mov       rcx,offset MT_AuroraScript.Runtime.Types.IAuroraNativeIndexer
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfInterface(Void*, System.Object)
       mov       rcx,rax
       jmp       near ptr M00_L07
M00_L17:
       lea       rdx,[rsp+40]
       mov       r11,7FF9777A04E0
       mov       r8d,1
       call      qword ptr [r11]
       jmp       near ptr M00_L04
M00_L18:
       mov       rdx,rdi
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptPackedArray
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r14,rax
       jmp       near ptr M00_L08
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
       call      qword ptr [7FF977C5D458]
M00_L21:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jbe       near ptr M00_L107
       mov       edx,[rdx+14]
       test      edx,edx
       je        short M00_L22
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2sd xmm0,xmm0,edx
       vmovq     r14,xmm0
       jmp       short M00_L23
M00_L22:
       mov       r14,7FF8000000000001
M00_L23:
       xor       ebp,ebp
       jmp       near ptr M00_L66
M00_L24:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jg        short M00_L25
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,1
       call      qword ptr [7FF977C5D458]
M00_L25:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jbe       near ptr M00_L107
       movsx     rdx,byte ptr [rdx+11]
       test      edx,edx
       je        short M00_L26
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2sd xmm0,xmm0,edx
       vmovq     r14,xmm0
       jmp       short M00_L27
M00_L26:
       mov       r14,7FF8000000000001
M00_L27:
       xor       ebp,ebp
       jmp       near ptr M00_L66
M00_L28:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jg        short M00_L29
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,1
       call      qword ptr [7FF977C5D458]
M00_L29:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jbe       near ptr M00_L107
       vcvtss2sd xmm0,xmm0,dword ptr [rdx+14]
       vucomisd  xmm0,xmm0
       jp        short M00_L30
       je        short M00_L31
M00_L30:
       mov       r14,7FF8000000000004
       jmp       short M00_L33
M00_L31:
       vmovq     r14,xmm0
       cmp       r14,2
       jbe       short M00_L32
       jmp       short M00_L33
M00_L32:
       mov       rdx,7FF8000000000001
       add       r14,rdx
M00_L33:
       xor       ebp,ebp
       jmp       near ptr M00_L66
M00_L34:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jg        short M00_L35
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,1
       call      qword ptr [7FF977C5D458]
M00_L35:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jbe       near ptr M00_L107
       vmovsd    xmm0,qword ptr [rdx+18]
       vucomisd  xmm0,xmm0
       jp        short M00_L36
       je        short M00_L37
M00_L36:
       mov       r14,7FF8000000000004
       jmp       short M00_L39
M00_L37:
       vmovq     r14,xmm0
       cmp       r14,2
       jbe       short M00_L38
       jmp       short M00_L39
M00_L38:
       mov       rdx,7FF8000000000001
       add       r14,rdx
M00_L39:
       xor       ebp,ebp
       jmp       near ptr M00_L66
M00_L40:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jg        short M00_L41
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,1
       call      qword ptr [7FF977C5D458]
M00_L41:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jbe       near ptr M00_L107
       movzx     edx,byte ptr [rdx+11]
       test      edx,edx
       jne       short M00_L42
       mov       r14d,1
       jmp       short M00_L43
M00_L42:
       mov       r14d,2
M00_L43:
       xor       ebp,ebp
       jmp       near ptr M00_L66
M00_L44:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jg        short M00_L45
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,1
       call      qword ptr [7FF977C5D458]
M00_L45:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jbe       near ptr M00_L107
       movzx     edx,byte ptr [rdx+11]
       test      edx,edx
       je        short M00_L46
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2sd xmm0,xmm0,edx
       vmovq     r14,xmm0
       jmp       short M00_L47
M00_L46:
       mov       r14,7FF8000000000001
M00_L47:
       xor       ebp,ebp
       jmp       near ptr M00_L66
M00_L48:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jg        short M00_L49
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,1
       call      qword ptr [7FF977C5D458]
M00_L49:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jbe       near ptr M00_L107
       movsx     rdx,word ptr [rdx+12]
       test      edx,edx
       je        short M00_L50
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2sd xmm0,xmm0,edx
       vmovq     r14,xmm0
       jmp       short M00_L51
M00_L50:
       mov       r14,7FF8000000000001
M00_L51:
       xor       ebp,ebp
       jmp       near ptr M00_L66
M00_L52:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jg        short M00_L53
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,1
       call      qword ptr [7FF977C5D458]
M00_L53:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jbe       near ptr M00_L107
       movzx     edx,word ptr [rdx+12]
       test      edx,edx
       je        short M00_L54
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2sd xmm0,xmm0,edx
       vmovq     r14,xmm0
       jmp       short M00_L55
M00_L54:
       mov       r14,7FF8000000000001
M00_L55:
       xor       ebp,ebp
       jmp       near ptr M00_L66
M00_L56:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jg        short M00_L57
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,1
       call      qword ptr [7FF977C5D458]
M00_L57:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jbe       near ptr M00_L107
       mov       edx,[rdx+14]
       test      edx,edx
       je        short M00_L58
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2sd xmm0,xmm0,rdx
       vmovq     r14,xmm0
       jmp       short M00_L59
M00_L58:
       mov       r14,7FF8000000000001
M00_L59:
       xor       ebp,ebp
       jmp       near ptr M00_L66
M00_L60:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jg        short M00_L61
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,1
       call      qword ptr [7FF977C5D458]
M00_L61:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jbe       near ptr M00_L107
       mov       r14,[rdx+18]
       mov       rdx,8001400450
       mov       rbp,[rdx]
       jmp       short M00_L66
M00_L62:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jg        short M00_L63
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,1
       call      qword ptr [7FF977C5D458]
M00_L63:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jbe       near ptr M00_L107
       mov       r14,[rdx+18]
       mov       rdx,8001400458
       mov       rbp,[rdx]
       jmp       short M00_L66
M00_L64:
       mov       rcx,r14
       mov       rax,[r14]
       mov       rax,[rax+58]
       call      qword ptr [rax+8]
       mov       edx,eax
       mov       ecx,1
       call      qword ptr [7FF977C5D458]
M00_L65:
       lea       rdx,[rsp+30]
       mov       rcx,r14
       mov       r8d,1
       mov       rax,[r14]
       mov       rax,[rax+58]
       call      qword ptr [rax+10]
       mov       rbp,[rsp+30]
       mov       r14,[rsp+38]
M00_L66:
       mov       [rsp+40],rbp
       mov       [rsp+48],r14
       jmp       near ptr M00_L04
M00_L67:
       mov       r14d,1000
       jmp       near ptr M00_L09
M00_L68:
       mov       r14d,2000
       jmp       near ptr M00_L09
M00_L69:
       mov       rcx,[rsp+28]
       test      rcx,rcx
       je        short M00_L70
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L71
       mov       r14d,2
       jmp       short M00_L72
M00_L70:
       xor       r14d,r14d
       jmp       short M00_L72
M00_L71:
       mov       r14d,1
M00_L72:
       jmp       near ptr M00_L09
M00_L73:
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       jmp       near ptr M00_L10
M00_L74:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BooleanValue
       call      qword ptr [7FF977855740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001401518
       mov       rdi,[rcx]
       jmp       short M00_L76
M00_L75:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BooleanValue
       call      qword ptr [7FF977855740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001401510
       mov       rdi,[rcx]
M00_L76:
       jmp       near ptr M00_L11
M00_L77:
       vmovq     xmm7,rcx
       jmp       short M00_L80
M00_L78:
       cmp       rax,2
       jbe       short M00_L79
       vmovsd    xmm7,qword ptr [7FF9778DA648]
       jmp       short M00_L80
M00_L79:
       vmovq     xmm7,rax
M00_L80:
       vucomisd  xmm7,xmm7
       jp        short M00_L81
       je        short M00_L82
M00_L81:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977855740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001401500
       mov       rdi,[rcx]
       jmp       near ptr M00_L94
M00_L82:
       vxorps    xmm0,xmm0,xmm0
       vucomisd  xmm7,xmm0
       jp        short M00_L83
       jne       short M00_L83
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977855740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80014014B0
       mov       rdi,[rcx]
       jmp       near ptr M00_L94
M00_L83:
       vucomisd  xmm7,qword ptr [7FF9778DA650]
       jp        short M00_L84
       jne       short M00_L84
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977855740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80014014B8
       mov       rdi,[rcx]
       jmp       near ptr M00_L94
M00_L84:
       vucomisd  xmm7,qword ptr [7FF9778DA658]
       jp        short M00_L85
       jne       short M00_L85
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977855740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80014014C0
       mov       rdi,[rcx]
       jmp       near ptr M00_L94
M00_L85:
       vucomisd  xmm7,qword ptr [7FF9778DA660]
       jp        short M00_L86
       jne       short M00_L86
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977855740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80014014C8
       mov       rdi,[rcx]
       jmp       near ptr M00_L94
M00_L86:
       vucomisd  xmm7,qword ptr [7FF9778DA668]
       jp        short M00_L87
       jne       short M00_L87
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977855740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80014014D0
       mov       rdi,[rcx]
       jmp       near ptr M00_L94
M00_L87:
       vucomisd  xmm7,qword ptr [7FF9778DA670]
       jp        short M00_L88
       jne       short M00_L88
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977855740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80014014D8
       mov       rdi,[rcx]
       jmp       near ptr M00_L94
M00_L88:
       vucomisd  xmm7,qword ptr [7FF9778DA678]
       jp        short M00_L89
       jne       short M00_L89
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977855740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80014014E0
       mov       rdi,[rcx]
       jmp       near ptr M00_L94
M00_L89:
       vucomisd  xmm7,qword ptr [7FF9778DA680]
       jp        short M00_L90
       jne       short M00_L90
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977855740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80014014E8
       mov       rdi,[rcx]
       jmp       near ptr M00_L94
M00_L90:
       vucomisd  xmm7,qword ptr [7FF9778DA688]
       jp        short M00_L91
       jne       short M00_L91
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977855740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80014014F0
       mov       rdi,[rcx]
       jmp       near ptr M00_L94
M00_L91:
       vucomisd  xmm7,qword ptr [7FF9778DA690]
       jp        short M00_L92
       jne       short M00_L92
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977855740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80014014F8
       mov       rdi,[rcx]
       jmp       short M00_L94
M00_L92:
       vucomisd  xmm7,qword ptr [7FF9778DA698]
       jp        short M00_L93
       jne       short M00_L93
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977855740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80014014A8
       mov       rdi,[rcx]
       jmp       short M00_L94
M00_L93:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      CORINFO_HELP_NEWSFAST
       mov       rdi,rax
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue+NativePrototypeHolder
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rdx,8001401528
       mov       rdx,[rdx]
       mov       rcx,rdi
       mov       r8d,1
       call      qword ptr [7FF977C5D4A0]
       or        dword ptr [rdi+30],1
       vmovsd    qword ptr [rdi+38],xmm7
M00_L94:
       jmp       near ptr M00_L11
M00_L95:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.Int64Value
       call      CORINFO_HELP_NEWSFAST
       mov       rdi,rax
       mov       rbp,[rsp+28]
       mov       rcx,offset MT_AuroraScript.Runtime.Types.Int64Value+NativePrototypeHolder
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rdx,8001401530
       mov       rdx,[rdx]
       mov       rcx,rdi
       mov       r8d,1
       call      qword ptr [7FF977C5D4A0]
       or        dword ptr [rdi+30],1
       mov       [rdi+38],rbp
       jmp       near ptr M00_L11
M00_L96:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.UInt64Value
       call      CORINFO_HELP_NEWSFAST
       mov       rdi,rax
       mov       r14,[rsp+28]
       mov       rcx,offset MT_AuroraScript.Runtime.Types.UInt64Value+NativePrototypeHolder
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rdx,8001401538
       mov       rdx,[rdx]
       mov       rcx,rdi
       mov       r8d,1
       call      qword ptr [7FF977C5D4A0]
       or        dword ptr [rdi+30],1
       mov       [rdi+38],r14
       jmp       near ptr M00_L11
M00_L97:
       mov       rdx,[rsp+20]
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       short M00_L98
       xor       edi,edi
       jmp       near ptr M00_L11
M00_L98:
       cmp       dword ptr [rbp+8],0
       jne       short M00_L99
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001401498
       mov       rdi,[rcx]
       jmp       short M00_L100
M00_L99:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      CORINFO_HELP_NEWSFAST
       mov       rdi,rax
       mov       rcx,rdi
       mov       rdx,rbp
       call      qword ptr [7FF977C5CA68]
M00_L100:
       jmp       near ptr M00_L11
M00_L101:
       mov       ecx,2
       call      qword ptr [7FF977BDCB40]; System.Runtime.CompilerServices.StaticsHelpers.GetOptimizedGCThreadStaticBase(Int32)
       jmp       near ptr M00_L12
M00_L102:
       mov       rax,8001400180
       mov       rax,[rax]
       jmp       near ptr M00_L13
M00_L103:
       mov       rdx,rax
       call      qword ptr [7FF977856328]; System.Runtime.CompilerServices.CastHelpers.ChkCastClass(Void*, System.Object)
       int       3
M00_L104:
       lea       rdx,[rsp+40]
       mov       rcx,rdi
       mov       r9,rax
       xor       r8d,r8d
       mov       rax,[rdi]
       mov       rax,[rax+50]
       call      qword ptr [rax+10]
       jmp       near ptr M00_L04
M00_L105:
       cmp       rcx,2
       jbe       short M00_L106
       vmovsd    xmm0,qword ptr [7FF9778DA648]
       jmp       near ptr M00_L05
M00_L106:
       vmovq     xmm0,rcx
       jmp       near ptr M00_L05
M00_L107:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 3333
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
       jmp       qword ptr [7FF977855C38]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBaseSlow(System.Runtime.CompilerServices.MethodTable*)
; Total bytes of code 23
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
       mov       rax,21E9420C423F
       mov       [rbp],rax
       mov       r9,r8
       test      [rsp],esp
       sub       rsp,40
       lea       r8,[rsp+20]
       mov       [rbp+28],r8
       mov       dword ptr [rbp+30],20
       test      rdx,rdx
       jne       near ptr M02_L04
       xor       r8d,r8d
       xor       ecx,ecx
M02_L00:
       mov       [rbp+8],r8
       mov       [rbp+10],ecx
       lea       r8,[rbp+8]
       lea       rcx,[rbp+18]
       vmovaps   xmm1,xmm0
       call      qword ptr [7FF977BD7138]; System.Number.FormatFloat[[System.Double, System.Private.CoreLib],[System.Char, System.Private.CoreLib]](System.Collections.Generic.ValueListBuilder`1<Char> ByRef, Double, System.ReadOnlySpan`1<Char>, System.Globalization.NumberFormatInfo)
       mov       rbx,rax
       test      rbx,rbx
       jne       short M02_L01
       mov       ecx,[rbp+20]
       cmp       ecx,[rbp+30]
       ja        short M02_L05
       mov       rax,[rbp+28]
       mov       [rbp+8],rax
       mov       [rbp+10],ecx
       lea       rcx,[rbp+8]
       call      System.String.Ctor(System.ReadOnlySpan`1<Char>)
       mov       rbx,rax
M02_L01:
       mov       rdx,[rbp+18]
       test      rdx,rdx
       je        short M02_L02
       xor       ecx,ecx
       mov       [rbp+18],rcx
       mov       rcx,80014002B0
       mov       rcx,[rcx]
       xor       r8d,r8d
       call      qword ptr [7FF977A49840]; Precode of System.Buffers.SharedArrayPool`1[[System.Char, System.Private.CoreLib]].Return(Char[], Boolean)
M02_L02:
       mov       rax,rbx
       mov       r8,21E9420C423F
       cmp       [rbp],r8
       je        short M02_L03
       call      CORINFO_HELP_FAIL_FAST
M02_L03:
       nop
       lea       rsp,[rbp+38]
       pop       rbx
       pop       rbp
       ret
M02_L04:
       lea       r8,[rdx+0C]
       mov       ecx,[rdx+8]
       jmp       near ptr M02_L00
M02_L05:
       call      qword ptr [7FF977A27D50]
       int       3
; Total bytes of code 236
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
       js        near ptr M03_L10
M03_L00:
       mov       r8,[r14+8]
       cmp       r15d,[r8+8]
       jae       near ptr M03_L43
       mov       edx,r15d
       shl       rdx,5
       mov       r8,[r8+rdx+10]
       cmp       r8,rdi
       je        short M03_L02
       test      r8,r8
       je        short M03_L01
       test      rdi,rdi
       je        short M03_L01
       mov       edx,[r8+8]
       cmp       edx,[rdi+8]
       jne       short M03_L01
       lea       rcx,[r8+0C]
       mov       r8d,[r8+8]
       add       r8d,r8d
       lea       rdx,[rdi+0C]
       call      qword ptr [7FF97785C330]; System.SpanHelpers.SequenceEqual(Byte ByRef, Byte ByRef, UIntPtr)
       test      eax,eax
       jne       short M03_L02
M03_L01:
       dec       r15d
       jns       short M03_L00
       jmp       near ptr M03_L10
M03_L02:
       mov       r8d,r15d
M03_L03:
       test      r8d,r8d
       jl        near ptr M03_L11
       mov       rcx,[r14+8]
       cmp       r8d,[rcx+8]
       jae       near ptr M03_L43
       mov       r8d,r8d
       shl       r8,5
       movzx     r8d,word ptr [rcx+r8+18]
       cmp       qword ptr [rsi+18],0
       jne       near ptr M03_L12
M03_L04:
       xor       ecx,ecx
M03_L05:
       mov       rdx,[rsi+10]
       cmp       r8d,[rdx+8]
       jae       near ptr M03_L43
       mov       r8d,r8d
       shl       r8,4
       vmovdqu   xmm0,xmmword ptr [rdx+r8+10]
       vmovdqu   xmmword ptr [rsp+28],xmm0
       vmovdqu   xmm0,xmmword ptr [rsp+20]
       vmovdqu   xmmword ptr [rsp+60],xmm0
       mov       r8,[rsp+30]
       mov       [rsp+70],r8
       mov       [rsp+60],rcx
M03_L06:
       mov       rdx,[rsp+60]
       test      rdx,rdx
       jne       near ptr M03_L15
M03_L07:
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
       jne       near ptr M03_L16
M03_L08:
       mov       rdx,r14
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BondingFunction
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rax,rax
       jne       near ptr M03_L17
       mov       [rbx],rbp
       mov       [rbx+8],rdi
M03_L09:
       mov       rax,rbx
       add       rsp,78
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       pop       r15
       ret
M03_L10:
       mov       r8d,0FFFFFFFF
       jmp       near ptr M03_L03
M03_L11:
       mov       rcx,[rsi+8]
       test      rcx,rcx
       je        short M03_L14
       jmp       short M03_L13
M03_L12:
       mov       rcx,[rsi+18]
       cmp       [rcx+8],r8d
       jle       near ptr M03_L04
       mov       rcx,[rsi+18]
       cmp       r8d,[rcx+8]
       jae       near ptr M03_L43
       mov       edx,r8d
       mov       rcx,[rcx+rdx*8+10]
       jmp       near ptr M03_L05
M03_L13:
       lea       r8,[rsp+60]
       mov       rdx,rdi
       call      qword ptr [7FF977C55FF8]; AuroraScript.Runtime.Types.ScriptObject.TryResolveProperty(System.String, AuroraScript.Runtime.Property.PropertyDescriptor ByRef)
       test      eax,eax
       je        near ptr M03_L36
       jmp       near ptr M03_L06
M03_L14:
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rsp+60],xmm0
       vmovdqu   xmmword ptr [rsp+68],xmm0
       jmp       near ptr M03_L36
M03_L15:
       mov       rdx,[rdx+8]
       test      rdx,rdx
       je        near ptr M03_L07
       lea       rcx,[rsp+60]
       call      qword ptr [7FF977C56010]; AuroraScript.Runtime.Property.PropertyDescriptor.get_Getter()
       cmp       [rax],al
       mov       rcx,rax
       mov       rdx,rbx
       mov       r8,rbp
       call      qword ptr [7FF977C5CBA0]
       jmp       near ptr M03_L09
M03_L16:
       mov       r15,[rax+38]
       test      r15,r15
       je        near ptr M03_L08
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
       jmp       near ptr M03_L09
M03_L17:
       mov       rcx,rax
       mov       rdx,rsi
       call      qword ptr [7FF977C56040]
       mov       r15,rax
       test      r15,r15
       je        near ptr M03_L18
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NullValue
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rax,rax
       jne       near ptr M03_L18
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rax,rax
       jne       near ptr M03_L19
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.Int64Value
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rax,rax
       jne       near ptr M03_L24
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.UInt64Value
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rax,rax
       jne       near ptr M03_L25
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BooleanValue
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rax,rax
       jne       near ptr M03_L26
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rax,rax
       jne       near ptr M03_L27
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptArray
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdx,rax
       test      rdx,rdx
       jne       near ptr M03_L28
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptDate
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdx,rax
       test      rdx,rdx
       jne       near ptr M03_L29
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptRegex
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdx,rax
       test      rdx,rdx
       jne       near ptr M03_L30
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ClosureFunction
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdx,rax
       test      rdx,rdx
       jne       near ptr M03_L31
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptError
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdx,rax
       test      rdx,rdx
       jne       near ptr M03_L32
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Interop.ClrMethodBinding
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdx,rax
       test      rdx,rdx
       jne       near ptr M03_L33
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptType
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdx,rax
       test      rdx,rdx
       jne       near ptr M03_L34
       mov       esi,408
       jmp       near ptr M03_L35
M03_L18:
       xor       r15d,r15d
       xor       esi,esi
       jmp       near ptr M03_L35
M03_L19:
       vmovsd    xmm0,qword ptr [rax+38]
       vucomisd  xmm0,xmm0
       jp        short M03_L20
       je        short M03_L21
M03_L20:
       mov       rsi,7FF8000000000004
       jmp       short M03_L23
M03_L21:
       vmovq     rsi,xmm0
       cmp       rsi,2
       jbe       short M03_L22
       jmp       short M03_L23
M03_L22:
       mov       rcx,7FF8000000000001
       add       rsi,rcx
M03_L23:
       xor       r15d,r15d
       jmp       near ptr M03_L35
M03_L24:
       mov       rsi,[rax+38]
       mov       rcx,8001400450
       mov       r15,[rcx]
       jmp       near ptr M03_L35
M03_L25:
       mov       rsi,[rax+38]
       mov       rcx,8001400458
       mov       r15,[rcx]
       jmp       short M03_L35
M03_L26:
       movzx     ecx,byte ptr [rax+40]
       mov       esi,2
       mov       edx,1
       test      ecx,ecx
       cmove     rsi,rdx
       xor       r15d,r15d
       jmp       short M03_L35
M03_L27:
       mov       rcx,[rax+38]
       mov       rdx,0A033E50008
       test      rcx,rcx
       cmove     rcx,rdx
       mov       r15,rcx
       mov       esi,4
       jmp       short M03_L35
M03_L28:
       mov       r15,rdx
       mov       esi,18
       jmp       short M03_L35
M03_L29:
       mov       r15,rdx
       mov       esi,28
       jmp       short M03_L35
M03_L30:
       mov       r15,rdx
       mov       esi,48
       jmp       short M03_L35
M03_L31:
       mov       r15,rdx
       mov       esi,88
       jmp       short M03_L35
M03_L32:
       mov       r15,rdx
       mov       esi,808
       jmp       short M03_L35
M03_L33:
       mov       r15,rdx
       mov       esi,208
       jmp       short M03_L35
M03_L34:
       mov       r15,rdx
       mov       esi,108
M03_L35:
       mov       rcx,rbx
       mov       rdx,r15
       call      CORINFO_HELP_CHECKED_ASSIGN_REF
       mov       [rbx+8],rsi
       jmp       near ptr M03_L09
M03_L36:
       lea       rdx,[rsp+58]
       mov       rcx,rsi
       call      qword ptr [7FF977C56058]
       test      eax,eax
       je        near ptr M03_L42
       lea       rdx,[rsp+38]
       mov       rcx,[rsp+58]
       mov       r8,rbp
       mov       r9,rdi
       cmp       [rcx],ecx
       call      qword ptr [7FF977C6B8D0]
       cmp       qword ptr [rsp+38],0
       je        short M03_L37
       mov       rcx,[rsp+38]
       mov       rax,8001400450
       cmp       rcx,[rax]
       je        short M03_L41
       mov       rcx,[rsp+38]
       mov       rax,8001400458
       cmp       rcx,[rax]
       je        short M03_L41
       movsx     rcx,word ptr [rsp+40]
       jmp       short M03_L40
M03_L37:
       mov       rcx,[rsp+40]
       test      rcx,rcx
       je        short M03_L39
       dec       rcx
       mov       eax,1
       mov       edx,2
       cmp       rcx,1
       cmova     eax,edx
       mov       ecx,eax
M03_L38:
       jmp       short M03_L40
M03_L39:
       xor       ecx,ecx
       jmp       short M03_L38
M03_L40:
       test      ecx,ecx
       jne       short M03_L41
       lea       rcx,[rsp+38]
       call      qword ptr [7FF977C55E78]; AuroraScript.Runtime.ScriptDatum.get_Object()
       test      rax,rax
       je        short M03_L42
M03_L41:
       vmovdqu   xmm0,xmmword ptr [rsp+38]
       vmovdqu   xmmword ptr [rbx],xmm0
       jmp       near ptr M03_L09
M03_L42:
       mov       rax,8001400428
       mov       rax,[rax]
       vmovdqu   xmm0,xmmword ptr [rax+8]
       vmovdqu   xmmword ptr [rbx],xmm0
       jmp       near ptr M03_L09
M03_L43:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 1480
```
```assembly
; System.Globalization.CultureInfo.GetCultureByName(System.String)
       push      rbp
       push      rsi
       push      rbx
       sub       rsp,30
       lea       rbp,[rsp+40]
       mov       rbx,rcx
       call      qword ptr [7FF96E843988]
       mov       rsi,rax
       mov       rcx,rsi
       mov       rdx,rbx
       mov       r8d,1
       call      qword ptr [7FF96E8508F8]
       mov       byte ptr [rsi+60],1
       mov       [rbp-18],rsi
M04_L00:
       mov       rax,[rbp-18]
       add       rsp,30
       pop       rbx
       pop       rsi
       pop       rbp
       ret
       sub       rsp,28
       call      qword ptr [7FF96E8509A0]
       mov       [rbp-18],rax
       lea       rax,[M04_L00]
       add       rsp,28
       ret
; Total bytes of code 88
```
```assembly
; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfInterface(Void*, System.Object)
       test      rdx,rdx
       je        short M05_L01
       mov       rax,[rdx]
       movzx     r8d,word ptr [rax+0E]
       test      r8,r8
       jne       short M05_L02
M05_L00:
       test      dword ptr [rax],500C0000
       jne       short M05_L05
       xor       edx,edx
M05_L01:
       mov       rax,rdx
       ret
M05_L02:
       mov       r10,[rax+38]
       cmp       r8,4
       jl        short M05_L04
M05_L03:
       cmp       [r10],rcx
       je        short M05_L01
       cmp       [r10+8],rcx
       je        short M05_L01
       cmp       [r10+10],rcx
       je        short M05_L01
       cmp       [r10+18],rcx
       je        short M05_L01
       add       r10,20
       add       r8,0FFFFFFFFFFFFFFFC
       cmp       r8,4
       jge       short M05_L03
       test      r8,r8
       je        short M05_L00
M05_L04:
       cmp       [r10],rcx
       je        short M05_L01
       add       r10,8
       dec       r8
       test      r8,r8
       jg        short M05_L04
       jmp       short M05_L00
M05_L05:
       jmp       qword ptr [7FF977BD5488]; System.Runtime.CompilerServices.CastHelpers.IsInstance_Helper(Void*, System.Object)
; Total bytes of code 109
```
```assembly
; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rdx,rdx
       je        short M06_L02
       cmp       [rdx],rcx
       je        short M06_L02
       mov       rax,[rdx]
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M06_L02
M06_L00:
       test      rax,rax
       je        short M06_L01
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M06_L02
       test      rax,rax
       jne       short M06_L03
M06_L01:
       xor       edx,edx
M06_L02:
       mov       rax,rdx
       ret
M06_L03:
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M06_L02
       test      rax,rax
       je        short M06_L01
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M06_L02
       test      rax,rax
       je        short M06_L01
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M06_L02
       jmp       short M06_L00
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
       jne       short M07_L00
       mov       rcx,7FF977CD6B4C
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rax,rsi
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
M07_L00:
       mov       rcx,7FF977CD6B48
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rcx,rbx
       add       rsp,28
       pop       rbx
       pop       rsi
       jmp       qword ptr [7FF977A2FD68]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBaseSlow(System.Runtime.CompilerServices.MethodTable*)
; Total bytes of code 80
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
       jne       short M08_L01
       cmp       [rax],ecx
       jle       short M08_L03
       mov       rax,[rax+8]
       cmp       [rax],al
       add       edx,0FFFFFFFE
       movsxd    rcx,edx
       mov       rax,[rax+rcx*8+10]
       test      rax,rax
       je        short M08_L03
M08_L00:
       add       rsp,20
       pop       rbx
       ret
M08_L01:
       mov       ecx,ebx
       sar       ecx,18
       cmp       ecx,2
       jne       short M08_L02
       movsxd    rcx,edx
       add       rax,rcx
       jmp       short M08_L00
M08_L02:
       cmp       [rax+4],edx
       jle       short M08_L03
       mov       rcx,[rax+10]
       movsxd    rax,edx
       mov       rcx,[rcx+rax*8]
       test      rcx,rcx
       je        short M08_L03
       mov       rax,[rcx]
       test      rax,rax
       je        short M08_L03
       jmp       short M08_L00
M08_L03:
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
       je        short M09_L00
       cmp       [rdx],rcx
       jne       short M09_L01
M09_L00:
       mov       rax,rdx
       ret
M09_L01:
       jmp       qword ptr [7FF977A24D20]; System.Runtime.CompilerServices.CastHelpers.ChkCastClassSpecial(Void*, System.Object)
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
M11_L00:
       vmovdqa   xmmword ptr [rbp+rax+120],xmm4
       vmovdqa   xmmword ptr [rbp+rax+130],xmm4
       vmovdqa   xmmword ptr [rbp+rax+140],xmm4
       add       rax,30
       jne       short M11_L00
       mov       [rbp+120],rax
       mov       rax,21E9420C423F
       mov       [rbp+8],rax
       mov       rdi,rcx
       vmovaps   xmm6,xmm1
       mov       rbx,r8
       mov       rsi,r9
       vmovq     rax,xmm6
       mov       rcx,7FF0000000000000
       andn      rax,rax,rcx
       je        near ptr M11_L20
       mov       r14,[rbx]
       mov       r15d,[rbx+8]
       xor       ecx,ecx
       test      r15d,r15d
       jg        near ptr M11_L27
M11_L01:
       mov       dword ptr [rbp+128],0FFFFFFFF
       test      r15d,r15d
       jne       near ptr M11_L37
M11_L02:
       mov       ecx,47
M11_L03:
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
       jbe       near ptr M11_L50
       mov       r9,[rbp+118]
       mov       byte ptr [r9],0
       vmovq     r9,xmm6
       shr       r9,3F
       mov       [rbp+110],r9b
       lea       r9,[rbp+100]
       lea       rdx,[rbp+128]
       mov       r8,rsi
       call      qword ptr [7FF977BD7150]; System.Number.GetFloatingPointMaxDigitsAndPrecision(Char, Int32 ByRef, System.Globalization.NumberFormatInfo, Boolean ByRef)
       mov       r15d,eax
       vxorps    xmm0,xmm0,xmm0
       vucomisd  xmm6,xmm0
       jp        short M11_L04
       je        near ptr M11_L15
M11_L04:
       cmp       byte ptr [rbp+100],0
       je        near ptr M11_L48
       mov       r13d,[rbp+128]
       vmovq     rax,xmm6
       test      rax,rax
       jl        near ptr M11_L38
       vmovaps   xmm0,xmm6
M11_L05:
       cmp       r13d,0FFFFFFFF
       jne       near ptr M11_L47
       vmovq     rax,xmm0
       mov       rdx,0FFFFFFFFFFFFF
       and       rdx,rax
       shr       rax,34
       and       eax,7FF
       je        near ptr M11_L39
       mov       rcx,10000000000000
       or        rdx,rcx
       add       eax,0FFFFFBCD
M11_L06:
       lea       rcx,[rdx*2+1]
       lea       r8d,[rax-1]
       xor       r10d,r10d
       lzcnt     r10,rcx
       shlx      rcx,rcx,r10
       sub       r8d,r10d
       mov       r10,10000000000000
       cmp       rdx,r10
       je        near ptr M11_L40
       lea       r10,[rdx*2-1]
       lea       r9d,[rax-1]
M11_L07:
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
       vmulsd    xmm0,xmm0,qword ptr [7FF9778D58B0]
       vroundsd  xmm0,xmm0,xmm0,0A
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       r9d,7FFFFFFF
       vcvttsd2si r11d,xmm1
       vucomisd  xmm0,qword ptr [7FF9778D58B8]
       cmovb     r9d,r11d
       add       r9d,15B
       mov       r11d,r9d
       sar       r11d,1F
       and       r11d,7
       add       r9d,r11d
       sar       r9d,3
       inc       r9d
       cmp       r9d,57
       jae       near ptr M11_L50
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
       jae       near ptr M11_L50
       mov       r10d,eax
       mov       r9,7FF96DBD36D0
       mov       r10d,[r9+r10*4]
       cmp       r11d,r10d
       jb        near ptr M11_L41
M11_L08:
       lea       r9d,[rax+1]
       xor       eax,eax
       mov       [rbp+0D8],eax
       test      r9d,r9d
       jle       near ptr M11_L46
M11_L09:
       mov       eax,r11d
       xor       edx,edx
       div       r10d
       mov       edx,eax
       imul      edx,r10d
       sub       r11d,edx
       mov       edx,[rbp+0D8]
       cmp       edx,r12d
       jae       near ptr M11_L50
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
       jae       near ptr M11_L19
       mov       eax,[rbp+0D8]
       mov       r11,[rbp+70]
       sub       r11,[rbp+0B8]
       mov       [rbp+10],r13
       mov       r10d,r10d
       shlx      r8,r10,r8
       mov       r10,r11
       sub       r10,1
       inc       r11
M11_L10:
       cmp       rdx,r10
       jb        near ptr M11_L43
M11_L11:
       cmp       rdx,r11
       jae       short M11_L12
       mov       rax,rcx
       sub       rax,rdx
       cmp       rax,r8
       jae       near ptr M11_L44
M11_L12:
       cmp       rdx,2
       jb        near ptr M11_L45
       sub       rcx,4
       cmp       rcx,rdx
       setae     al
       movzx     eax,al
M11_L13:
       mov       edx,r9d
       sub       edx,[rbp+0CC]
       mov       [rbp+0D0],edx
       movzx     r13d,al
M11_L14:
       test      r13d,r13d
       je        near ptr M11_L48
       mov       r8d,[rbp+0D8]
       add       r8d,[rbp+0D0]
       mov       [rbp+10C],r8d
       mov       r8d,[rbp+120]
       cmp       [rbp+0D8],r8d
       jae       near ptr M11_L50
       mov       r8,[rbp+118]
       mov       r9d,[rbp+0D8]
       mov       byte ptr [r8+r9],0
       mov       r8d,[rbp+0D8]
       mov       [rbp+108],r8d
M11_L15:
       test      r14d,r14d
       je        near ptr M11_L49
       cmp       dword ptr [rbp+128],0FFFFFFFF
       jne       short M11_L16
       mov       r15d,[rbp+108]
       mov       edx,11
       cmp       r15d,11
       cmovl     r15d,edx
M11_L16:
       mov       [rsp+20],rsi
       lea       rdx,[rbp+108]
       mov       rcx,rdi
       mov       r8d,r14d
       mov       r9d,r15d
       call      qword ptr [7FF977C55F38]; System.Number.NumberToString[[System.Char, System.Private.CoreLib]](System.Collections.Generic.ValueListBuilder`1<Char> ByRef, NumberBuffer ByRef, Char, Int32, System.Globalization.NumberFormatInfo)
M11_L17:
       xor       eax,eax
       mov       r8,21E9420C423F
       cmp       [rbp+8],r8
       je        short M11_L18
       call      CORINFO_HELP_FAIL_FAST
M11_L18:
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
M11_L19:
       mov       edx,0CCCCCCCD
       mov       r10d,r10d
       imul      r10,rdx
       shr       r10,23
       test      r9d,r9d
       mov       [rbp+68],rax
       jg        near ptr M11_L09
       jmp       near ptr M11_L46
M11_L20:
       vucomisd  xmm6,xmm6
       jp        short M11_L21
       je        short M11_L23
M11_L21:
       mov       rax,[rsi+58]
       mov       r8,21E9420C423F
       cmp       [rbp+8],r8
       je        short M11_L22
       call      CORINFO_HELP_FAIL_FAST
M11_L22:
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
M11_L23:
       vmovq     rax,xmm6
       test      rax,rax
       jl        short M11_L25
       mov       rax,[rsi+60]
       mov       r8,21E9420C423F
       cmp       [rbp+8],r8
       je        short M11_L24
       call      CORINFO_HELP_FAIL_FAST
M11_L24:
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
M11_L25:
       mov       rax,[rsi+68]
       mov       r8,21E9420C423F
       cmp       [rbp+8],r8
       je        short M11_L26
       call      CORINFO_HELP_FAIL_FAST
M11_L26:
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
M11_L27:
       movzx     ecx,word ptr [r14]
       mov       eax,ecx
       or        eax,20
       add       eax,0FFFFFF9F
       cmp       eax,19
       ja        near ptr M11_L01
       cmp       r15d,1
       jne       short M11_L29
       mov       dword ptr [rbp+128],0FFFFFFFF
M11_L28:
       jmp       near ptr M11_L03
M11_L29:
       cmp       r15d,2
       jne       short M11_L30
       movzx     eax,word ptr [r14+2]
       add       eax,0FFFFFFD0
       cmp       eax,0A
       jae       short M11_L31
       mov       [rbp+128],eax
       jmp       short M11_L28
M11_L30:
       cmp       r15d,3
       jne       short M11_L31
       movzx     eax,word ptr [r14+2]
       add       eax,0FFFFFFD0
       movzx     edx,word ptr [r14+4]
       add       edx,0FFFFFFD0
       cmp       eax,0A
       jae       short M11_L31
       cmp       edx,0A
       jae       short M11_L31
       lea       eax,[rax+rax*4]
       lea       eax,[rdx+rax*2]
       mov       [rbp+128],eax
       jmp       short M11_L28
M11_L31:
       xor       r13d,r13d
       mov       eax,1
       jmp       short M11_L33
M11_L32:
       cmp       r13d,5F5E100
       jge       short M11_L34
       lea       edx,[rax+1]
       mov       eax,eax
       movzx     eax,word ptr [r14+rax*2]
       lea       r8d,[r13+r13*4]
       lea       r13d,[rax+r8*2-30]
       mov       eax,edx
M11_L33:
       cmp       eax,r15d
       jae       short M11_L36
       mov       edx,eax
       movzx     edx,word ptr [r14+rdx*2]
       add       edx,0FFFFFFD0
       cmp       edx,9
       jbe       short M11_L32
       jmp       short M11_L35
M11_L34:
       call      qword ptr [7FF977C5C270]
       int       3
M11_L35:
       mov       r9d,eax
       cmp       word ptr [r14+r9*2],0
       jne       near ptr M11_L01
M11_L36:
       mov       [rbp+128],r13d
       jmp       near ptr M11_L28
M11_L37:
       test      ecx,ecx
       je        near ptr M11_L02
       xor       ecx,ecx
       jmp       near ptr M11_L03
M11_L38:
       vxorps    xmm0,xmm6,[7FF9778D58C0]
       jmp       near ptr M11_L05
M11_L39:
       mov       eax,0FFFFFBCE
       jmp       near ptr M11_L06
M11_L40:
       lea       r9d,[rax-2]
       mov       r10,3FFFFFFFFFFFFF
       jmp       near ptr M11_L07
M11_L41:
       dec       eax
       cmp       eax,0A
       jae       near ptr M11_L50
       mov       r10d,eax
       mov       r9,7FF96DBD36D0
       mov       r10d,[r9+r10*4]
       jmp       near ptr M11_L08
M11_L42:
       lea       r13d,[rax-1]
       cmp       r13d,r12d
       jae       near ptr M11_L50
       lea       r13d,[rax-1]
       add       r13,[rbp+10]
       dec       byte ptr [r13]
       add       rdx,r8
       jmp       near ptr M11_L10
M11_L43:
       mov       r13,rcx
       sub       r13,rdx
       cmp       r13,r8
       jb        near ptr M11_L11
       lea       r13,[rdx+r8]
       cmp       r13,r10
       jb        short M11_L42
       mov       r13,r10
       sub       r13,rdx
       mov       [rbp],r13
       lea       r13,[rdx+r8]
       sub       r13,r10
       cmp       [rbp],r13
       jae       short M11_L42
       jmp       near ptr M11_L11
M11_L44:
       lea       rax,[rdx+r8]
       cmp       rax,r11
       jb        short M11_L45
       mov       rax,r11
       sub       rax,rdx
       add       r8,rdx
       sub       r8,r11
       cmp       rax,r8
       jbe       near ptr M11_L12
M11_L45:
       xor       eax,eax
       jmp       near ptr M11_L13
M11_L46:
       mov       rdx,[rbp+68]
       lea       rdx,[rdx+rdx*4]
       add       rdx,rdx
       mov       rax,[rbp+80]
       lea       rax,[rax+rax*4]
       add       rax,rax
       mov       [rbp+80],rax
       lea       r10,[rcx+rcx*4]
       add       r10,r10
       mov       rcx,r10
       shrx      r11,rdx,r8
       mov       eax,[rbp+0D8]
       cmp       eax,r12d
       jae       near ptr M11_L50
       add       r11d,30
       mov       [rax+r13],r11b
       mov       eax,[rbp+0D8]
       inc       eax
       mov       [rbp+0D8],eax
       dec       r9d
       mov       [rbp+0B4],r9d
       mov       rax,[rbp+30]
       lea       r11,[rax-1]
       and       rdx,r11
       mov       [rbp+68],rdx
       cmp       rdx,rcx
       mov       [rbp+48],r10
       jae       near ptr M11_L51
       mov       r11d,[rbp+0D8]
       mov       [rbp+64],r11d
       lea       rcx,[rbp+70]
       lea       rdx,[rbp+50]
       lea       r8,[rbp+0B8]
       call      qword ptr [7FF977C5CAC8]
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
       call      qword ptr [7FF977C55F20]; System.Number+Grisu3.TryRoundWeedShortest(System.Span`1<Byte>, Int32, UInt64, UInt64, UInt64, UInt64, UInt64)
       mov       r9d,[rbp+0B4]
       jmp       near ptr M11_L13
M11_L47:
       lea       rdx,[rbp+40]
       call      qword ptr [7FF977BD7180]; System.Number.ExtractFractionAndBiasedExponent[[System.Double, System.Private.CoreLib]](Double, Int32 ByRef)
       mov       ecx,[rbp+40]
       mov       [rbp+0F0],rax
       mov       [rbp+0F8],ecx
       lea       rcx,[rbp+0F0]
       lea       rdx,[rbp+0E0]
       call      qword ptr [7FF977BD7198]; System.Number+DiyFp.Normalize()
       vmovdqu   xmm0,xmmword ptr [rbp+118]
       vmovdqu   xmmword ptr [rbp+20],xmm0
       lea       r8,[rbp+0D0]
       mov       [rsp+20],r8
       lea       r8,[rbp+20]
       lea       rcx,[rbp+0E0]
       lea       r9,[rbp+0D8]
       mov       edx,r13d
       call      qword ptr [7FF977BD71B0]; System.Number+Grisu3.TryRunCounted(DiyFp ByRef, Int32, System.Span`1<Byte>, Int32 ByRef, Int32 ByRef)
       mov       r13d,eax
       jmp       near ptr M11_L14
M11_L48:
       movzx     r8d,byte ptr [rbp+100]
       lea       r9,[rbp+108]
       vmovaps   xmm0,xmm6
       mov       edx,[rbp+128]
       call      qword ptr [7FF977BD7258]; System.Number.Dragon4[[System.Double, System.Private.CoreLib]](Double, Int32, Boolean, NumberBuffer ByRef)
       jmp       near ptr M11_L15
M11_L49:
       lea       rdx,[rbp+108]
       mov       rcx,rdi
       mov       r8,rbx
       mov       r9,rsi
       call      qword ptr [7FF977BD7330]; System.Number.NumberToStringFormat[[System.Char, System.Private.CoreLib]](System.Collections.Generic.ValueListBuilder`1<Char> ByRef, NumberBuffer ByRef, System.ReadOnlySpan`1<Char>, System.Globalization.NumberFormatInfo)
       jmp       near ptr M11_L17
M11_L50:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
M11_L51:
       mov       r9d,[rbp+0B4]
       jmp       near ptr M11_L46
; Total bytes of code 2665
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
       je        short M12_L00
       mov       edx,esi
       mov       rcx,offset MT_System.String
       call      00007FF9D74C4E80
       mov       rdi,rax
       cmp       [rdi],dil
       lea       rcx,[rdi+0C]
       mov       r8d,esi
       add       r8,r8
       mov       rdx,rbx
       call      qword ptr [7FF977855818]; System.SpanHelpers.Memmove(Byte ByRef, Byte ByRef, UIntPtr)
       mov       rax,rdi
       add       rsp,20
       pop       rbx
       pop       rsi
       pop       rdi
       ret
M12_L00:
       mov       rax,0A033E50008
       add       rsp,20
       pop       rbx
       pop       rsi
       pop       rdi
       ret
; Total bytes of code 88
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
       mov       r8,8001400068
       mov       r15,[r8]
M14_L01:
       mov       r8,[rbp+8]
       cmp       r14d,[r8+8]
       jae       near ptr M14_L11
       mov       edx,r14d
       shl       rdx,5
       mov       r8,[r8+rdx+10]
       cmp       r8,rdi
       je        short M14_L03
       test      r8,r8
       je        short M14_L02
       test      rdi,rdi
       je        short M14_L02
       mov       edx,[r8+8]
       cmp       edx,[rdi+8]
       jne       short M14_L02
       lea       rcx,[r8+0C]
       mov       r8d,[r8+8]
       add       r8d,r8d
       lea       rdx,[rdi+0C]
       call      qword ptr [7FF97785C330]; System.SpanHelpers.SequenceEqual(Byte ByRef, Byte ByRef, UIntPtr)
       test      eax,eax
       jne       short M14_L03
M14_L02:
       dec       r14d
       jns       short M14_L01
       jmp       short M14_L07
M14_L03:
       mov       eax,r14d
M14_L04:
       test      eax,eax
       jl        short M14_L08
       mov       rcx,[rbp+8]
       cmp       eax,[rcx+8]
       jae       near ptr M14_L11
       mov       eax,eax
       shl       rax,5
       movzx     eax,word ptr [rcx+rax+18]
       cmp       qword ptr [rsi+18],0
       jne       short M14_L09
M14_L05:
       xor       ecx,ecx
M14_L06:
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
M14_L07:
       mov       eax,0FFFFFFFF
       jmp       short M14_L04
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
       jle       short M14_L05
       mov       rcx,[rsi+18]
       cmp       eax,[rcx+8]
       jae       short M14_L11
       mov       edx,eax
       mov       rcx,[rcx+rdx*8+10]
       jmp       near ptr M14_L06
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
; Total bytes of code 367
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
; AuroraScript.Runtime.ScriptDatum.get_Object()
       push      rsi
       push      rbx
       sub       rsp,28
       mov       rbx,rcx
       mov       rdx,[rbx]
       mov       rax,rdx
       test      rax,rax
       je        short M16_L00
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       cmp       [rax],rcx
       jne       short M16_L01
M16_L00:
       test      rax,rax
       je        short M16_L02
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
M16_L01:
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       jmp       short M16_L00
M16_L02:
       mov       rdx,[rbx]
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbx,rax
       test      rbx,rbx
       jne       short M16_L03
       xor       eax,eax
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
M16_L03:
       cmp       dword ptr [rbx+8],0
       jne       short M16_L04
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001401498
       mov       rax,[rcx]
       jmp       short M16_L05
M16_L04:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      CORINFO_HELP_NEWSFAST
       mov       rsi,rax
       mov       rcx,rsi
       mov       rdx,rbx
       call      qword ptr [7FF977C5CA68]
       mov       rax,rsi
M16_L05:
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
; Total bytes of code 165
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
       je        short M19_L00
       mov       rcx,rsi
       cmp       [r8],rcx
       je        short M19_L00
       mov       rdx,rax
       call      qword ptr [7FF96E839090]; Precode of System.Runtime.CompilerServices.CastHelpers.ChkCastAny(Void*, System.Object)
       mov       r8,rax
M19_L00:
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
       jne       short M20_L01
M20_L00:
       mov       rax,rdx
       ret
M20_L01:
       test      rax,rax
       je        short M20_L04
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M20_L00
       jmp       short M20_L03
M20_L02:
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M20_L00
       jmp       short M20_L01
M20_L03:
       test      rax,rax
       je        short M20_L04
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M20_L00
       test      rax,rax
       je        short M20_L04
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M20_L00
       test      rax,rax
       jne       short M20_L02
M20_L04:
       lea       rax,[System.Reflection.CustomAttributeExtensions.GetCustomAttribute[[System.__Canon, System.Private.CoreLib]](System.Reflection.Assembly)]
       jmp       qword ptr [rax]
; Total bytes of code 86
```
```assembly
; System.Number.GetFloatingPointMaxDigitsAndPrecision(Char, Int32 ByRef, System.Globalization.NumberFormatInfo, Boolean ByRef)
       push      rbx
       sub       rsp,20
M21_L00:
       movzx     eax,cx
       test      eax,eax
       je        short M21_L03
       mov       ecx,[rdx]
       mov       r10d,ecx
       or        eax,20
       lea       r11d,[rax-63]
       cmp       r11d,4
       jne       short M21_L04
       test      ecx,ecx
       je        near ptr M21_L10
M21_L01:
       mov       byte ptr [r9],1
M21_L02:
       mov       eax,r10d
       add       rsp,20
       pop       rbx
       ret
M21_L03:
       mov       byte ptr [r9],1
       mov       eax,[rdx]
       add       rsp,20
       pop       rbx
       ret
M21_L04:
       cmp       r11d,4
       ja        short M21_L05
       mov       eax,r11d
       lea       r11,[7FF9778D5B20]
       mov       r11d,[r11+rax*4]
       lea       rbx,[M21_L00]
       add       r11,rbx
       jmp       r11
M21_L05:
       add       eax,0FFFFFF92
       cmp       eax,4
       ja        short M21_L06
       lea       r11,[7FF9778D5B34]
       mov       r11d,[r11+rax*4]
       lea       rbx,[M21_L00]
       add       r11,rbx
       jmp       r11
M21_L06:
       call      qword ptr [7FF977C5C270]
       int       3
       cmp       ecx,0FFFFFFFF
       jne       short M21_L07
       mov       ecx,[r8+10C]
       mov       [rdx],ecx
M21_L07:
       mov       byte ptr [r9],0
       jmp       short M21_L02
       cmp       ecx,0FFFFFFFF
       jne       short M21_L08
       mov       dword ptr [rdx],6
M21_L08:
       inc       dword ptr [rdx]
       jmp       near ptr M21_L01
       cmp       ecx,0FFFFFFFF
       jne       short M21_L07
       mov       eax,[r8+108]
       mov       [rdx],eax
       jmp       short M21_L07
       cmp       ecx,0FFFFFFFF
       jne       short M21_L09
       mov       ecx,[r8+124]
       mov       [rdx],ecx
M21_L09:
       add       dword ptr [rdx],2
       jmp       short M21_L07
M21_L10:
       mov       dword ptr [rdx],0FFFFFFFF
       jmp       near ptr M21_L01
; Total bytes of code 219
```
```assembly
; System.Number.NumberToString[[System.Char, System.Private.CoreLib]](System.Collections.Generic.ValueListBuilder`1<Char> ByRef, NumberBuffer ByRef, Char, Int32, System.Globalization.NumberFormatInfo)
       push      r15
       push      r14
       push      r13
       push      r12
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,58
       xor       eax,eax
       mov       [rsp+38],rax
       vxorps    xmm4,xmm4,xmm4
       vmovdqa   xmmword ptr [rsp+40],xmm4
       mov       [rsp+50],rax
       mov       rbx,rcx
       mov       rsi,rdx
       mov       ebp,r8d
       mov       edi,r9d
M22_L00:
       cmp       byte ptr [rsi+0A],3
       sete      r9b
       movzx     r9d,r9b
       movzx     edx,bp
       cmp       edx,52
       jg        near ptr M22_L28
       movzx     edx,bp
       add       edx,0FFFFFFBD
       cmp       edx,4
       jne       near ptr M22_L26
M22_L01:
       xor       r15d,r15d
       test      edi,edi
       jle       near ptr M22_L47
M22_L02:
       mov       r13,[rsi+10]
       xor       r12d,r12d
       test      edi,edi
       jle       short M22_L04
M22_L03:
       movsxd    r8,r12d
       cmp       byte ptr [r8+r13],0
       je        short M22_L04
       inc       r12d
       cmp       r12d,edi
       jl        short M22_L03
M22_L04:
       cmp       r12d,edi
       je        near ptr M22_L49
M22_L05:
       test      r12d,r12d
       jle       short M22_L07
M22_L06:
       lea       edx,[r12-1]
       movsxd    rdx,edx
       cmp       byte ptr [rdx+r13],30
       je        near ptr M22_L53
M22_L07:
       test      r12d,r12d
       je        near ptr M22_L54
M22_L08:
       movsxd    rdx,r12d
       mov       byte ptr [rdx+r13],0
       mov       [rsi],r12d
M22_L09:
       cmp       byte ptr [rsi+8],0
       jne       near ptr M22_L56
M22_L10:
       mov       r13d,[rsi+4]
       xor       r12d,r12d
       test      r15d,r15d
       jne       short M22_L11
       cmp       r13d,edi
       jg        near ptr M22_L60
       cmp       r13d,0FFFFFFFD
       jl        near ptr M22_L60
M22_L11:
       mov       rdi,[rsi+10]
       test      r13d,r13d
       jle       near ptr M22_L63
M22_L12:
       movzx     ecx,byte ptr [rdi]
       test      ecx,ecx
       je        near ptr M22_L61
       inc       rdi
M22_L13:
       movzx     edx,cx
       mov       ecx,[rbx+8]
       mov       rax,[rbx+10]
       mov       r8d,[rbx+18]
       cmp       ecx,r8d
       jae       near ptr M22_L62
       mov       r8d,ecx
       mov       [rax+r8*2],dx
       inc       ecx
       mov       [rbx+8],ecx
M22_L14:
       dec       r13d
       test      r13d,r13d
       jg        short M22_L12
M22_L15:
       cmp       byte ptr [rdi],0
       je        near ptr M22_L65
M22_L16:
       mov       r14,[rsp+0C0]
       mov       rdx,[r14+30]
       test      rdx,rdx
       jne       near ptr M22_L24
       xor       ecx,ecx
       xor       eax,eax
M22_L17:
       mov       edx,[rbx+8]
       mov       r8,[rbx+10]
       mov       r10d,[rbx+18]
       cmp       eax,1
       jne       near ptr M22_L25
       cmp       edx,r10d
       jae       near ptr M22_L25
       cmp       edx,r10d
       jae       near ptr M22_L74
       mov       eax,edx
       movzx     ecx,word ptr [rcx]
       mov       [r8+rax*2],cx
       inc       edx
       mov       [rbx+8],edx
M22_L18:
       test      r13d,r13d
       jl        near ptr M22_L66
M22_L19:
       cmp       byte ptr [rdi],0
       je        short M22_L22
M22_L20:
       mov       rcx,rdi
       lea       rdi,[rcx+1]
       movzx     edx,byte ptr [rcx]
       mov       ecx,[rbx+8]
       mov       rax,[rbx+10]
       mov       r8d,[rbx+18]
       cmp       ecx,r8d
       jae       near ptr M22_L70
       cmp       ecx,r8d
       jae       near ptr M22_L74
       mov       r8d,ecx
       mov       [rax+r8*2],dx
       inc       ecx
       mov       [rbx+8],ecx
M22_L21:
       cmp       byte ptr [rdi],0
       jne       short M22_L20
M22_L22:
       test      r12d,r12d
       jne       near ptr M22_L71
M22_L23:
       add       rsp,58
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       ret
M22_L24:
       lea       rcx,[rdx+0C]
       mov       eax,[rdx+8]
       jmp       near ptr M22_L17
M22_L25:
       mov       [rsp+48],rcx
       mov       [rsp+50],eax
       lea       rdx,[rsp+48]
       mov       rcx,rbx
       call      qword ptr [7FF977C5C6C0]
       test      r13d,r13d
       jl        near ptr M22_L66
       jmp       near ptr M22_L19
M22_L26:
       cmp       edx,4
       ja        short M22_L27
       mov       edx,edx
       lea       rcx,[7FF9778D6F58]
       mov       ecx,[rcx+rdx*4]
       lea       r8,[M22_L00]
       add       rcx,r8
       jmp       rcx
M22_L27:
       movzx     edx,bp
       add       edx,0FFFFFFB2
       cmp       edx,4
       ja        short M22_L30
       lea       rcx,[7FF9778D6F6C]
       mov       ecx,[rcx+rdx*4]
       lea       r8,[M22_L00]
       add       rcx,r8
       jmp       rcx
M22_L28:
       movzx     edx,bp
       add       edx,0FFFFFF9D
       cmp       edx,4
       ja        short M22_L29
       lea       rcx,[7FF9778D6F80]
       mov       ecx,[rcx+rdx*4]
       lea       r8,[M22_L00]
       add       rcx,r8
       jmp       rcx
M22_L29:
       movzx     r8d,bp
       add       r8d,0FFFFFF92
       cmp       r8d,4
       ja        short M22_L30
       lea       rcx,[7FF9778D6F94]
       mov       ecx,[rcx+r8*4]
       lea       rdx,[M22_L00]
       add       rcx,rdx
       jmp       rcx
M22_L30:
       call      qword ptr [7FF977C5C270]
       int       3
       test      edi,edi
       jge       short M22_L32
       mov       r14,[rsp+0C0]
       mov       edi,[r14+10C]
M22_L31:
       mov       edx,edi
       add       edx,[rsi+4]
       mov       rcx,rsi
       mov       r8d,r9d
       call      qword ptr [7FF977BD7360]; System.Number.RoundNumber(NumberBuffer ByRef, Int32, Boolean)
       mov       rcx,rbx
       mov       rdx,rsi
       mov       r8d,edi
       mov       r9,r14
       call      qword ptr [7FF977C5C528]
       jmp       near ptr M22_L23
M22_L32:
       mov       r14,[rsp+0C0]
       jmp       short M22_L31
       test      edi,edi
       jge       short M22_L33
       mov       r14,[rsp+0C0]
       mov       edi,[r14+108]
       mov       [rsp+0C0],r14
M22_L33:
       mov       edx,edi
       add       edx,[rsi+4]
       mov       rcx,rsi
       mov       r8d,r9d
       call      qword ptr [7FF977BD7360]; System.Number.RoundNumber(NumberBuffer ByRef, Int32, Boolean)
       cmp       byte ptr [rsi+8],0
       je        short M22_L37
       mov       r14,[rsp+0C0]
       mov       rdx,[r14+28]
       test      rdx,rdx
       jne       short M22_L34
       xor       ecx,ecx
       xor       eax,eax
       jmp       short M22_L35
M22_L34:
       lea       rcx,[rdx+0C]
       mov       eax,[rdx+8]
M22_L35:
       mov       edx,[rbx+8]
       mov       r8,[rbx+10]
       mov       r10d,[rbx+18]
       cmp       eax,1
       jne       short M22_L36
       cmp       edx,r10d
       jae       short M22_L36
       mov       eax,edx
       movzx     ecx,word ptr [rcx]
       mov       [r8+rax*2],cx
       inc       edx
       mov       [rbx+8],edx
       mov       [rsp+0C0],r14
       jmp       short M22_L37
M22_L36:
       mov       [rsp+48],rcx
       mov       [rsp+50],eax
       lea       rdx,[rsp+48]
       mov       rcx,rbx
       call      qword ptr [7FF977C5C6C0]
       mov       [rsp+0C0],r14
M22_L37:
       mov       r14,[rsp+0C0]
       mov       r8,[r14+30]
       test      r8,r8
       jne       short M22_L38
       xor       ecx,ecx
       xor       edx,edx
       jmp       short M22_L39
M22_L38:
       lea       rcx,[r8+0C]
       mov       edx,[r8+8]
M22_L39:
       mov       [rsp+48],rcx
       mov       [rsp+50],edx
       xor       r8d,r8d
       mov       [rsp+38],r8
       mov       [rsp+40],r8d
       lea       r8,[rsp+38]
       mov       [rsp+28],r8
       lea       r8,[rsp+48]
       mov       [rsp+20],r8
       mov       r8d,edi
       mov       rcx,rbx
       mov       rdx,rsi
       xor       r9d,r9d
       call      qword ptr [7FF977C5C4C8]
       jmp       near ptr M22_L23
       test      edi,edi
       jge       short M22_L41
       mov       r14,[rsp+0C0]
       mov       edi,[r14+108]
M22_L40:
       mov       edx,edi
       add       edx,[rsi+4]
       mov       rcx,rsi
       mov       r8d,r9d
       call      qword ptr [7FF977BD7360]; System.Number.RoundNumber(NumberBuffer ByRef, Int32, Boolean)
       mov       rcx,rbx
       mov       rdx,rsi
       mov       r8d,edi
       mov       r9,r14
       add       rsp,58
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       jmp       qword ptr [7FF977C5C390]
M22_L41:
       mov       r14,[rsp+0C0]
       jmp       short M22_L40
       mov       ecx,6
       test      edi,edi
       cmovl     edi,ecx
       inc       edi
       mov       rcx,rsi
       mov       edx,edi
       mov       r8d,r9d
       call      qword ptr [7FF977BD7360]; System.Number.RoundNumber(NumberBuffer ByRef, Int32, Boolean)
       cmp       byte ptr [rsi+8],0
       je        short M22_L42
       mov       r14,[rsp+0C0]
       mov       rdx,[r14+28]
       test      rdx,rdx
       jne       short M22_L43
       xor       ecx,ecx
       xor       eax,eax
       jmp       short M22_L44
M22_L42:
       mov       r14,[rsp+0C0]
       jmp       short M22_L46
M22_L43:
       lea       rcx,[rdx+0C]
       mov       eax,[rdx+8]
M22_L44:
       mov       edx,[rbx+8]
       mov       r8,[rbx+10]
       mov       r10d,[rbx+18]
       cmp       eax,1
       jne       short M22_L45
       cmp       edx,r10d
       jae       short M22_L45
       mov       eax,edx
       movzx     ecx,word ptr [rcx]
       mov       [r8+rax*2],cx
       inc       edx
       mov       [rbx+8],edx
       jmp       short M22_L46
M22_L45:
       mov       [rsp+48],rcx
       mov       [rsp+50],eax
       lea       rdx,[rsp+48]
       mov       rcx,rbx
       call      qword ptr [7FF977C5C6C0]
M22_L46:
       movzx     ecx,bp
       mov       [rsp+20],ecx
       mov       rcx,rbx
       mov       rdx,rsi
       mov       r8d,edi
       mov       r9,r14
       call      qword ptr [7FF977C5C4F8]
       jmp       near ptr M22_L23
M22_L47:
       cmp       byte ptr [rsi+0A],2
       jne       short M22_L48
       cmp       edi,0FFFFFFFF
       jne       short M22_L48
       mov       r15d,1
       lea       r8,[rsi+10]
       cmp       dword ptr [r8+8],0
       jbe       near ptr M22_L74
       mov       r8,[r8]
       cmp       byte ptr [r8],0
       jne       near ptr M22_L09
       jmp       near ptr M22_L10
M22_L48:
       mov       edi,[rsi]
       jmp       near ptr M22_L02
M22_L49:
       movzx     r8d,byte ptr [rsi+0A]
       mov       rcx,r13
       mov       edx,edi
       call      qword ptr [7FF977C5CB88]; System.Number.<RoundNumber>g__ShouldRoundUp|162_0(Byte*, Int32, NumberBufferKind, Boolean)
       test      eax,eax
       je        near ptr M22_L05
       test      r12d,r12d
       jle       short M22_L51
M22_L50:
       lea       edx,[r12-1]
       movsxd    rdx,edx
       cmp       byte ptr [rdx+r13],39
       jne       short M22_L51
       dec       r12d
       test      r12d,r12d
       jg        short M22_L50
M22_L51:
       test      r12d,r12d
       jle       short M22_L52
       lea       edx,[r12-1]
       movsxd    rdx,edx
       add       rdx,r13
       inc       byte ptr [rdx]
       jmp       near ptr M22_L07
M22_L52:
       lea       rdx,[rsi+4]
       inc       dword ptr [rdx]
       mov       byte ptr [r13],31
       mov       r12d,1
       jmp       near ptr M22_L07
M22_L53:
       dec       r12d
       test      r12d,r12d
       jle       near ptr M22_L07
       jmp       near ptr M22_L06
M22_L54:
       cmp       byte ptr [rsi+0A],3
       je        short M22_L55
       mov       byte ptr [rsi+8],0
M22_L55:
       xor       edx,edx
       mov       [rsi+4],edx
       jmp       near ptr M22_L08
M22_L56:
       mov       r14,[rsp+0C0]
       mov       rdx,[r14+28]
       test      rdx,rdx
       jne       short M22_L57
       xor       ecx,ecx
       xor       eax,eax
       jmp       short M22_L58
M22_L57:
       lea       rcx,[rdx+0C]
       mov       eax,[rdx+8]
M22_L58:
       mov       edx,[rbx+8]
       mov       r8,[rbx+10]
       mov       r10d,[rbx+18]
       cmp       eax,1
       jne       short M22_L59
       cmp       edx,r10d
       jae       short M22_L59
       mov       eax,edx
       movzx     ecx,word ptr [rcx]
       mov       [r8+rax*2],cx
       inc       edx
       mov       [rbx+8],edx
       mov       [rsp+0C0],r14
       jmp       near ptr M22_L10
M22_L59:
       mov       [rsp+48],rcx
       mov       [rsp+50],eax
       lea       rdx,[rsp+48]
       mov       rcx,rbx
       call      qword ptr [7FF977C5C6C0]
       mov       [rsp+0C0],r14
       jmp       near ptr M22_L10
M22_L60:
       mov       r13d,1
       mov       r12d,1
       jmp       near ptr M22_L11
M22_L61:
       mov       ecx,30
       jmp       near ptr M22_L13
M22_L62:
       mov       rcx,rbx
       call      qword ptr [7FF977C5C7F8]
       jmp       near ptr M22_L14
M22_L63:
       mov       ecx,[rbx+8]
       mov       rdx,[rbx+10]
       mov       eax,[rbx+18]
       cmp       ecx,eax
       jae       short M22_L64
       mov       eax,ecx
       mov       word ptr [rdx+rax*2],30
       inc       ecx
       mov       [rbx+8],ecx
       jmp       near ptr M22_L15
M22_L64:
       mov       rcx,rbx
       mov       edx,30
       call      qword ptr [7FF977C5C7F8]
       jmp       near ptr M22_L15
M22_L65:
       test      r13d,r13d
       jge       short M22_L67
       jmp       near ptr M22_L16
M22_L66:
       mov       ecx,[rbx+8]
       mov       rdx,[rbx+10]
       mov       eax,[rbx+18]
       cmp       ecx,eax
       jae       short M22_L68
       cmp       ecx,eax
       jae       near ptr M22_L74
       mov       eax,ecx
       mov       word ptr [rdx+rax*2],30
       inc       ecx
       mov       [rbx+8],ecx
       jmp       short M22_L69
M22_L67:
       mov       r14,[rsp+0C0]
       jmp       near ptr M22_L22
M22_L68:
       mov       rcx,rbx
       mov       edx,30
       call      qword ptr [7FF977C5C7F8]
M22_L69:
       inc       r13d
       jmp       near ptr M22_L18
M22_L70:
       mov       rcx,rbx
       call      qword ptr [7FF977C5C7F8]
       jmp       near ptr M22_L21
M22_L71:
       mov       dword ptr [rsp+20],2
       mov       dword ptr [rsp+28],1
       mov       r8d,[rsi+4]
       dec       r8d
       lea       r9d,[rbp-2]
       movzx     r9d,r9w
       mov       rcx,rbx
       mov       rdx,r14
       call      qword ptr [7FF977C5C798]
       jmp       near ptr M22_L23
       test      edi,edi
       jge       short M22_L73
       mov       r14,[rsp+0C0]
       mov       edi,[r14+124]
M22_L72:
       lea       rdx,[rsi+4]
       add       dword ptr [rdx],2
       mov       edx,edi
       add       edx,[rsi+4]
       mov       rcx,rsi
       mov       r8d,r9d
       call      qword ptr [7FF977BD7360]; System.Number.RoundNumber(NumberBuffer ByRef, Int32, Boolean)
       mov       rcx,rbx
       mov       rdx,rsi
       mov       r8d,edi
       mov       r9,r14
       add       rsp,58
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       jmp       qword ptr [7FF977C5C360]
M22_L73:
       mov       r14,[rsp+0C0]
       jmp       short M22_L72
       add       ebp,0FFFFFFF5
       movzx     ebp,bp
       jmp       near ptr M22_L01
M22_L74:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 1877
```
```assembly
; System.Number+Grisu3.TryRoundWeedShortest(System.Span`1<Byte>, Int32, UInt64, UInt64, UInt64, UInt64, UInt64)
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,20
       mov       rax,[rsp+60]
       mov       r10,[rsp+70]
       mov       r11,r8
       sub       r11,r10
       add       r8,r10
M23_L00:
       cmp       rax,r11
       jb        short M23_L04
M23_L01:
       cmp       rax,r8
       jae       short M23_L02
       mov       rcx,r9
       sub       rcx,rax
       mov       rsi,[rsp+68]
       cmp       rcx,rsi
       jae       short M23_L05
M23_L02:
       lea       rcx,[r10+r10]
       cmp       rcx,rax
       ja        near ptr M23_L07
       shl       r10,2
       sub       r9,r10
       cmp       r9,rax
       setae     al
       movzx     eax,al
       add       rsp,20
       pop       rbx
       pop       rsi
       pop       rdi
       ret
M23_L03:
       lea       ebx,[rdx-1]
       cmp       ebx,[rcx+8]
       jae       short M23_L08
       lea       ebx,[rdx-1]
       add       rbx,[rcx]
       dec       byte ptr [rbx]
       add       rax,rsi
       mov       [rsp+68],rsi
       jmp       short M23_L00
M23_L04:
       mov       rbx,r9
       sub       rbx,rax
       mov       rsi,[rsp+68]
       cmp       rbx,rsi
       jb        short M23_L06
       lea       rbx,[rax+rsi]
       cmp       rbx,r11
       jb        short M23_L03
       mov       rbx,r11
       sub       rbx,rax
       lea       rdi,[rax+rsi]
       sub       rdi,r11
       cmp       rbx,rdi
       jae       short M23_L03
       mov       [rsp+68],rsi
       jmp       near ptr M23_L01
M23_L05:
       lea       rcx,[rax+rsi]
       cmp       rcx,r8
       jb        short M23_L07
       mov       rcx,r8
       sub       rcx,rax
       add       rsi,rax
       sub       rsi,r8
       cmp       rcx,rsi
       jbe       near ptr M23_L02
       jmp       short M23_L07
M23_L06:
       mov       [rsp+68],rsi
       jmp       near ptr M23_L01
M23_L07:
       xor       eax,eax
       add       rsp,20
       pop       rbx
       pop       rsi
       pop       rdi
       ret
M23_L08:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 226
```
```assembly
; System.Number.ExtractFractionAndBiasedExponent[[System.Double, System.Private.CoreLib]](Double, Int32 ByRef)
       vmovq     rax,xmm0
       mov       rcx,0FFFFFFFFFFFFF
       and       rcx,rax
       shr       rax,34
       and       eax,7FF
       mov       [rdx],eax
       mov       eax,[rdx]
       test      eax,eax
       je        short M24_L01
       mov       r8,10000000000000
       or        rcx,r8
       add       eax,0FFFFFBCD
       mov       [rdx],eax
M24_L00:
       mov       rax,rcx
       ret
M24_L01:
       mov       dword ptr [rdx],0FFFFFBCE
       jmp       short M24_L00
; Total bytes of code 67
```
```assembly
; System.Number+DiyFp.Normalize()
       mov       rax,[rcx]
       xor       r8d,r8d
       lzcnt     r8,rax
       shlx      rax,rax,r8
       mov       ecx,[rcx+8]
       sub       ecx,r8d
       mov       [rdx],rax
       mov       [rdx+8],ecx
       mov       rax,rdx
       ret
; Total bytes of code 32
```
```assembly
; System.Number+Grisu3.TryRunCounted(DiyFp ByRef, Int32, System.Span`1<Byte>, Int32 ByRef, Int32 ByRef)
       push      r15
       push      r14
       push      r13
       push      r12
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,68
       xor       eax,eax
       mov       [rsp+40],rax
       mov       rbx,r9
       mov       eax,[rcx+8]
       lea       r10d,[rax+40]
       neg       r10d
       add       r10d,0FFFFFFC4
       add       r10d,3F
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2sd xmm0,xmm0,r10d
       vmulsd    xmm0,xmm0,qword ptr [7FF9778E56E8]
       vroundsd  xmm0,xmm0,xmm0,0A
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       r10d,7FFFFFFF
       vcvttsd2si r9d,xmm1
       vucomisd  xmm0,qword ptr [7FF9778E56F0]
       cmovb     r10d,r9d
       add       r10d,15B
       mov       r9d,r10d
       sar       r9d,1F
       and       r9d,7
       add       r10d,r9d
       sar       r10d,3
       inc       r10d
       cmp       r10d,57
       jae       near ptr M26_L11
       mov       r9d,r10d
       add       r9,r9
       mov       r11,7FF96DBD5658
       movsx     rsi,word ptr [r11+r9]
       mov       r11,7FF96DBC4128
       mov       r10,[r11+r10*8]
       mov       r11,7FF96DBCCF40
       movsx     r9,word ptr [r11+r9]
       mov       r11,[rcx]
       shr       r11,20
       mov       ecx,[rcx]
       mov       rdi,r10
       shr       rdi,20
       mov       ebp,r11d
       mov       r14d,edi
       imul      rbp,r14
       mov       r14d,ecx
       mov       edi,edi
       imul      rdi,r14
       mov       r14d,r11d
       mov       r15d,r10d
       imul      r14,r15
       mov       r15d,ecx
       mov       r13d,r10d
       imul      r15,r13
       shr       r15,20
       mov       r13d,r14d
       add       r15,r13
       mov       r13d,edi
       add       r15,r13
       mov       r13d,80000000
       add       r15,r13
       lea       ecx,[rax+r9+40]
       shr       r14,20
       add       rbp,r14
       shr       rdi,20
       add       rdi,rbp
       shr       r15,20
       add       rdi,r15
       mov       ebp,edx
       mov       r14,[r8]
       mov       r15d,[r8+8]
       mov       r13d,1
       mov       r12d,ecx
       neg       r12d
       and       r12d,3F
       mov       edx,1
       shlx      rax,rdx,r12
       mov       [rsp+50],rax
       shrx      r10,rdi,r12
       lea       r9,[rax-1]
       mov       [rsp+38],r9
       and       rdi,r9
       jne       short M26_L02
       cmp       ebp,0B
       jl        near ptr M26_L08
M26_L00:
       xor       eax,eax
       mov       [rbx],eax
       mov       [rsp+60],eax
M26_L01:
       mov       ecx,[rsp+60]
       sub       ecx,esi
       mov       rbx,[rsp+0D0]
       mov       [rbx],ecx
       movzx     eax,al
       add       rsp,68
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       ret
M26_L02:
       mov       edx,ecx
       neg       edx
       neg       edx
       add       edx,40
       lea       r8,[rsp+60]
       mov       [rsp+5C],r10d
       mov       ecx,r10d
       call      qword ptr [7FF977C55BF0]; System.Number+Grisu3.BiggestPowerTen(UInt32, Int32, Int32 ByRef)
       mov       r10d,eax
       xor       eax,eax
       mov       [rbx],eax
       cmp       dword ptr [rsp+60],0
       jle       short M26_L04
M26_L03:
       mov       eax,[rsp+5C]
       xor       edx,edx
       div       r10d
       mov       ecx,eax
       imul      ecx,r10d
       mov       edx,[rsp+5C]
       sub       edx,ecx
       mov       r11d,edx
       mov       ecx,[rbx]
       cmp       ecx,r15d
       jae       near ptr M26_L11
       add       eax,30
       mov       [r14+rcx],al
       inc       dword ptr [rbx]
       dec       ebp
       mov       ecx,[rsp+60]
       dec       ecx
       mov       [rsp+60],ecx
       test      ebp,ebp
       je        short M26_L05
       mov       ecx,0CCCCCCCD
       mov       r10d,r10d
       imul      r10,rcx
       shr       r10,23
       cmp       dword ptr [rsp+60],0
       jle       short M26_L05
       mov       [rsp+5C],r11d
       jmp       short M26_L03
M26_L04:
       mov       r11d,[rsp+5C]
M26_L05:
       test      ebp,ebp
       je        near ptr M26_L09
       test      ebp,ebp
       jle       short M26_L07
M26_L06:
       cmp       rdi,r13
       jbe       short M26_L07
       lea       rdi,[rdi+rdi*4]
       add       rdi,rdi
       lea       r13,[r13+r13*4]
       add       r13,r13
       shrx      rcx,rdi,r12
       mov       edx,[rbx]
       cmp       edx,r15d
       jae       near ptr M26_L11
       add       ecx,30
       mov       [r14+rdx],cl
       inc       dword ptr [rbx]
       dec       ebp
       mov       ecx,[rsp+60]
       dec       ecx
       mov       [rsp+60],ecx
       mov       rcx,[rsp+38]
       and       rdi,rcx
       test      ebp,ebp
       jg        short M26_L06
M26_L07:
       test      ebp,ebp
       jne       near ptr M26_L10
       mov       [rsp+40],r14
       mov       [rsp+48],r15d
       mov       [rsp+20],r13
       lea       rcx,[rsp+60]
       mov       [rsp+28],rcx
       lea       rcx,[rsp+40]
       mov       edx,[rbx]
       mov       r8,rdi
       mov       r9,[rsp+50]
       call      qword ptr [7FF977C55C08]; System.Number+Grisu3.TryRoundWeedCounted(System.Span`1<Byte>, Int32, UInt64, UInt64, UInt64, Int32 ByRef)
       jmp       near ptr M26_L01
M26_L08:
       lea       edx,[rbp-1]
       cmp       edx,0A
       jae       short M26_L11
       lea       edx,[rbp-1]
       mov       r8,7FF96DBD36D0
       cmp       r10d,[r8+rdx*4]
       jae       near ptr M26_L02
       jmp       near ptr M26_L00
M26_L09:
       mov       [rsp+40],r14
       mov       [rsp+48],r15d
       mov       qword ptr [rsp+20],1
       lea       rcx,[rsp+60]
       mov       [rsp+28],rcx
       lea       rcx,[rsp+40]
       mov       r8d,r11d
       shlx      r8,r8,r12
       add       r8,rdi
       mov       r9d,r10d
       shlx      r9,r9,r12
       mov       edx,[rbx]
       call      qword ptr [7FF977C55C08]; System.Number+Grisu3.TryRoundWeedCounted(System.Span`1<Byte>, Int32, UInt64, UInt64, UInt64, Int32 ByRef)
       jmp       near ptr M26_L01
M26_L10:
       test      r15d,r15d
       je        short M26_L11
       mov       byte ptr [r14],0
       jmp       near ptr M26_L00
M26_L11:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 803
```
```assembly
; System.Number.Dragon4[[System.Double, System.Private.CoreLib]](Double, Int32, Boolean, NumberBuffer ByRef)
       push      rbp
       push      r15
       push      r14
       push      r13
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,60
       lea       rbp,[rsp+90]
       xor       eax,eax
       mov       [rbp-50],rax
       mov       esi,edx
       mov       edi,r8d
       mov       rbx,r9
       vmovsd    qword ptr [rbp+10],xmm0
       vmovq     rcx,xmm0
       test      rcx,rcx
       jl        near ptr M27_L02
M27_L00:
       lea       rdx,[rbp-38]
       vmovsd    xmm0,qword ptr [rbp+10]
       call      qword ptr [7FF977BD7180]; System.Number.ExtractFractionAndBiasedExponent[[System.Double, System.Private.CoreLib]](Double, Int32 ByRef)
       mov       r14,rax
       xor       r15d,r15d
       mov       rcx,r14
       shr       rcx,34
       je        near ptr M27_L03
       mov       r13d,34
       cmp       r14,100000
       sete      r15b
       movzx     r15d,r15b
M27_L01:
       mov       rcx,7FF977D41CD8
       call      CORINFO_HELP_COUNTPROFILE32
       mov       edx,[rbp-38]
       mov       [rsp+20],esi
       movzx     r8d,dil
       mov       [rsp+28],r8d
       lea       rsi,[rbx+10]
       vmovdqu   xmm0,xmmword ptr [rsi]
       vmovdqu   xmmword ptr [rbp-50],xmm0
       lea       r8,[rbp-40]
       mov       [rsp+38],r8
       lea       r8,[rbp-50]
       mov       [rsp+30],r8
       mov       r8d,r13d
       mov       r9d,r15d
       mov       rcx,r14
       call      qword ptr [7FF977BD7270]; System.Number.Dragon4(UInt64, Int32, UInt32, Boolean, Int32, Boolean, System.Span`1<Byte>, Int32 ByRef)
       mov       ecx,[rbp-40]
       inc       ecx
       mov       [rbx+4],ecx
       cmp       eax,[rsi+8]
       jae       short M27_L04
       mov       rcx,[rsi]
       mov       edx,eax
       mov       byte ptr [rcx+rdx],0
       mov       [rbx],eax
       add       rsp,60
       pop       rbx
       pop       rsi
       pop       rdi
       pop       r13
       pop       r14
       pop       r15
       pop       rbp
       ret
M27_L02:
       mov       rcx,7FF977D41CD0
       call      CORINFO_HELP_COUNTPROFILE32
       jmp       near ptr M27_L00
M27_L03:
       mov       rcx,7FF977D41CD4
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rcx,r14
       or        rcx,1
       xor       r13d,r13d
       lzcnt     r13,rcx
       xor       r13d,3F
       jmp       near ptr M27_L01
M27_L04:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 291
```
```assembly
; System.Number.NumberToStringFormat[[System.Char, System.Private.CoreLib]](System.Collections.Generic.ValueListBuilder`1<Char> ByRef, NumberBuffer ByRef, System.ReadOnlySpan`1<Char>, System.Globalization.NumberFormatInfo)
       push      r15
       push      r14
       push      r13
       push      r12
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,128
       xor       eax,eax
       mov       [rsp+78],rax
       vxorps    xmm4,xmm4,xmm4
       vmovdqu   ymmword ptr [rsp+80],ymm4
       vmovdqu   ymmword ptr [rsp+0A0],ymm4
       vmovdqa   xmmword ptr [rsp+0C0],xmm4
       mov       [rsp+0D0],rax
       mov       rax,21E9420C423F
       mov       [rsp+120],rax
M28_L00:
       mov       [rsp+48],r9
       mov       rsi,r8
       mov       rdi,rdx
       mov       rbp,rcx
       xor       r14d,r14d
       mov       [rsp+100],r14d
       mov       r15,[rdi+10]
       mov       [rsp+0E8],r15
       mov       r13,[rsi]
       mov       r12d,[rsi+8]
       cmp       byte ptr [r15],0
       je        short M28_L02
       cmp       byte ptr [rdi+8],0
       setne     dl
       movzx     edx,dl
M28_L01:
       mov       [rsp+78],r13
       mov       [rsp+80],r12d
       lea       rcx,[rsp+78]
       call      qword ptr [7FF977BD7348]; System.Number.FindSection(System.ReadOnlySpan`1<Char>, Int32)
       jmp       near ptr M28_L29
M28_L02:
       mov       rcx,7FF977CEBF48
       call      CORINFO_HELP_COUNTPROFILE32
       mov       edx,2
       jmp       short M28_L01
       mov       rcx,7FF977CEBF88
       call      CORINFO_HELP_COUNTPROFILE32
       add       ebx,2
       mov       r9d,[rsp+0F4]
       mov       r10,[rsp+0D8]
       jmp       near ptr M28_L30
       mov       rcx,7FF977CEBF4C
       call      CORINFO_HELP_COUNTPROFILE32
       mov       r9d,[rsp+0F4]
       mov       r10,[rsp+0D8]
       jmp       near ptr M28_L30
       mov       rcx,7FF977CEBF60
       call      CORINFO_HELP_COUNTPROFILE32
       mov       r15d,[rsp+11C]
       inc       r15d
       mov       [rsp+11C],r15d
       mov       r9d,[rsp+0F4]
       mov       r10,[rsp+0D8]
       jmp       near ptr M28_L30
M28_L03:
       movsxd    rcx,r11d
       mov       r10,[rsp+0D8]
       cmp       word ptr [r10+rcx*2],0
       jne       short M28_L04
       mov       [rsp+0F4],r11d
       mov       rcx,7FF977CEBF98
       call      CORINFO_HELP_COUNTPROFILE32
       mov       r9d,[rsp+0F4]
       mov       r10,[rsp+0D8]
       jmp       near ptr M28_L30
M28_L04:
       mov       ecx,r11d
       lea       r11d,[rcx+1]
       mov       [rsp+0F4],r11d
       movsxd    rcx,ecx
       movzx     ecx,word ptr [r10+rcx*2]
       mov       [rsp+0E4],r9d
       cmp       ecx,r9d
       je        near ptr M28_L156
       mov       rcx,7FF977CEBF94
       call      CORINFO_HELP_COUNTPROFILE32
       mov       eax,[rsp+0F4]
       cmp       eax,r12d
       mov       r9d,[rsp+0E4]
       jge       near ptr M28_L31
       jmp       near ptr M28_L155
M28_L05:
       lea       ecx,[r9-2C]
       cmp       ecx,4
       ja        near ptr M28_L07
       mov       ecx,ecx
       lea       r9,[7FF9778E7E48]
       mov       r9d,[r9+rcx*4]
       lea       r15,[M28_L00]
       add       r9,r15
       jmp       r9
       mov       rcx,7FF977CEBF50
       call      CORINFO_HELP_COUNTPROFILE32
       mov       r9d,[rsp+0F4]
       mov       r10,[rsp+0D8]
       jmp       near ptr M28_L30
       cmp       dword ptr [rsp+11C],0
       jg        short M28_L06
       mov       rcx,7FF977CEBF7C
       call      CORINFO_HELP_COUNTPROFILE32
       mov       r9d,[rsp+0F4]
       mov       r10,[rsp+0D8]
       jmp       near ptr M28_L30
M28_L06:
       cmp       dword ptr [rsp+118],0
       jl        near ptr M28_L152
       mov       rcx,7FF977CEBF78
       call      CORINFO_HELP_COUNTPROFILE32
       mov       r9d,[rsp+0F4]
       mov       r10,[rsp+0D8]
       jmp       near ptr M28_L30
M28_L07:
       cmp       r9d,45
       je        near ptr M28_L11
       mov       rcx,7FF977CEBF54
       call      CORINFO_HELP_COUNTPROFILE32
       mov       r9d,[rsp+0F4]
       mov       r10,[rsp+0D8]
       jmp       near ptr M28_L30
M28_L08:
       cmp       r9d,5C
       je        near ptr M28_L20
       cmp       r9d,65
       je        short M28_L10
       cmp       r9d,2030
       je        short M28_L09
       mov       rcx,7FF977CEBF58
       call      CORINFO_HELP_COUNTPROFILE32
       mov       r9d,[rsp+0F4]
       mov       r10,[rsp+0D8]
       jmp       near ptr M28_L30
M28_L09:
       mov       rcx,7FF977CEBF8C
       call      CORINFO_HELP_COUNTPROFILE32
       add       ebx,3
       mov       r9d,[rsp+0F4]
       mov       r10,[rsp+0D8]
       jmp       near ptr M28_L30
M28_L10:
       mov       rcx,7FF977CEBF5C
       call      CORINFO_HELP_COUNTPROFILE32
M28_L11:
       mov       eax,[rsp+0F4]
       cmp       eax,r12d
       jge       short M28_L12
       mov       [rsp+0F4],eax
       movsxd    rcx,eax
       mov       rdx,[rsp+0D8]
       cmp       word ptr [rdx+rcx*2],30
       je        short M28_L13
       mov       rcx,7FF977CEBFAC
       call      CORINFO_HELP_COUNTPROFILE32
       mov       eax,[rsp+0F4]
M28_L12:
       mov       [rsp+0F4],eax
       lea       ecx,[rax+1]
       cmp       ecx,r12d
       jl        near ptr M28_L16
       mov       rcx,7FF977CEBFD8
       call      CORINFO_HELP_COUNTPROFILE32
       mov       r9d,[rsp+0F4]
       mov       r10,[rsp+0D8]
       jmp       near ptr M28_L30
M28_L13:
       mov       rcx,7FF977CEBFDC
       call      CORINFO_HELP_COUNTPROFILE32
M28_L14:
       mov       eax,[rsp+0F4]
       lea       edx,[rax+1]
       mov       eax,edx
       mov       [rsp+0F4],eax
       cmp       eax,r12d
       jge       near ptr M28_L19
M28_L15:
       movsxd    rcx,edx
       mov       r8,[rsp+0D8]
       cmp       word ptr [r8+rcx*2],30
       jne       near ptr M28_L18
       mov       rcx,7FF977CEBFCC
       call      CORINFO_HELP_COUNTPROFILE32
       mov       eax,[rsp+0F4]
       lea       edx,[rax+1]
       mov       eax,edx
       mov       [rsp+0F4],eax
       cmp       eax,r12d
       jge       near ptr M28_L19
       jmp       short M28_L15
M28_L16:
       movsxd    rcx,dword ptr [rsp+0F4]
       mov       rdx,[rsp+0D8]
       movzx     ecx,word ptr [rdx+rcx*2]
       cmp       ecx,2B
       je        short M28_L17
       cmp       ecx,2D
       je        near ptr M28_L157
       mov       rcx,7FF977CEBFD4
       call      CORINFO_HELP_COUNTPROFILE32
       mov       r9d,[rsp+0F4]
       mov       r10,[rsp+0D8]
       jmp       near ptr M28_L30
M28_L17:
       mov       eax,[rsp+0F4]
       lea       ecx,[rax+1]
       movsxd    rcx,ecx
       mov       rdx,[rsp+0D8]
       cmp       word ptr [rdx+rcx*2],30
       je        near ptr M28_L14
       mov       rcx,7FF977CEBFD0
       call      CORINFO_HELP_COUNTPROFILE32
       mov       r9d,[rsp+0F4]
       mov       r10,[rsp+0D8]
       jmp       near ptr M28_L30
M28_L18:
       mov       rcx,7FF977CEBFB4
       call      CORINFO_HELP_COUNTPROFILE32
M28_L19:
       mov       rcx,7FF977CEBFB8
       call      CORINFO_HELP_COUNTPROFILE32
       mov       dword ptr [rsp+108],1
       mov       r9d,[rsp+0F4]
       mov       r10,[rsp+0D8]
       jmp       near ptr M28_L30
M28_L20:
       mov       r11d,[rsp+0F4]
       cmp       r11d,r12d
       jl        short M28_L21
       mov       rcx,7FF977CEBFA8
       call      CORINFO_HELP_COUNTPROFILE32
       mov       r9d,[rsp+0F4]
       mov       r10,[rsp+0D8]
       jmp       near ptr M28_L30
M28_L21:
       movsxd    r9,dword ptr [rsp+0F4]
       mov       r10,[rsp+0D8]
       cmp       word ptr [r10+r9*2],0
       jne       short M28_L22
       mov       rcx,7FF977CEBFA4
       call      CORINFO_HELP_COUNTPROFILE32
       mov       r9d,[rsp+0F4]
       mov       r10,[rsp+0D8]
       jmp       near ptr M28_L30
M28_L22:
       mov       rcx,7FF977CEBFA0
       call      CORINFO_HELP_COUNTPROFILE32
       mov       eax,[rsp+0F4]
       inc       eax
       mov       r9d,eax
       mov       r10,[rsp+0D8]
       jmp       near ptr M28_L30
M28_L23:
       mov       rcx,7FF977CEBFBC
       call      CORINFO_HELP_COUNTPROFILE32
       jmp       short M28_L25
M28_L24:
       mov       rcx,7FF977CEBFC8
       call      CORINFO_HELP_COUNTPROFILE32
M28_L25:
       xor       r10d,r10d
       mov       [rsp+0D0],r10
       mov       r8d,[rsp+118]
       test      r8d,r8d
       jge       short M28_L26
       mov       rcx,7FF977CEBFC0
       call      CORINFO_HELP_COUNTPROFILE32
       mov       eax,[rsp+11C]
       mov       edx,eax
       mov       r8d,edx
M28_L26:
       mov       ecx,[rsp+104]
       test      ecx,ecx
       jl        short M28_L27
       mov       [rsp+118],r8d
       cmp       ecx,r8d
       jne       near ptr M28_L32
       mov       rcx,7FF977CEBFC4
       call      CORINFO_HELP_COUNTPROFILE32
       mov       eax,[rsp+100]
       lea       ecx,[rax+rax*2]
       sub       ebx,ecx
       mov       [rsp+100],eax
       mov       r8d,[rsp+118]
M28_L27:
       mov       r15,[rsp+0E8]
       cmp       byte ptr [r15],0
       je        near ptr M28_L35
       lea       rcx,[rdi+4]
       add       [rcx],ebx
       cmp       dword ptr [rsp+108],0
       jne       near ptr M28_L33
       mov       edx,[rsp+11C]
       mov       ecx,edx
       add       ecx,[rdi+4]
       mov       [rsp+118],r8d
       sub       ecx,r8d
       mov       r10d,ecx
M28_L28:
       mov       rcx,rdi
       mov       edx,r10d
       xor       r8d,r8d
       call      qword ptr [7FF977BD7360]; System.Number.RoundNumber(NumberBuffer ByRef, Int32, Boolean)
       cmp       byte ptr [r15],0
       jne       near ptr M28_L37
       mov       [rsi],r13
       mov       [rsi+8],r12d
       mov       rcx,rsi
       mov       edx,2
       call      qword ptr [7FF977BD7348]; System.Number.FindSection(System.ReadOnlySpan`1<Char>, Int32)
       mov       ebx,eax
       mov       eax,[rsp+0F8]
       cmp       ebx,eax
       je        near ptr M28_L34
       mov       rcx,7FF977CEBFE8
       call      CORINFO_HELP_COUNTPROFILE32
       mov       [rsp+0F8],ebx
       mov       eax,[rsp+0F8]
M28_L29:
       xor       edx,edx
       mov       [rsp+11C],edx
       mov       dword ptr [rsp+118],0FFFFFFFF
       mov       dword ptr [rsp+114],7FFFFFFF
       xor       r9d,r9d
       mov       [rsp+110],r9d
       xor       r11d,r11d
       mov       [rsp+108],r11d
       mov       dword ptr [rsp+104],0FFFFFFFF
       xor       r14d,r14d
       xor       ebx,ebx
       mov       [rsp+0F8],eax
       mov       r9d,eax
       mov       [rsp+0D0],r13
       mov       r10,r13
       mov       [rsp+0D8],r10
M28_L30:
       cmp       r9d,r12d
       jge       near ptr M28_L25
       lea       r11d,[r9+1]
       mov       [rsp+0F4],r11d
       movsxd    r9,r9d
       movzx     r9d,word ptr [r10+r9*2]
       test      r9d,r9d
       je        near ptr M28_L24
       cmp       r9d,3B
       je        near ptr M28_L23
       cmp       r9d,45
       jg        near ptr M28_L08
       lea       ecx,[r9-22]
       cmp       ecx,5
       ja        near ptr M28_L05
       mov       ecx,ecx
       lea       r15,[7FF9778E7E5C]
       mov       r15d,[r15+rcx*4]
       lea       rdx,[M28_L00]
       add       r15,rdx
       jmp       r15
       mov       r11d,[rsp+0F4]
       cmp       r11d,r12d
       jl        near ptr M28_L03
       mov       [rsp+0F4],r11d
M28_L31:
       mov       rcx,7FF977CEBF9C
       call      CORINFO_HELP_COUNTPROFILE32
       mov       r9d,[rsp+0F4]
       mov       r10,[rsp+0D8]
       jmp       near ptr M28_L30
M28_L32:
       mov       rcx,7FF977CEBFE0
       call      CORINFO_HELP_COUNTPROFILE32
       mov       r14d,1
       mov       r8d,[rsp+118]
       jmp       near ptr M28_L27
M28_L33:
       mov       [rsp+118],r8d
       mov       rcx,7FF977CEBFE4
       call      CORINFO_HELP_COUNTPROFILE32
       mov       ebx,[rsp+11C]
       mov       r10d,ebx
       jmp       near ptr M28_L28
M28_L34:
       mov       [rsp+0F8],eax
       mov       rcx,7FF977CEBFEC
       call      CORINFO_HELP_COUNTPROFILE32
       jmp       short M28_L37
M28_L35:
       cmp       byte ptr [rdi+0A],3
       mov       [rsp+118],r8d
       je        short M28_L36
       mov       rcx,7FF977CEBFF0
       call      CORINFO_HELP_COUNTPROFILE32
       mov       byte ptr [rdi+8],0
M28_L36:
       mov       rcx,7FF977CEBFF4
       call      CORINFO_HELP_COUNTPROFILE32
       xor       ecx,ecx
       mov       [rdi+4],ecx
M28_L37:
       mov       esi,[rsp+114]
       mov       ebx,[rsp+118]
       cmp       esi,ebx
       jl        near ptr M28_L144
       xor       eax,eax
M28_L38:
       mov       esi,eax
       cmp       [rsp+110],ebx
       jg        near ptr M28_L145
       xor       edx,edx
M28_L39:
       mov       [rsp+110],edx
       cmp       dword ptr [rsp+108],0
       je        near ptr M28_L146
       mov       r8d,ebx
       xor       r10d,r10d
M28_L40:
       mov       r9d,[rsp+0F8]
       mov       [rsp+0F4],r9d
       lea       rcx,[rsp+38]
       mov       [rsp+58],rcx
       mov       dword ptr [rsp+88],4
       mov       r15d,0FFFFFFFF
       test      r14d,r14d
       je        near ptr M28_L46
       mov       rax,[rsp+48]
       mov       rax,[rax+38]
       cmp       dword ptr [rax+8],0
       jle       near ptr M28_L45
       mov       rax,[rsp+48]
       mov       rax,[rax+8]
       mov       [rsp+68],rax
       xor       eax,eax
       mov       [rsp+0CC],eax
       xor       eax,eax
       mov       [rsp+0C8],eax
       mov       rax,[rsp+68]
       mov       eax,[rax+8]
       mov       [rsp+0C4],eax
       test      eax,eax
       je        short M28_L41
       mov       [rsp+0FC],r10d
       mov       [rsp+10C],r8d
       mov       rcx,7FF977CEC008
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rax,[rsp+68]
       mov       ecx,[rax+10]
       mov       edx,ecx
       mov       [rsp+0C8],edx
       mov       r8d,[rsp+10C]
       mov       r10d,[rsp+0FC]
M28_L41:
       mov       eax,[rsp+0C8]
       mov       [rsp+0C0],eax
       mov       [rsp+0FC],r10d
       test      r10d,r10d
       jl        near ptr M28_L149
       xor       eax,eax
M28_L42:
       mov       [rsp+10C],r8d
       add       eax,r8d
       cmp       esi,eax
       jg        near ptr M28_L150
       mov       [rsp+114],esi
M28_L43:
       mov       [rsp+0BC],eax
       mov       esi,[rsp+0C0]
       cmp       eax,esi
       jg        near ptr M28_L143
       mov       rsi,[rsp+58]
M28_L44:
       mov       rcx,7FF977CEC024
       call      CORINFO_HELP_COUNTPROFILE32
       mov       [rsp+58],rsi
       mov       esi,[rsp+114]
       mov       r8d,[rsp+10C]
       mov       r10d,[rsp+0FC]
       jmp       short M28_L46
M28_L45:
       mov       [rsp+0FC],r10d
       mov       [rsp+10C],r8d
       mov       rcx,7FF977CEC00C
       call      CORINFO_HELP_COUNTPROFILE32
       mov       r8d,[rsp+10C]
       mov       r10d,[rsp+0FC]
M28_L46:
       cmp       byte ptr [rdi+8],0
       je        near ptr M28_L52
       cmp       dword ptr [rsp+0F4],0
       jne       near ptr M28_L51
       cmp       dword ptr [rdi+4],0
       je        near ptr M28_L50
       mov       [rsp+0FC],r10d
       mov       [rsp+10C],r8d
       mov       rcx,7FF977CEC028
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rax,[rsp+48]
       mov       rdx,[rax+28]
       test      rdx,rdx
       jne       short M28_L48
       xor       ecx,ecx
       xor       r8d,r8d
M28_L47:
       mov       edx,[rbp+8]
       mov       r10,[rbp+10]
       mov       r9d,[rbp+18]
       cmp       r8d,1
       jne       short M28_L49
       cmp       edx,r9d
       jae       short M28_L49
       mov       r8d,edx
       movzx     ecx,word ptr [rcx]
       mov       [r10+r8*2],cx
       inc       edx
       mov       [rbp+8],edx
       mov       r8d,[rsp+10C]
       mov       r10d,[rsp+0FC]
       jmp       near ptr M28_L52
M28_L48:
       lea       rcx,[rdx+0C]
       mov       r8d,[rdx+8]
       jmp       short M28_L47
M28_L49:
       mov       [rsp+78],rcx
       mov       [rsp+80],r8d
       lea       rdx,[rsp+78]
       mov       rcx,rbp
       call      qword ptr [7FF977C5C6C0]
       mov       r8d,[rsp+10C]
       mov       r10d,[rsp+0FC]
       jmp       short M28_L52
M28_L50:
       mov       [rsp+0FC],r10d
       mov       [rsp+10C],r8d
       mov       rcx,7FF977CEC02C
       call      CORINFO_HELP_COUNTPROFILE32
       mov       r8d,[rsp+10C]
       mov       r10d,[rsp+0FC]
       jmp       short M28_L52
M28_L51:
       mov       [rsp+0FC],r10d
       mov       [rsp+10C],r8d
       mov       rcx,7FF977CEC030
       call      CORINFO_HELP_COUNTPROFILE32
       mov       r8d,[rsp+10C]
       mov       r10d,[rsp+0FC]
M28_L52:
       xor       eax,eax
       mov       [rsp+0E0],eax
       mov       [rsp+0D0],r13
       mov       [rsp+0B0],r13
       mov       rax,[rsp+0E8]
       mov       [rsp+0A8],rax
M28_L53:
       mov       r11d,[rsp+0F4]
       cmp       r11d,r12d
       mov       [rsp+114],esi
       jge       near ptr M28_L112
       lea       esi,[r11+1]
       movsxd    r11,r11d
       movzx     r11d,word ptr [r13+r11*2]
       test      r11d,r11d
       je        near ptr M28_L111
       cmp       r11d,3B
       je        near ptr M28_L110
       mov       [rsp+0FC],r10d
       test      r10d,r10d
       jle       near ptr M28_L72
       cmp       r11d,23
       je        near ptr M28_L63
       cmp       r11d,2E
       je        near ptr M28_L69
       mov       [rsp+0E4],r11d
       cmp       r11d,30
       je        short M28_L54
       mov       [rsp+10C],r8d
       mov       rcx,7FF977CEC038
       call      CORINFO_HELP_COUNTPROFILE32
       mov       r8d,[rsp+10C]
       mov       r11d,[rsp+0E4]
       jmp       near ptr M28_L72
M28_L54:
       mov       [rsp+10C],r8d
       mov       rcx,7FF977CEC03C
       call      CORINFO_HELP_COUNTPROFILE32
       mov       r8d,[rsp+10C]
       mov       r11d,[rsp+0E4]
       jmp       near ptr M28_L63
M28_L55:
       mov       [rsp+0E4],r11d
       mov       [rsp+10C],r8d
       mov       rcx,7FF977CEC044
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rcx,[rsp+0A8]
       lea       rdx,[rcx+1]
       mov       [rsp+0A8],rdx
       movzx     ecx,byte ptr [rcx]
       mov       r8d,[rsp+10C]
       mov       r11d,[rsp+0E4]
       jmp       near ptr M28_L65
M28_L56:
       lea       rcx,[rdx+0C]
       mov       r8d,[rdx+8]
       jmp       near ptr M28_L67
M28_L57:
       mov       [rsp+78],rcx
       mov       [rsp+80],r8d
       lea       rdx,[rsp+78]
       mov       rcx,rbp
       call      qword ptr [7FF977C5C6C0]
       jmp       near ptr M28_L68
M28_L58:
       mov       [rsp+0E4],r11d
       mov       rcx,7FF977CEC054
       call      CORINFO_HELP_COUNTPROFILE32
       jmp       short M28_L62
M28_L59:
       mov       [rsp+0E4],r11d
       mov       [rsp+10C],r8d
       mov       rcx,7FF977CEC058
       call      CORINFO_HELP_COUNTPROFILE32
       jmp       short M28_L62
M28_L60:
       mov       [rsp+0E4],r11d
       mov       [rsp+10C],r8d
       mov       rcx,7FF977CEC05C
       call      CORINFO_HELP_COUNTPROFILE32
       jmp       short M28_L62
M28_L61:
       mov       [rsp+0E4],r11d
       mov       [rsp+10C],r8d
M28_L62:
       mov       rcx,7FF977CEC04C
       call      CORINFO_HELP_COUNTPROFILE32
       mov       r8d,[rsp+10C]
       dec       r8d
       mov       [rsp+10C],r8d
       mov       r10d,[rsp+0FC]
       dec       r10d
       mov       [rsp+0FC],r10d
       mov       r8d,[rsp+10C]
       mov       r11d,[rsp+0E4]
M28_L63:
       cmp       dword ptr [rsp+0FC],0
       jle       near ptr M28_L70
M28_L64:
       mov       rax,[rsp+0A8]
       cmp       byte ptr [rax],0
       jne       near ptr M28_L55
       mov       ecx,30
M28_L65:
       movzx     ecx,cx
       mov       [rsp+90],ecx
       mov       r13d,[rbp+8]
       mov       rdx,[rbp+10]
       mov       r9d,[rbp+18]
       cmp       r13d,r9d
       jae       near ptr M28_L159
       cmp       r13d,r9d
       jae       near ptr M28_L191
       mov       r9d,r13d
       mov       [rdx+r9*2],cx
       inc       r13d
       mov       [rbp+8],r13d
M28_L66:
       test      r14d,r14d
       je        near ptr M28_L61
       cmp       r8d,1
       jle       near ptr M28_L60
       test      r15d,r15d
       jl        near ptr M28_L59
       cmp       r15d,[rsp+88]
       jae       near ptr M28_L191
       mov       ecx,r15d
       mov       rdx,[rsp+58]
       mov       ecx,[rdx+rcx*4]
       inc       ecx
       mov       [rsp+10C],r8d
       cmp       ecx,r8d
       jne       near ptr M28_L58
       mov       [rsp+0E4],r11d
       mov       rcx,7FF977CEC048
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rax,[rsp+48]
       mov       rdx,[rax+38]
       test      rdx,rdx
       jne       near ptr M28_L56
       xor       ecx,ecx
       xor       r8d,r8d
M28_L67:
       mov       edx,[rbp+8]
       mov       r10,[rbp+10]
       mov       r9d,[rbp+18]
       cmp       r8d,1
       jne       near ptr M28_L57
       cmp       edx,r9d
       jae       near ptr M28_L57
       cmp       edx,r9d
       jae       near ptr M28_L191
       mov       r8d,edx
       movzx     ecx,word ptr [rcx]
       mov       [r10+r8*2],cx
       inc       edx
       mov       [rbp+8],edx
M28_L68:
       dec       r15d
       jmp       near ptr M28_L62
M28_L69:
       mov       [rsp+0E4],r11d
       mov       [rsp+10C],r8d
       mov       rcx,7FF977CEC040
       call      CORINFO_HELP_COUNTPROFILE32
       mov       eax,[rsp+0FC]
       test      eax,eax
       mov       [rsp+0FC],eax
       jle       short M28_L71
       mov       r8d,[rsp+10C]
       mov       r11d,[rsp+0E4]
       jmp       near ptr M28_L64
M28_L70:
       mov       [rsp+0E4],r11d
       mov       [rsp+10C],r8d
M28_L71:
       mov       rcx,7FF977CEC050
       call      CORINFO_HELP_COUNTPROFILE32
       mov       r8d,[rsp+10C]
       mov       r11d,[rsp+0E4]
M28_L72:
       cmp       r11d,45
       jg        near ptr M28_L83
       lea       r13d,[r11-22]
       cmp       r13d,5
       ja        near ptr M28_L81
       mov       r13d,r13d
       mov       [rsp+30],r13
       lea       r13,[7FF9778E7E74]
       mov       rdx,[rsp+30]
       mov       r13d,[r13+rdx*4]
       lea       rcx,[M28_L00]
       add       r13,rcx
       jmp       r13
       mov       [rsp+0E4],r11d
       mov       [rsp+10C],r8d
M28_L73:
       mov       rcx,7FF977CEC12C
       call      CORINFO_HELP_COUNTPROFILE32
       mov       ecx,[rbp+8]
       mov       rax,[rbp+10]
       mov       edx,[rbp+18]
       cmp       ecx,edx
       jae       near ptr M28_L190
       cmp       ecx,edx
       jae       near ptr M28_L191
       mov       edx,ecx
       mov       r8d,[rsp+0E4]
       mov       [rax+rdx*2],r8w
       inc       ecx
       mov       [rbp+8],ecx
       mov       [rsp+0F4],esi
       mov       esi,[rsp+114]
       mov       r8d,[rsp+10C]
       mov       r10d,[rsp+0FC]
       mov       r13,[rsp+0B0]
       jmp       near ptr M28_L53
       cmp       esi,r12d
       jge       near ptr M28_L79
M28_L74:
       movsxd    rcx,esi
       mov       rdx,[rsp+0B0]
       movzx     ecx,word ptr [rdx+rcx*2]
       test      ecx,ecx
       je        near ptr M28_L77
       mov       [rsp+0E4],r11d
       cmp       ecx,r11d
       je        short M28_L76
       mov       [rsp+10C],r8d
       mov       rcx,7FF977CEC0BC
       call      CORINFO_HELP_COUNTPROFILE32
       mov       ecx,esi
       lea       esi,[rcx+1]
       movsxd    rcx,ecx
       mov       rax,[rsp+0B0]
       movzx     edx,word ptr [rax+rcx*2]
       mov       ecx,[rbp+8]
       mov       r8,[rbp+10]
       mov       r10d,[rbp+18]
       cmp       ecx,r10d
       jae       near ptr M28_L175
       cmp       ecx,r10d
       jae       near ptr M28_L191
       mov       r10d,ecx
       mov       [r8+r10*2],dx
       inc       ecx
       mov       [rbp+8],ecx
M28_L75:
       cmp       esi,r12d
       mov       r11d,[rsp+0E4]
       jge       short M28_L78
       mov       r8d,[rsp+10C]
       jmp       near ptr M28_L74
M28_L76:
       mov       [rsp+10C],r8d
       mov       rcx,7FF977CEC0C0
       call      CORINFO_HELP_COUNTPROFILE32
       jmp       short M28_L78
M28_L77:
       mov       [rsp+10C],r8d
       mov       rcx,7FF977CEC0D0
       call      CORINFO_HELP_COUNTPROFILE32
M28_L78:
       cmp       esi,r12d
       jl        short M28_L80
       mov       rcx,7FF977CEC0CC
       call      CORINFO_HELP_COUNTPROFILE32
       mov       [rsp+0F4],esi
       mov       esi,[rsp+114]
       mov       r8d,[rsp+10C]
       mov       r10d,[rsp+0FC]
       mov       r13,[rsp+0B0]
       jmp       near ptr M28_L53
M28_L79:
       mov       [rsp+10C],r8d
       jmp       short M28_L78
M28_L80:
       movsxd    rcx,esi
       mov       rax,[rsp+0B0]
       cmp       word ptr [rax+rcx*2],0
       jne       near ptr M28_L176
       mov       rcx,7FF977CEC0C8
       call      CORINFO_HELP_COUNTPROFILE32
       mov       [rsp+0F4],esi
       mov       esi,[rsp+114]
       mov       r8d,[rsp+10C]
       mov       r10d,[rsp+0FC]
       mov       r13,[rsp+0B0]
       jmp       near ptr M28_L53
M28_L81:
       lea       r13d,[r11-2C]
       cmp       r13d,4
       ja        near ptr M28_L82
       mov       r13d,r13d
       mov       [rsp+30],r13
       lea       r13,[7FF9778E7E8C]
       mov       rdx,[rsp+30]
       mov       r13d,[r13+rdx*4]
       lea       rcx,[M28_L00]
       add       r13,rcx
       jmp       r13
       mov       [rsp+0E4],r11d
       mov       [rsp+10C],r8d
       mov       rcx,7FF977CEC064
       call      CORINFO_HELP_COUNTPROFILE32
       jmp       near ptr M28_L73
       mov       [rsp+10C],r8d
       mov       rcx,7FF977CEC068
       call      CORINFO_HELP_COUNTPROFILE32
       mov       [rsp+0F4],esi
       mov       esi,[rsp+114]
       mov       r8d,[rsp+10C]
       mov       r10d,[rsp+0FC]
       mov       r13,[rsp+0B0]
       jmp       near ptr M28_L53
M28_L82:
       mov       [rsp+0E4],r11d
       cmp       r11d,45
       mov       [rsp+10C],r8d
       je        near ptr M28_L89
       mov       rcx,7FF977CEC06C
       call      CORINFO_HELP_COUNTPROFILE32
       jmp       near ptr M28_L73
M28_L83:
       cmp       r11d,5C
       je        near ptr M28_L108
       cmp       r11d,65
       je        near ptr M28_L88
       mov       [rsp+0E4],r11d
       cmp       r11d,2030
       je        short M28_L84
       mov       [rsp+10C],r8d
       mov       rcx,7FF977CEC070
       call      CORINFO_HELP_COUNTPROFILE32
       jmp       near ptr M28_L73
M28_L84:
       mov       [rsp+10C],r8d
       mov       rcx,7FF977CEC0B4
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rax,[rsp+48]
       mov       rdx,[rax+88]
       test      rdx,rdx
       jne       short M28_L86
       xor       ecx,ecx
       xor       r8d,r8d
M28_L85:
       mov       edx,[rbp+8]
       mov       r10,[rbp+10]
       mov       r9d,[rbp+18]
       cmp       r8d,1
       jne       short M28_L87
       cmp       edx,r9d
       jae       short M28_L87
       cmp       edx,r9d
       jae       near ptr M28_L191
       mov       r8d,edx
       movzx     ecx,word ptr [rcx]
       mov       [r10+r8*2],cx
       inc       edx
       mov       [rbp+8],edx
       mov       [rsp+0F4],esi
       mov       esi,[rsp+114]
       mov       r8d,[rsp+10C]
       mov       r10d,[rsp+0FC]
       mov       r13,[rsp+0B0]
       jmp       near ptr M28_L53
M28_L86:
       lea       rcx,[rdx+0C]
       mov       r8d,[rdx+8]
       jmp       short M28_L85
M28_L87:
       mov       [rsp+78],rcx
       mov       [rsp+80],r8d
       lea       rdx,[rsp+78]
       mov       rcx,rbp
       call      qword ptr [7FF977C5C6C0]
       mov       [rsp+0F4],esi
       mov       esi,[rsp+114]
       mov       r8d,[rsp+10C]
       mov       r10d,[rsp+0FC]
       mov       r13,[rsp+0B0]
       jmp       near ptr M28_L53
M28_L88:
       mov       [rsp+0E4],r11d
       mov       [rsp+10C],r8d
       mov       rcx,7FF977CEC074
       call      CORINFO_HELP_COUNTPROFILE32
M28_L89:
       xor       eax,eax
       mov       [rsp+0A4],eax
       xor       edx,edx
       mov       [rsp+0A0],edx
       cmp       dword ptr [rsp+108],0
       je        near ptr M28_L102
       cmp       esi,r12d
       jge       near ptr M28_L139
       movsxd    rcx,esi
       mov       r13,[rsp+0B0]
       cmp       word ptr [r13+rcx*2],30
       jne       near ptr M28_L138
       mov       rcx,7FF977CEC0E0
       call      CORINFO_HELP_COUNTPROFILE32
M28_L90:
       mov       edx,[rsp+0A0]
       inc       edx
       mov       [rsp+0A0],edx
M28_L91:
       inc       esi
       cmp       esi,r12d
       jge       near ptr M28_L96
       movsxd    rcx,esi
       cmp       word ptr [r13+rcx*2],30
       jne       short M28_L95
       mov       rcx,7FF977CEC100
       call      CORINFO_HELP_COUNTPROFILE32
       jmp       short M28_L90
M28_L92:
       mov       rcx,7FF977CEC0EC
       call      CORINFO_HELP_COUNTPROFILE32
       jmp       short M28_L94
M28_L93:
       mov       rcx,7FF977CEC0F0
       call      CORINFO_HELP_COUNTPROFILE32
M28_L94:
       movsxd    rcx,esi
       cmp       word ptr [r13+rcx*2],2D
       jne       near ptr M28_L100
       lea       ecx,[rsi+1]
       movsxd    rcx,ecx
       cmp       word ptr [r13+rcx*2],30
       je        short M28_L91
       mov       rcx,7FF977CEC0F4
       call      CORINFO_HELP_COUNTPROFILE32
       jmp       near ptr M28_L101
M28_L95:
       mov       rcx,7FF977CEC104
       call      CORINFO_HELP_COUNTPROFILE32
M28_L96:
       mov       eax,[rsp+0A0]
       cmp       eax,0A
       jle       short M28_L97
       mov       rcx,7FF977CEC108
       call      CORINFO_HELP_COUNTPROFILE32
       mov       dword ptr [rsp+0A0],0A
       mov       eax,[rsp+0A0]
M28_L97:
       mov       rdx,[rsp+0E8]
       cmp       byte ptr [rdx],0
       je        near ptr M28_L99
       mov       r8d,[rdi+4]
       sub       r8d,ebx
       mov       [rsp+0A0],eax
       mov       [rsp+98],r8d
M28_L98:
       mov       rcx,7FF977CEC110
       call      CORINFO_HELP_COUNTPROFILE32
       mov       ecx,[rsp+0A0]
       mov       [rsp+20],ecx
       mov       ecx,[rsp+0A4]
       mov       [rsp+28],ecx
       mov       rcx,rbp
       mov       rdx,[rsp+48]
       mov       r8d,[rsp+98]
       mov       r9d,[rsp+0E4]
       call      qword ptr [7FF977C5C798]
       xor       r8d,r8d
       mov       [rsp+108],r8d
       mov       [rsp+0F4],esi
       mov       esi,[rsp+114]
       mov       r8d,[rsp+10C]
       mov       r10d,[rsp+0FC]
       jmp       near ptr M28_L53
M28_L99:
       mov       [rsp+0A0],eax
       mov       rcx,7FF977CEC10C
       call      CORINFO_HELP_COUNTPROFILE32
       xor       r8d,r8d
       mov       [rsp+98],r8d
       jmp       near ptr M28_L98
M28_L100:
       mov       rcx,7FF977CEC0FC
       call      CORINFO_HELP_COUNTPROFILE32
M28_L101:
       mov       rcx,7FF977CEC0F8
       call      CORINFO_HELP_COUNTPROFILE32
       mov       ecx,[rbp+8]
       mov       rdx,[rbp+10]
       mov       eax,[rbp+18]
       cmp       ecx,eax
       jae       near ptr M28_L179
       cmp       ecx,eax
       jae       near ptr M28_L191
       mov       eax,ecx
       mov       r8d,[rsp+0E4]
       mov       [rdx+rax*2],r8w
       inc       ecx
       mov       [rbp+8],ecx
       mov       [rsp+0F4],esi
       mov       esi,[rsp+114]
       mov       r8d,[rsp+10C]
       mov       r10d,[rsp+0FC]
       mov       r13,[rsp+0B0]
       jmp       near ptr M28_L53
M28_L102:
       mov       ecx,[rbp+8]
       mov       rdx,[rbp+10]
       mov       eax,[rbp+18]
       cmp       ecx,eax
       jae       near ptr M28_L181
       cmp       ecx,eax
       jae       near ptr M28_L191
       mov       eax,ecx
       mov       r10d,[rsp+0E4]
       mov       [rdx+rax*2],r10w
       inc       ecx
       mov       [rbp+8],ecx
M28_L103:
       cmp       esi,r12d
       jl        short M28_L104
       mov       rcx,7FF977CEC11C
       call      CORINFO_HELP_COUNTPROFILE32
       mov       [rsp+0F4],esi
       mov       esi,[rsp+114]
       mov       r8d,[rsp+10C]
       mov       r10d,[rsp+0FC]
       mov       r13,[rsp+0B0]
       jmp       near ptr M28_L53
M28_L104:
       movsxd    rcx,esi
       mov       r13,[rsp+0B0]
       movzx     ecx,word ptr [r13+rcx*2]
       cmp       ecx,2B
       jne       near ptr M28_L182
M28_L105:
       mov       ecx,esi
       lea       esi,[rcx+1]
       movsxd    rcx,ecx
       movzx     edx,word ptr [r13+rcx*2]
       mov       ecx,[rbp+8]
       mov       rax,[rbp+10]
       mov       r10d,[rbp+18]
       cmp       ecx,r10d
       jae       near ptr M28_L183
       cmp       ecx,r10d
       jae       near ptr M28_L191
       mov       r10d,ecx
       mov       [rax+r10*2],dx
       inc       ecx
       mov       [rbp+8],ecx
M28_L106:
       cmp       esi,r12d
       jl        near ptr M28_L187
M28_L107:
       mov       rcx,7FF977CEC128
       call      CORINFO_HELP_COUNTPROFILE32
       mov       [rsp+0F4],esi
       mov       esi,[rsp+114]
       mov       r8d,[rsp+10C]
       mov       r10d,[rsp+0FC]
       jmp       near ptr M28_L53
M28_L108:
       cmp       esi,r12d
       jl        short M28_L109
       mov       [rsp+10C],r8d
       mov       rcx,7FF977CEC0DC
       call      CORINFO_HELP_COUNTPROFILE32
       mov       [rsp+0F4],esi
       mov       esi,[rsp+114]
       mov       r8d,[rsp+10C]
       mov       r10d,[rsp+0FC]
       mov       r13,[rsp+0B0]
       jmp       near ptr M28_L53
M28_L109:
       movsxd    r11,esi
       mov       r13,[rsp+0B0]
       cmp       word ptr [r13+r11*2],0
       jne       near ptr M28_L177
       mov       [rsp+10C],r8d
       mov       rcx,7FF977CEC0D8
       call      CORINFO_HELP_COUNTPROFILE32
       mov       [rsp+0F4],esi
       mov       esi,[rsp+114]
       mov       r8d,[rsp+10C]
       mov       r10d,[rsp+0FC]
       jmp       near ptr M28_L53
M28_L110:
       mov       rcx,7FF977CEC130
       call      CORINFO_HELP_COUNTPROFILE32
       jmp       short M28_L112
M28_L111:
       mov       rcx,7FF977CEC148
       call      CORINFO_HELP_COUNTPROFILE32
M28_L112:
       xor       ecx,ecx
       mov       [rsp+0D0],rcx
       cmp       byte ptr [rdi+8],0
       je        near ptr M28_L118
       cmp       dword ptr [rsp+0F8],0
       jne       near ptr M28_L117
       cmp       dword ptr [rdi+4],0
       jne       short M28_L116
       cmp       dword ptr [rbp+8],0
       jle       short M28_L115
       mov       rcx,7FF977CEC138
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rcx,7FF977CEC134
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rbx,[rsp+48]
       mov       r8,[rbx+28]
       test      r8,r8
       jne       short M28_L114
       xor       ecx,ecx
       xor       edx,edx
M28_L113:
       mov       [rsp+78],rcx
       mov       [rsp+80],edx
       lea       r8,[rsp+78]
       mov       rcx,rbp
       xor       edx,edx
       call      qword ptr [7FF977C5FEA0]
       jmp       short M28_L119
M28_L114:
       lea       rcx,[r8+0C]
       mov       edx,[r8+8]
       jmp       short M28_L113
M28_L115:
       mov       rcx,7FF977CEC13C
       call      CORINFO_HELP_COUNTPROFILE32
       jmp       short M28_L118
M28_L116:
       mov       rcx,7FF977CEC140
       call      CORINFO_HELP_COUNTPROFILE32
       jmp       short M28_L118
M28_L117:
       mov       rcx,7FF977CEC144
       call      CORINFO_HELP_COUNTPROFILE32
M28_L118:
       mov       rcx,7FF977CEC134
       call      CORINFO_HELP_COUNTPROFILE32
M28_L119:
       mov       r8,21E9420C423F
       cmp       [rsp+120],r8
       je        short M28_L120
       call      CORINFO_HELP_FAIL_FAST
M28_L120:
       nop
       add       rsp,128
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       ret
       mov       [rsp+10C],r8d
       mov       rcx,7FF977CEC0B8
       call      CORINFO_HELP_COUNTPROFILE32
       mov       r13,[rsp+48]
       mov       rdx,[r13+80]
       test      rdx,rdx
       jne       short M28_L122
       xor       ecx,ecx
       xor       eax,eax
M28_L121:
       mov       edx,[rbp+8]
       mov       r8,[rbp+10]
       mov       r10d,[rbp+18]
       cmp       eax,1
       jne       short M28_L123
       cmp       edx,r10d
       jae       short M28_L123
       cmp       edx,r10d
       jae       near ptr M28_L191
       mov       eax,edx
       movzx     ecx,word ptr [rcx]
       mov       [r8+rax*2],cx
       inc       edx
       mov       [rbp+8],edx
       mov       [rsp+0F4],esi
       mov       esi,[rsp+114]
       mov       r8d,[rsp+10C]
       mov       r10d,[rsp+0FC]
       mov       r13,[rsp+0B0]
       jmp       near ptr M28_L53
M28_L122:
       lea       rcx,[rdx+0C]
       mov       eax,[rdx+8]
       jmp       short M28_L121
M28_L123:
       mov       [rsp+78],rcx
       mov       [rsp+80],eax
       lea       rdx,[rsp+78]
       mov       rcx,rbp
       call      qword ptr [7FF977C5C6C0]
       mov       [rsp+0F4],esi
       mov       esi,[rsp+114]
       mov       r8d,[rsp+10C]
       mov       r10d,[rsp+0FC]
       mov       r13,[rsp+0B0]
       jmp       near ptr M28_L53
       mov       r15d,[rsp+114]
       cmp       r15d,7FFFFFFF
       jne       short M28_L124
       mov       rcx,7FF977CEBF64
       call      CORINFO_HELP_COUNTPROFILE32
       mov       r15d,[rsp+11C]
       mov       eax,r15d
       mov       r15d,eax
M28_L124:
       mov       rcx,7FF977CEBF68
       call      CORINFO_HELP_COUNTPROFILE32
       mov       eax,[rsp+11C]
       inc       eax
       mov       [rsp+11C],eax
       mov       edx,eax
       mov       [rsp+114],r15d
       mov       [rsp+110],edx
       mov       r9d,[rsp+0F4]
       mov       r10,[rsp+0D8]
       jmp       near ptr M28_L30
       cmp       dword ptr [rsp+118],0
       jl        short M28_L125
       mov       rcx,7FF977CEBF70
       call      CORINFO_HELP_COUNTPROFILE32
       mov       r9d,[rsp+0F4]
       mov       r10,[rsp+0D8]
       jmp       near ptr M28_L30
M28_L125:
       mov       rcx,7FF977CEBF6C
       call      CORINFO_HELP_COUNTPROFILE32
       mov       eax,[rsp+11C]
       mov       [rsp+118],eax
       mov       r9d,[rsp+0F4]
       mov       r10,[rsp+0D8]
       jmp       near ptr M28_L30
       mov       [rsp+10C],r8d
       mov       rcx,7FF977CEC060
       call      CORINFO_HELP_COUNTPROFILE32
       mov       r8d,[rsp+10C]
       mov       r10d,[rsp+0FC]
       test      r10d,r10d
       jge       near ptr M28_L130
       inc       r10d
       mov       [rsp+0FC],r10d
       mov       [rsp+10C],r8d
       mov       r13d,[rsp+114]
       cmp       r8d,r13d
       jle       near ptr M28_L129
       xor       edx,edx
M28_L126:
       movzx     r11d,dx
       mov       edx,r11d
M28_L127:
       test      edx,edx
       je        near ptr M28_L137
       mov       ecx,[rbp+8]
       mov       [rsp+8C],ecx
       mov       r11,[rbp+10]
       mov       [rsp+50],r11
       mov       r11d,[rbp+18]
       cmp       ecx,r11d
       jae       near ptr M28_L160
       cmp       ecx,r11d
       jae       near ptr M28_L191
       mov       r11d,ecx
       mov       rcx,[rsp+50]
       mov       [rcx+r11*2],dx
       mov       ecx,[rsp+8C]
       inc       ecx
       mov       [rbp+8],ecx
M28_L128:
       test      r14d,r14d
       je        near ptr M28_L136
       mov       r8d,[rsp+10C]
       cmp       r8d,1
       jle       near ptr M28_L135
       test      r15d,r15d
       jl        near ptr M28_L134
       cmp       r15d,[rsp+88]
       jae       near ptr M28_L191
       mov       ecx,r15d
       mov       r11,[rsp+58]
       mov       ecx,[r11+rcx*4]
       inc       ecx
       mov       [rsp+10C],r8d
       cmp       ecx,r8d
       jne       near ptr M28_L166
       jmp       near ptr M28_L161
M28_L129:
       mov       rcx,7FF977CEC078
       call      CORINFO_HELP_COUNTPROFILE32
       mov       edx,30
       jmp       near ptr M28_L126
M28_L130:
       mov       rax,[rsp+0A8]
       cmp       byte ptr [rax],0
       jne       short M28_L132
       mov       [rsp+10C],r8d
       cmp       r8d,[rsp+110]
       jg        short M28_L131
       xor       edx,edx
       mov       [rsp+0FC],r10d
       mov       [rsp+94],edx
       jmp       short M28_L133
M28_L131:
       mov       [rsp+0FC],r10d
       mov       rcx,7FF977CEC07C
       call      CORINFO_HELP_COUNTPROFILE32
       mov       eax,30
       mov       [rsp+94],eax
       jmp       short M28_L133
M28_L132:
       mov       [rsp+0FC],r10d
       mov       [rsp+10C],r8d
       mov       rcx,7FF977CEC080
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rax,[rsp+0A8]
       mov       rcx,rax
       lea       rax,[rcx+1]
       mov       [rsp+0A8],rax
       movzx     ecx,byte ptr [rcx]
       mov       [rsp+94],ecx
M28_L133:
       mov       rcx,7FF977CEC084
       call      CORINFO_HELP_COUNTPROFILE32
       movzx     edx,word ptr [rsp+94]
       mov       eax,edx
       mov       edx,eax
       mov       r13d,[rsp+114]
       jmp       near ptr M28_L127
M28_L134:
       mov       [rsp+10C],r8d
       mov       rcx,7FF977CEC094
       call      CORINFO_HELP_COUNTPROFILE32
       jmp       short M28_L137
M28_L135:
       mov       [rsp+10C],r8d
       mov       rcx,7FF977CEC098
       call      CORINFO_HELP_COUNTPROFILE32
       jmp       short M28_L137
M28_L136:
       mov       rcx,7FF977CEC09C
       call      CORINFO_HELP_COUNTPROFILE32
M28_L137:
       mov       rcx,7FF977CEC08C
       call      CORINFO_HELP_COUNTPROFILE32
       mov       eax,[rsp+10C]
       dec       eax
       mov       [rsp+0F4],esi
       mov       esi,r13d
       mov       r8d,eax
       mov       r10d,[rsp+0FC]
       mov       r13,[rsp+0B0]
       jmp       near ptr M28_L53
       mov       [rsp+10C],r8d
       test      r8d,r8d
       setne     cl
       movzx     ecx,cl
       or        ecx,[rsp+0E0]
       je        near ptr M28_L167
       mov       rcx,7FF977CEC0B0
       call      CORINFO_HELP_COUNTPROFILE32
       mov       [rsp+0F4],esi
       mov       esi,[rsp+114]
       mov       r8d,[rsp+10C]
       mov       r10d,[rsp+0FC]
       mov       r13,[rsp+0B0]
       jmp       near ptr M28_L53
M28_L138:
       mov       rcx,7FF977CEC0E4
       call      CORINFO_HELP_COUNTPROFILE32
M28_L139:
       lea       ecx,[rsi+1]
       cmp       ecx,r12d
       jge       near ptr M28_L101
       movsxd    rcx,esi
       mov       r13,[rsp+0B0]
       cmp       word ptr [r13+rcx*2],2B
       jne       near ptr M28_L93
       lea       ecx,[rsi+1]
       movsxd    rcx,ecx
       cmp       word ptr [r13+rcx*2],30
       jne       near ptr M28_L92
       mov       rcx,7FF977CEC0E8
       call      CORINFO_HELP_COUNTPROFILE32
       mov       dword ptr [rsp+0A4],1
       jmp       near ptr M28_L91
M28_L140:
       mov       r8d,r10d
       shl       r8,2
       mov       rdx,[rsp+58]
       call      qword ptr [7FF977855818]; System.SpanHelpers.Memmove(Byte ByRef, Byte ByRef, UIntPtr)
       mov       rcx,[rsp+60]
       lea       rdx,[rcx+10]
       mov       r10d,[rcx+8]
       mov       rax,rdx
       mov       edx,r10d
       mov       [rsp+88],edx
       mov       [rsp+58],rax
M28_L141:
       cmp       r15d,[rsp+88]
       jae       near ptr M28_L191
       mov       eax,r15d
       mov       rsi,[rsp+58]
       mov       ecx,[rsp+0C8]
       mov       [rsi+rax*4],ecx
       mov       eax,[rsp+0C4]
       dec       eax
       cmp       [rsp+0CC],eax
       jge       short M28_L142
       mov       rcx,7FF977CEC01C
       call      CORINFO_HELP_COUNTPROFILE32
       mov       eax,[rsp+0CC]
       inc       eax
       mov       [rsp+0CC],eax
       mov       ecx,eax
       mov       rdx,[rsp+68]
       mov       ecx,[rdx+rcx*4+10]
       mov       r8d,ecx
       mov       [rsp+0C0],r8d
M28_L142:
       mov       rcx,7FF977CEC020
       call      CORINFO_HELP_COUNTPROFILE32
       mov       ecx,[rsp+0C8]
       mov       r8d,[rsp+0C0]
       add       ecx,r8d
       mov       eax,[rsp+0BC]
       cmp       eax,ecx
       mov       [rsp+0C8],ecx
       jle       near ptr M28_L44
       mov       [rsp+58],rsi
       mov       esi,r8d
M28_L143:
       mov       [rsp+0C0],esi
       test      esi,esi
       je        near ptr M28_L151
       inc       r15d
       cmp       r15d,[rsp+88]
       jl        near ptr M28_L141
       mov       rcx,7FF977CEC018
       call      CORINFO_HELP_COUNTPROFILE32
       mov       eax,[rsp+88]
       lea       edx,[rax+rax]
       movsxd    rdx,edx
       mov       rcx,offset MT_System.Int32[]
       call      CORINFO_HELP_NEWARR_1_VC
       mov       [rsp+60],rax
       lea       rcx,[rax+10]
       mov       r8d,[rax+8]
       mov       r10d,[rsp+88]
       cmp       r10d,r8d
       jg        near ptr M28_L158
       jmp       near ptr M28_L140
M28_L144:
       mov       rcx,7FF977CEBFF8
       call      CORINFO_HELP_COUNTPROFILE32
       mov       eax,ebx
       sub       eax,esi
       jmp       near ptr M28_L38
M28_L145:
       mov       rcx,7FF977CEBFFC
       call      CORINFO_HELP_COUNTPROFILE32
       mov       edx,ebx
       sub       edx,[rsp+110]
       jmp       near ptr M28_L39
M28_L146:
       cmp       [rdi+4],ebx
       jg        short M28_L148
       mov       r8d,ebx
       mov       [rsp+9C],r8d
M28_L147:
       mov       rcx,7FF977CEC004
       call      CORINFO_HELP_COUNTPROFILE32
       mov       r8d,[rsp+9C]
       mov       [rsp+10C],r8d
       mov       r10d,[rdi+4]
       sub       r10d,ebx
       mov       [rsp+0FC],r10d
       mov       r8d,[rsp+10C]
       mov       r10d,[rsp+0FC]
       jmp       near ptr M28_L40
M28_L148:
       mov       rcx,7FF977CEC000
       call      CORINFO_HELP_COUNTPROFILE32
       mov       r8d,[rdi+4]
       mov       [rsp+9C],r8d
       jmp       short M28_L147
M28_L149:
       mov       [rsp+10C],r8d
       mov       rcx,7FF977CEC010
       call      CORINFO_HELP_COUNTPROFILE32
       mov       eax,[rsp+0FC]
       mov       edx,eax
       mov       eax,edx
       mov       r8d,[rsp+10C]
       jmp       near ptr M28_L42
M28_L150:
       mov       rcx,7FF977CEC014
       call      CORINFO_HELP_COUNTPROFILE32
       mov       [rsp+114],esi
       mov       eax,esi
       jmp       near ptr M28_L43
M28_L151:
       mov       rcx,7FF977CEC034
       call      CORINFO_HELP_COUNTPROFILE32
       mov       esi,[rsp+114]
       mov       r8d,[rsp+10C]
       mov       r10d,[rsp+0FC]
       jmp       near ptr M28_L46
M28_L152:
       mov       r9d,[rsp+104]
       test      r9d,r9d
       jl        short M28_L154
       mov       [rsp+104],r9d
       mov       edx,[rsp+11C]
       cmp       r9d,edx
       jne       short M28_L153
       mov       rcx,7FF977CEBF74
       call      CORINFO_HELP_COUNTPROFILE32
       mov       ecx,[rsp+100]
       inc       ecx
       mov       [rsp+100],ecx
       mov       r9d,[rsp+0F4]
       mov       r10,[rsp+0D8]
       jmp       near ptr M28_L30
M28_L153:
       mov       rcx,7FF977CEBF80
       call      CORINFO_HELP_COUNTPROFILE32
       mov       r14d,1
M28_L154:
       mov       rcx,7FF977CEBF84
       call      CORINFO_HELP_COUNTPROFILE32
       mov       eax,[rsp+11C]
       mov       [rsp+104],eax
       mov       r8d,1
       mov       [rsp+100],r8d
       mov       r9d,[rsp+0F4]
       mov       r10,[rsp+0D8]
       jmp       near ptr M28_L30
M28_L155:
       mov       r11d,[rsp+0F4]
       jmp       near ptr M28_L03
M28_L156:
       mov       rcx,7FF977CEBF90
       call      CORINFO_HELP_COUNTPROFILE32
       mov       r9d,[rsp+0F4]
       mov       r10,[rsp+0D8]
       jmp       near ptr M28_L30
M28_L157:
       mov       rcx,7FF977CEBFB0
       call      CORINFO_HELP_COUNTPROFILE32
       jmp       near ptr M28_L17
M28_L158:
       call      qword ptr [7FF977C5FB28]
       int       3
M28_L159:
       mov       [rsp+0E4],r11d
       mov       [rsp+10C],r8d
       mov       rcx,rbp
       mov       edx,[rsp+90]
       call      qword ptr [7FF977C5C7F8]
       mov       r8d,[rsp+10C]
       mov       r11d,[rsp+0E4]
       jmp       near ptr M28_L66
M28_L160:
       mov       rcx,rbp
       call      qword ptr [7FF977C5C7F8]
       jmp       near ptr M28_L128
M28_L161:
       mov       rcx,7FF977CEC088
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rax,[rsp+48]
       mov       rdx,[rax+38]
       test      rdx,rdx
       jne       short M28_L162
       xor       ecx,ecx
       xor       r8d,r8d
       jmp       short M28_L163
M28_L162:
       lea       rcx,[rdx+0C]
       mov       r8d,[rdx+8]
M28_L163:
       mov       edx,[rbp+8]
       mov       r10,[rbp+10]
       mov       r9d,[rbp+18]
       cmp       r8d,1
       jne       short M28_L164
       cmp       edx,r9d
       jae       short M28_L164
       cmp       edx,r9d
       jae       near ptr M28_L191
       mov       r8d,edx
       movzx     ecx,word ptr [rcx]
       mov       [r10+r8*2],cx
       inc       edx
       mov       [rbp+8],edx
       jmp       short M28_L165
M28_L164:
       mov       [rsp+78],rcx
       mov       [rsp+80],r8d
       lea       rdx,[rsp+78]
       mov       rcx,rbp
       call      qword ptr [7FF977C5C6C0]
M28_L165:
       dec       r15d
       jmp       near ptr M28_L137
M28_L166:
       mov       rcx,7FF977CEC090
       call      CORINFO_HELP_COUNTPROFILE32
       jmp       near ptr M28_L137
M28_L167:
       cmp       dword ptr [rsp+110],0
       jl        short M28_L168
       cmp       ebx,[rsp+11C]
       jge       near ptr M28_L174
       mov       rax,[rsp+0A8]
       cmp       byte ptr [rax],0
       je        near ptr M28_L173
       mov       rcx,7FF977CEC0A0
       call      CORINFO_HELP_COUNTPROFILE32
M28_L168:
       mov       rcx,7FF977CEC0A4
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rax,[rsp+48]
       mov       rdx,[rax+30]
       test      rdx,rdx
       jne       short M28_L169
       xor       ecx,ecx
       xor       r8d,r8d
       jmp       short M28_L170
M28_L169:
       lea       rcx,[rdx+0C]
       mov       r8d,[rdx+8]
M28_L170:
       mov       edx,[rbp+8]
       mov       r10,[rbp+10]
       mov       r9d,[rbp+18]
       cmp       r8d,1
       jne       short M28_L171
       cmp       edx,r9d
       jae       short M28_L171
       cmp       edx,r9d
       jae       near ptr M28_L191
       mov       r8d,edx
       movzx     ecx,word ptr [rcx]
       mov       [r10+r8*2],cx
       inc       edx
       mov       [rbp+8],edx
       jmp       short M28_L172
M28_L171:
       mov       [rsp+78],rcx
       mov       [rsp+80],r8d
       lea       rdx,[rsp+78]
       mov       rcx,rbp
       call      qword ptr [7FF977C5C6C0]
M28_L172:
       mov       dword ptr [rsp+0E0],1
       mov       [rsp+0F4],esi
       mov       esi,[rsp+114]
       mov       r8d,[rsp+10C]
       mov       r10d,[rsp+0FC]
       mov       r13,[rsp+0B0]
       jmp       near ptr M28_L53
M28_L173:
       mov       rcx,7FF977CEC0A8
       call      CORINFO_HELP_COUNTPROFILE32
       mov       [rsp+0F4],esi
       mov       esi,[rsp+114]
       mov       r8d,[rsp+10C]
       mov       r10d,[rsp+0FC]
       mov       r13,[rsp+0B0]
       jmp       near ptr M28_L53
M28_L174:
       mov       rcx,7FF977CEC0AC
       call      CORINFO_HELP_COUNTPROFILE32
       mov       [rsp+0F4],esi
       mov       esi,[rsp+114]
       mov       r8d,[rsp+10C]
       mov       r10d,[rsp+0FC]
       mov       r13,[rsp+0B0]
       jmp       near ptr M28_L53
M28_L175:
       mov       rcx,rbp
       call      qword ptr [7FF977C5C7F8]
       jmp       near ptr M28_L75
M28_L176:
       mov       rcx,7FF977CEC0C4
       call      CORINFO_HELP_COUNTPROFILE32
       inc       esi
       mov       [rsp+0F4],esi
       mov       esi,[rsp+114]
       mov       r8d,[rsp+10C]
       mov       r10d,[rsp+0FC]
       mov       r13,[rsp+0B0]
       jmp       near ptr M28_L53
M28_L177:
       mov       [rsp+10C],r8d
       mov       rcx,7FF977CEC0D4
       call      CORINFO_HELP_COUNTPROFILE32
       mov       ecx,esi
       lea       esi,[rcx+1]
       movsxd    rcx,ecx
       movzx     edx,word ptr [r13+rcx*2]
       mov       ecx,[rbp+8]
       mov       rax,[rbp+10]
       mov       r8d,[rbp+18]
       cmp       ecx,r8d
       jae       short M28_L178
       cmp       ecx,r8d
       jae       near ptr M28_L191
       mov       r8d,ecx
       mov       [rax+r8*2],dx
       inc       ecx
       mov       [rbp+8],ecx
       mov       [rsp+0F4],esi
       mov       esi,[rsp+114]
       mov       r8d,[rsp+10C]
       mov       r10d,[rsp+0FC]
       jmp       near ptr M28_L53
M28_L178:
       mov       rcx,rbp
       call      qword ptr [7FF977C5C7F8]
       mov       [rsp+0F4],esi
       mov       esi,[rsp+114]
       mov       r8d,[rsp+10C]
       mov       r10d,[rsp+0FC]
       jmp       near ptr M28_L53
M28_L179:
       mov       r8d,[rsp+0E4]
M28_L180:
       mov       rcx,rbp
       mov       edx,r8d
       call      qword ptr [7FF977C5C7F8]
       mov       [rsp+0F4],esi
       mov       esi,[rsp+114]
       mov       r8d,[rsp+10C]
       mov       r10d,[rsp+0FC]
       mov       r13,[rsp+0B0]
       jmp       near ptr M28_L53
M28_L181:
       mov       r10d,[rsp+0E4]
       mov       rcx,rbp
       mov       edx,r10d
       call      qword ptr [7FF977C5C7F8]
       jmp       near ptr M28_L103
M28_L182:
       cmp       ecx,2D
       jne       short M28_L184
       mov       rcx,7FF977CEC114
       call      CORINFO_HELP_COUNTPROFILE32
       jmp       near ptr M28_L105
M28_L183:
       mov       rcx,rbp
       call      qword ptr [7FF977C5C7F8]
       jmp       near ptr M28_L106
M28_L184:
       mov       rcx,7FF977CEC118
       call      CORINFO_HELP_COUNTPROFILE32
       jmp       near ptr M28_L106
M28_L185:
       mov       rcx,7FF977CEC120
       call      CORINFO_HELP_COUNTPROFILE32
       inc       esi
       mov       rcx,[rsp+70]
       movzx     edx,word ptr [r13+rcx*2]
       mov       ecx,[rbp+8]
       mov       rax,[rbp+10]
       mov       r8d,[rbp+18]
       cmp       ecx,r8d
       jae       short M28_L186
       cmp       ecx,r8d
       jae       short M28_L191
       mov       r8d,ecx
       mov       [rax+r8*2],dx
       inc       ecx
       mov       [rbp+8],ecx
       jmp       short M28_L188
M28_L186:
       mov       rcx,rbp
       call      qword ptr [7FF977C5C7F8]
       jmp       short M28_L188
M28_L187:
       movsxd    rax,esi
       mov       [rsp+70],rax
       cmp       word ptr [r13+rax*2],30
       je        short M28_L185
       jmp       short M28_L189
M28_L188:
       cmp       esi,r12d
       jge       near ptr M28_L107
       jmp       short M28_L187
M28_L189:
       mov       rcx,7FF977CEC124
       call      CORINFO_HELP_COUNTPROFILE32
       mov       [rsp+0F4],esi
       mov       esi,[rsp+114]
       mov       r8d,[rsp+10C]
       mov       r10d,[rsp+0FC]
       jmp       near ptr M28_L53
M28_L190:
       mov       r8d,[rsp+0E4]
       jmp       near ptr M28_L180
M28_L191:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 9221
```
```assembly
; System.SpanHelpers.Memmove(Byte ByRef, Byte ByRef, UIntPtr)
       mov       rax,rcx
       sub       rax,rdx
       cmp       rax,r8
       jb        near ptr M29_L09
       mov       rax,rdx
       sub       rax,rcx
       cmp       rax,r8
       jb        near ptr M29_L09
       lea       rax,[rdx+r8]
       lea       r10,[rcx+r8]
       cmp       r8,10
       ja        short M29_L01
       test      r8b,18
       jne       short M29_L03
       test      r8b,4
       je        short M29_L04
       mov       r8d,[rdx]
       mov       [rcx],r8d
       mov       edx,[rax-4]
       mov       [r10-4],edx
M29_L00:
       vzeroupper
       ret
M29_L01:
       cmp       r8,40
       ja        short M29_L05
M29_L02:
       vmovups   xmm0,[rdx]
       vmovups   [rcx],xmm0
       cmp       r8,20
       jbe       near ptr M29_L08
       vmovups   xmm0,[rdx+10]
       vmovups   [rcx+10],xmm0
       cmp       r8,30
       jbe       near ptr M29_L08
       vmovups   xmm0,[rdx+20]
       vmovups   [rcx+20],xmm0
       jmp       near ptr M29_L08
M29_L03:
       mov       r8,[rdx]
       mov       [rcx],r8
       mov       rcx,[rax-8]
       mov       [r10-8],rcx
       jmp       short M29_L00
M29_L04:
       test      r8,r8
       je        short M29_L00
       movzx     edx,byte ptr [rdx]
       mov       [rcx],dl
       test      r8b,2
       je        short M29_L00
       movsx     rcx,word ptr [rax-2]
       mov       [r10-2],cx
       jmp       short M29_L00
M29_L05:
       cmp       r8,800
       ja        near ptr M29_L10
       cmp       r8,100
       jb        short M29_L06
       mov       r9,rcx
       and       r9,3F
       neg       r9
       add       r9,40
       vmovdqu   ymm0,ymmword ptr [rdx]
       vmovdqu   ymmword ptr [rcx],ymm0
       vmovdqu   ymm0,ymmword ptr [rdx+20]
       vmovdqu   ymmword ptr [rcx+20],ymm0
       add       rdx,r9
       add       rcx,r9
       sub       r8,r9
M29_L06:
       mov       r9,r8
       shr       r9,6
M29_L07:
       vmovdqu   ymm0,ymmword ptr [rdx]
       vmovdqu   ymmword ptr [rcx],ymm0
       vmovdqu   ymm0,ymmword ptr [rdx+20]
       vmovdqu   ymmword ptr [rcx+20],ymm0
       add       rcx,40
       add       rdx,40
       dec       r9
       jne       short M29_L07
       and       r8,3F
       cmp       r8,10
       ja        near ptr M29_L02
M29_L08:
       vmovups   xmm0,[rax-10]
       vmovups   [r10-10],xmm0
       jmp       near ptr M29_L00
M29_L09:
       cmp       rcx,rdx
       jne       short M29_L10
       cmp       [rdx],dl
       jmp       near ptr M29_L00
M29_L10:
       cmp       [rcx],cl
       cmp       [rdx],dl
       vzeroupper
       jmp       qword ptr [7FF9778566E8]; System.Buffer.MemmoveInternal(Byte ByRef, Byte ByRef, UIntPtr)
; Total bytes of code 332
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
       sub       rsp,198
       xor       eax,eax
       mov       [rsp+28],rax
       vxorps    xmm4,xmm4,xmm4
       vmovdqa   xmmword ptr [rsp+30],xmm4
       mov       rax,0FFFFFFFFFFFFFEB0
M00_L00:
       vmovdqa   xmmword ptr [rsp+rax+190],xmm4
       vmovdqa   xmmword ptr [rsp+rax+1A0],xmm4
       vmovdqa   xmmword ptr [rsp+rax+1B0],xmm4
       add       rax,30
       jne       short M00_L00
       mov       [rsp+190],rax
       mov       rbx,rcx
M00_L01:
       xor       esi,esi
       jmp       near ptr M00_L28
M00_L02:
       xor       ecx,ecx
M00_L03:
       test      rcx,rcx
       jne       near ptr M00_L38
       mov       r13,rdi
       test      r13,r13
       je        short M00_L04
       mov       rdx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       cmp       [r13],rdx
       jne       near ptr M00_L39
       xor       r13d,r13d
M00_L04:
       test      r13,r13
       jne       near ptr M00_L40
       mov       [rsp+98],rdi
       mov       [rsp+0A0],rbp
       cmp       qword ptr [rsp+98],0
       je        near ptr M00_L204
       mov       rcx,[rsp+98]
       mov       rax,8001400448
       cmp       rcx,[rax]
       je        near ptr M00_L202
       mov       rcx,[rsp+98]
       mov       rax,8001400450
       cmp       rcx,[rax]
       je        near ptr M00_L203
       movsx     r13,word ptr [rsp+0A0]
M00_L05:
       cmp       r13d,4
       jbe       near ptr M00_L30
       cmp       r13d,1000
       je        near ptr M00_L230
       cmp       r13d,2000
       je        near ptr M00_L231
       mov       rdx,[rsp+98]
       mov       rdi,rdx
       test      rdi,rdi
       je        short M00_L06
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       cmp       [rdi],rcx
       jne       near ptr M00_L208
M00_L06:
       test      rdi,rdi
       je        near ptr M00_L232
M00_L07:
       mov       rcx,8001400A18
       mov       rcx,[rcx]
       mov       r8,[rcx+48]
       test      r8,r8
       jne       short M00_L08
       mov       ecx,7
       call      qword ptr [7FF977BC55A8]; System.Number.<UInt32ToDecStrForKnownSmallNumber>g__CreateAndCacheString|50_0(UInt32)
       mov       r8,rax
M00_L08:
       mov       rbp,r8
       mov       rdx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       cmp       [rdi],rdx
       jne       near ptr M00_L246
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rsp+78],xmm0
       vmovdqu   xmmword ptr [rsp+80],xmm0
       mov       r13,[rdi+20]
       mov       r8,[r13+8]
       mov       r12d,[r8+8]
       dec       r12d
       js        near ptr M00_L32
M00_L09:
       mov       r8,[r13+8]
       cmp       r12d,[r8+8]
       jae       near ptr M00_L247
       mov       edx,r12d
       shl       rdx,5
       mov       r8,[r8+rdx+10]
       cmp       r8,rbp
       je        short M00_L10
       test      r8,r8
       je        near ptr M00_L31
       test      rbp,rbp
       je        near ptr M00_L31
       mov       edx,[r8+8]
       cmp       edx,[rbp+8]
       jne       near ptr M00_L31
       lea       rcx,[r8+0C]
       mov       r8d,[r8+8]
       add       r8d,r8d
       lea       rdx,[rbp+0C]
       call      qword ptr [7FF97784C330]; System.SpanHelpers.SequenceEqual(Byte ByRef, Byte ByRef, UIntPtr)
       test      eax,eax
       je        near ptr M00_L31
M00_L10:
       mov       r8d,r12d
M00_L11:
       test      r8d,r8d
       jl        near ptr M00_L236
       mov       rcx,[r13+8]
       cmp       r8d,[rcx+8]
       jae       near ptr M00_L247
       mov       r8d,r8d
       shl       r8,5
       movzx     r8d,word ptr [rcx+r8+18]
       cmp       qword ptr [rdi+18],0
       jne       near ptr M00_L237
M00_L12:
       xor       ecx,ecx
M00_L13:
       mov       rdx,[rdi+10]
       cmp       r8d,[rdx+8]
       jae       near ptr M00_L247
       mov       r8d,r8d
       shl       r8,4
       vmovdqu   xmm0,xmmword ptr [rdx+r8+10]
       vmovdqu   xmmword ptr [rsp+58],xmm0
       vmovdqu   xmm0,xmmword ptr [rsp+50]
       vmovdqu   xmmword ptr [rsp+78],xmm0
       mov       r8,[rsp+60]
       mov       [rsp+88],r8
       mov       [rsp+78],rcx
M00_L14:
       mov       rdx,[rsp+78]
       test      rdx,rdx
       jne       near ptr M00_L240
M00_L15:
       mov       rdx,[rsp+80]
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BondingAccessor
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rax,rax
       jne       near ptr M00_L241
M00_L16:
       mov       r13d,3
       mov       r12,[rdi+20]
       mov       r8,[r12+8]
       mov       eax,[r8+8]
       dec       eax
       js        near ptr M00_L34
M00_L17:
       mov       r8,[r12+8]
       cmp       eax,[r8+8]
       jae       near ptr M00_L247
       mov       [rsp+4C],eax
       mov       edx,eax
       shl       rdx,5
       mov       r8,[r8+rdx+10]
       cmp       r8,rbp
       je        short M00_L18
       test      r8,r8
       je        near ptr M00_L33
       test      rbp,rbp
       je        near ptr M00_L33
       mov       edx,[r8+8]
       cmp       edx,[rbp+8]
       jne       near ptr M00_L33
       lea       rcx,[r8+0C]
       mov       r8d,[r8+8]
       add       r8d,r8d
       lea       rdx,[rbp+0C]
       call      qword ptr [7FF97784C330]; System.SpanHelpers.SequenceEqual(Byte ByRef, Byte ByRef, UIntPtr)
       test      eax,eax
       je        near ptr M00_L33
M00_L18:
       mov       eax,[rsp+4C]
M00_L19:
       test      eax,eax
       jl        short M00_L20
       mov       rcx,[r12+8]
       cmp       eax,[rcx+8]
       jae       near ptr M00_L247
       mov       eax,eax
       shl       rax,5
       movzx     r13d,word ptr [rcx+rax+1C]
       and       r13d,18
       or        r13d,3
M00_L20:
       xor       eax,eax
       mov       [rsp+40],rax
       mov       eax,[rdi+30]
       test      al,2
       jne       near ptr M00_L27
       test      al,1
       jne       near ptr M00_L242
M00_L21:
       mov       r12,[rdi+20]
       mov       r8,[r12+8]
       mov       eax,[r8+8]
       dec       eax
       js        near ptr M00_L36
M00_L22:
       mov       r8,[r12+8]
       cmp       eax,[r8+8]
       jae       near ptr M00_L247
       mov       [rsp+3C],eax
       mov       edx,eax
       shl       rdx,5
       mov       r8,[r8+rdx+10]
       cmp       r8,rbp
       je        short M00_L23
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
       call      qword ptr [7FF97784C330]; System.SpanHelpers.SequenceEqual(Byte ByRef, Byte ByRef, UIntPtr)
       test      eax,eax
       je        near ptr M00_L35
M00_L23:
       mov       eax,[rsp+3C]
M00_L24:
       test      eax,eax
       jl        near ptr M00_L243
       mov       rcx,[r12+8]
       cmp       eax,[rcx+8]
       jae       near ptr M00_L247
       mov       eax,eax
       shl       rax,5
       mov       rax,[rcx+rax+18]
       mov       [rsp+40],rax
       test      byte ptr [rsp+44],1
       je        near ptr M00_L244
M00_L25:
       mov       rcx,[rdi+20]
       lea       r9,[rsp+40]
       mov       rdx,rbp
       mov       r8d,r13d
       cmp       [rcx],ecx
       call      qword ptr [7FF977A1E928]; AuroraScript.Runtime.Property.HiddenClass.AddProperty(System.String, AuroraScript.Runtime.Property.PropertyFlags, AuroraScript.Runtime.Property.PropertyMeta ByRef)
       lea       rcx,[rdi+20]
       mov       rdx,rax
       call      CORINFO_HELP_ASSIGN_REF
       movzx     ecx,word ptr [rsp+40]
       mov       rax,[rdi+10]
       mov       eax,[rax+8]
       cmp       ecx,eax
       jge       near ptr M00_L245
M00_L26:
       mov       rcx,[rdi+10]
       movzx     edx,word ptr [rsp+40]
       cmp       edx,[rcx+8]
       jae       near ptr M00_L247
       shl       rdx,4
       lea       rdi,[rcx+rdx+10]
       mov       rcx,rdi
       mov       rdx,r14
       call      CORINFO_HELP_ASSIGN_REF
       mov       [rdi+8],r15
M00_L27:
       inc       esi
       cmp       esi,200
       jge       short M00_L29
M00_L28:
       mov       rdi,[rbx+68]
       mov       rbp,[rbx+70]
       mov       rdx,[rbx+8]
       mov       ecx,esi
       and       ecx,1F
       cmp       ecx,[rdx+8]
       jae       near ptr M00_L247
       mov       ecx,esi
       and       ecx,1F
       shl       rcx,4
       lea       rdx,[rdx+rcx+10]
       mov       r14,[rdx]
       mov       r15,[rdx+8]
       mov       rcx,rdi
       test      rcx,rcx
       je        near ptr M00_L03
       mov       rdx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       cmp       [rcx],rdx
       je        near ptr M00_L02
       jmp       near ptr M00_L37
M00_L29:
       add       rsp,198
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       ret
M00_L30:
       mov       ecx,r13d
       lea       rax,[7FF9778C8850]
       mov       eax,[rax+rcx*4]
       lea       rdx,[M00_L01]
       add       rax,rdx
       jmp       rax
       lea       rcx,[rsp+98]
       call      qword ptr [7FF977C45E48]
       mov       rdi,rax
       jmp       near ptr M00_L07
       mov       rcx,[rsp+0A0]
       mov       rax,8007FFFFFFFFFFFF
       add       rax,rcx
       cmp       rax,3
       jbe       near ptr M00_L213
       jmp       near ptr M00_L212
       cmp       qword ptr [rsp+0A0],2
       je        near ptr M00_L210
       jmp       near ptr M00_L209
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001400408
       mov       rdi,[rcx]
       jmp       near ptr M00_L07
M00_L31:
       dec       r12d
       jns       near ptr M00_L09
M00_L32:
       mov       r8d,0FFFFFFFF
       jmp       near ptr M00_L11
M00_L33:
       mov       eax,[rsp+4C]
       dec       eax
       jns       near ptr M00_L17
M00_L34:
       mov       eax,0FFFFFFFF
       jmp       near ptr M00_L19
M00_L35:
       mov       eax,[rsp+3C]
       dec       eax
       jns       near ptr M00_L22
M00_L36:
       mov       eax,0FFFFFFFF
       jmp       near ptr M00_L24
M00_L37:
       mov       rdx,rdi
       mov       rcx,offset MT_AuroraScript.Runtime.Types.IAuroraNativeIndexer
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfInterface(Void*, System.Object)
       mov       rcx,rax
       jmp       near ptr M00_L03
M00_L38:
       mov       [rsp+28],r14
       mov       [rsp+30],r15
       lea       r8,[rsp+28]
       mov       r11,7FF9777904E0
       mov       edx,7
       call      qword ptr [r11]
       jmp       near ptr M00_L27
M00_L39:
       mov       rdx,rdi
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptPackedArray
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r13,rax
       jmp       near ptr M00_L04
M00_L40:
       mov       rdx,r13
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt32Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       near ptr M00_L41
       mov       rdx,r13
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt8Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       near ptr M00_L55
       mov       rdx,r13
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptFloat32Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       near ptr M00_L69
       mov       rdx,r13
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptFloat64Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       near ptr M00_L83
       mov       rdx,r13
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptBooleanArray
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       near ptr M00_L97
       mov       rdx,r13
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptUInt8Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       near ptr M00_L111
       mov       rdx,r13
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt16Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       near ptr M00_L125
       mov       rdx,r13
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptUInt16Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       near ptr M00_L139
       mov       rdx,r13
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptUInt32Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       near ptr M00_L153
       mov       rdx,r13
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt64Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       near ptr M00_L167
       mov       rdx,r13
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptUInt64Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       near ptr M00_L183
       mov       rcx,r13
       mov       rax,[r13]
       mov       rax,[rax+58]
       call      qword ptr [rax+8]
       cmp       eax,7
       ja        near ptr M00_L201
       jmp       near ptr M00_L200
M00_L41:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],7
       jg        short M00_L42
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,7
       call      qword ptr [7FF977C4CA80]
M00_L42:
       mov       rdi,[rdi+38]
       xor       ecx,ecx
       mov       [rsp+190],rcx
       mov       [rsp+180],r14
       mov       [rsp+188],r15
       cmp       qword ptr [rsp+180],0
       je        short M00_L43
       mov       rcx,[rsp+180]
       mov       rdx,8001400448
       cmp       rcx,[rdx]
       je        short M00_L47
       mov       rcx,[rsp+180]
       mov       rdx,8001400450
       cmp       rcx,[rdx]
       je        short M00_L47
       movsx     rcx,word ptr [rsp+188]
       jmp       short M00_L46
M00_L43:
       mov       rcx,[rsp+188]
       test      rcx,rcx
       je        short M00_L44
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L45
       mov       ecx,2
       jmp       short M00_L46
M00_L44:
       xor       ecx,ecx
       jmp       short M00_L46
M00_L45:
       mov       ecx,1
M00_L46:
       test      ecx,ecx
       jne       short M00_L47
       xor       ecx,ecx
       mov       [rsp+190],rcx
       jmp       near ptr M00_L53
M00_L47:
       cmp       qword ptr [rsp+180],0
       jne       short M00_L51
       cmp       qword ptr [rsp+188],2
       jbe       short M00_L51
       mov       rcx,[rsp+188]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L48
       vmovq     xmm0,rcx
       jmp       short M00_L50
M00_L48:
       cmp       rdx,2
       jbe       short M00_L49
       vmovsd    xmm0,qword ptr [7FF9778C8868]
       jmp       short M00_L50
M00_L49:
       vmovq     xmm0,rdx
M00_L50:
       vmovsd    qword ptr [rsp+190],xmm0
       mov       r14d,1
       jmp       short M00_L52
M00_L51:
       lea       rcx,[rsp+180]
       lea       rdx,[rsp+190]
       call      qword ptr [7FF977C4CA98]
       mov       r14d,eax
M00_L52:
       test      r14d,r14d
       jne       short M00_L53
       vmovsd    xmm0,qword ptr [7FF9778C8868]
       jmp       short M00_L54
M00_L53:
       vmovsd    xmm0,qword ptr [rsp+190]
M00_L54:
       cmp       dword ptr [rdi+8],7
       jbe       near ptr M00_L247
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       r8d,7FFFFFFF
       vcvttsd2si ecx,xmm1
       vucomisd  xmm0,qword ptr [7FF9778C8870]
       cmovb     r8d,ecx
       mov       [rdi+2C],r8d
       jmp       near ptr M00_L27
M00_L55:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],7
       jg        short M00_L56
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,7
       call      qword ptr [7FF977C4CA80]
M00_L56:
       mov       rdi,[rdi+38]
       xor       ecx,ecx
       mov       [rsp+178],rcx
       mov       [rsp+168],r14
       mov       [rsp+170],r15
       cmp       qword ptr [rsp+168],0
       je        short M00_L57
       mov       rcx,[rsp+168]
       mov       rdx,8001400448
       cmp       rcx,[rdx]
       je        short M00_L61
       mov       rcx,[rsp+168]
       mov       rdx,8001400450
       cmp       rcx,[rdx]
       je        short M00_L61
       movsx     rcx,word ptr [rsp+170]
       jmp       short M00_L60
M00_L57:
       mov       rcx,[rsp+170]
       test      rcx,rcx
       je        short M00_L58
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L59
       mov       ecx,2
       jmp       short M00_L60
M00_L58:
       xor       ecx,ecx
       jmp       short M00_L60
M00_L59:
       mov       ecx,1
M00_L60:
       test      ecx,ecx
       jne       short M00_L61
       xor       ecx,ecx
       mov       [rsp+178],rcx
       jmp       near ptr M00_L67
M00_L61:
       cmp       qword ptr [rsp+168],0
       jne       short M00_L65
       cmp       qword ptr [rsp+170],2
       jbe       short M00_L65
       mov       rcx,[rsp+170]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L62
       vmovq     xmm0,rcx
       jmp       short M00_L64
M00_L62:
       cmp       rdx,2
       jbe       short M00_L63
       vmovsd    xmm0,qword ptr [7FF9778C8868]
       jmp       short M00_L64
M00_L63:
       vmovq     xmm0,rdx
M00_L64:
       vmovsd    qword ptr [rsp+178],xmm0
       mov       r14d,1
       jmp       short M00_L66
M00_L65:
       lea       rcx,[rsp+168]
       lea       rdx,[rsp+178]
       call      qword ptr [7FF977C4CA98]
       mov       r14d,eax
M00_L66:
       test      r14d,r14d
       jne       short M00_L67
       vmovsd    xmm0,qword ptr [7FF9778C8868]
       jmp       short M00_L68
M00_L67:
       vmovsd    xmm0,qword ptr [rsp+178]
M00_L68:
       cmp       dword ptr [rdi+8],7
       jbe       near ptr M00_L247
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       edx,7FFFFFFF
       vcvttsd2si ecx,xmm1
       vucomisd  xmm0,qword ptr [7FF9778C8870]
       cmovb     edx,ecx
       mov       [rdi+17],dl
       jmp       near ptr M00_L27
M00_L69:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],7
       jg        short M00_L70
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,7
       call      qword ptr [7FF977C4CA80]
M00_L70:
       mov       rdi,[rdi+38]
       xor       ecx,ecx
       mov       [rsp+160],rcx
       mov       [rsp+150],r14
       mov       [rsp+158],r15
       cmp       qword ptr [rsp+150],0
       je        short M00_L71
       mov       rcx,[rsp+150]
       mov       rdx,8001400448
       cmp       rcx,[rdx]
       je        short M00_L75
       mov       rcx,[rsp+150]
       mov       rdx,8001400450
       cmp       rcx,[rdx]
       je        short M00_L75
       movsx     rcx,word ptr [rsp+158]
       jmp       short M00_L74
M00_L71:
       mov       rcx,[rsp+158]
       test      rcx,rcx
       je        short M00_L72
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L73
       mov       ecx,2
       jmp       short M00_L74
M00_L72:
       xor       ecx,ecx
       jmp       short M00_L74
M00_L73:
       mov       ecx,1
M00_L74:
       test      ecx,ecx
       jne       short M00_L75
       xor       ecx,ecx
       mov       [rsp+160],rcx
       jmp       near ptr M00_L81
M00_L75:
       cmp       qword ptr [rsp+150],0
       jne       short M00_L79
       cmp       qword ptr [rsp+158],2
       jbe       short M00_L79
       mov       rcx,[rsp+158]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L76
       vmovq     xmm0,rcx
       jmp       short M00_L78
M00_L76:
       cmp       rdx,2
       jbe       short M00_L77
       vmovsd    xmm0,qword ptr [7FF9778C8868]
       jmp       short M00_L78
M00_L77:
       vmovq     xmm0,rdx
M00_L78:
       vmovsd    qword ptr [rsp+160],xmm0
       mov       r14d,1
       jmp       short M00_L80
M00_L79:
       lea       rcx,[rsp+150]
       lea       rdx,[rsp+160]
       call      qword ptr [7FF977C4CA98]
       mov       r14d,eax
M00_L80:
       test      r14d,r14d
       jne       short M00_L81
       vmovsd    xmm0,qword ptr [7FF9778C8868]
       jmp       short M00_L82
M00_L81:
       vmovsd    xmm0,qword ptr [rsp+160]
M00_L82:
       cmp       dword ptr [rdi+8],7
       jbe       near ptr M00_L247
       vcvtsd2ss xmm0,xmm0,xmm0
       vmovss    dword ptr [rdi+2C],xmm0
       jmp       near ptr M00_L27
M00_L83:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],7
       jg        short M00_L84
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,7
       call      qword ptr [7FF977C4CA80]
M00_L84:
       mov       rdi,[rdi+38]
       xor       ecx,ecx
       mov       [rsp+148],rcx
       mov       [rsp+138],r14
       mov       [rsp+140],r15
       cmp       qword ptr [rsp+138],0
       je        short M00_L85
       mov       rcx,[rsp+138]
       mov       rdx,8001400448
       cmp       rcx,[rdx]
       je        short M00_L89
       mov       rcx,[rsp+138]
       mov       rdx,8001400450
       cmp       rcx,[rdx]
       je        short M00_L89
       movsx     rcx,word ptr [rsp+140]
       jmp       short M00_L88
M00_L85:
       mov       rcx,[rsp+140]
       test      rcx,rcx
       je        short M00_L86
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L87
       mov       ecx,2
       jmp       short M00_L88
M00_L86:
       xor       ecx,ecx
       jmp       short M00_L88
M00_L87:
       mov       ecx,1
M00_L88:
       test      ecx,ecx
       jne       short M00_L89
       xor       ecx,ecx
       mov       [rsp+148],rcx
       jmp       near ptr M00_L95
M00_L89:
       cmp       qword ptr [rsp+138],0
       jne       short M00_L93
       cmp       qword ptr [rsp+140],2
       jbe       short M00_L93
       mov       rcx,[rsp+140]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L90
       vmovq     xmm0,rcx
       jmp       short M00_L92
M00_L90:
       cmp       rdx,2
       jbe       short M00_L91
       vmovsd    xmm0,qword ptr [7FF9778C8868]
       jmp       short M00_L92
M00_L91:
       vmovq     xmm0,rdx
M00_L92:
       vmovsd    qword ptr [rsp+148],xmm0
       mov       r14d,1
       jmp       short M00_L94
M00_L93:
       lea       rcx,[rsp+138]
       lea       rdx,[rsp+148]
       call      qword ptr [7FF977C4CA98]
       mov       r14d,eax
M00_L94:
       test      r14d,r14d
       jne       short M00_L95
       vmovsd    xmm0,qword ptr [7FF9778C8868]
       jmp       short M00_L96
M00_L95:
       vmovsd    xmm0,qword ptr [rsp+148]
M00_L96:
       cmp       dword ptr [rdi+8],7
       jbe       near ptr M00_L247
       vmovsd    qword ptr [rdi+48],xmm0
       jmp       near ptr M00_L27
M00_L97:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],7
       jg        short M00_L98
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,7
       call      qword ptr [7FF977C4CA80]
M00_L98:
       mov       rdi,[rdi+38]
       mov       [rsp+128],r14
       mov       [rsp+130],r15
       cmp       qword ptr [rsp+128],0
       je        short M00_L101
       mov       rdx,[rsp+128]
       mov       rcx,8001400448
       cmp       rdx,[rcx]
       jne       short M00_L99
       mov       r14d,1000
       jmp       short M00_L104
M00_L99:
       mov       rdx,[rsp+128]
       mov       rcx,8001400450
       cmp       rdx,[rcx]
       jne       short M00_L100
       mov       r14d,2000
       jmp       short M00_L104
M00_L100:
       movsx     r14,word ptr [rsp+130]
       jmp       short M00_L104
M00_L101:
       mov       rdx,[rsp+130]
       test      rdx,rdx
       je        short M00_L102
       dec       rdx
       cmp       rdx,1
       jbe       short M00_L103
       mov       r14d,2
       jmp       short M00_L104
M00_L102:
       xor       r14d,r14d
       jmp       short M00_L104
M00_L103:
       mov       r14d,1
M00_L104:
       cmp       r14d,4
       ja        short M00_L105
       mov       edx,r14d
       lea       rcx,[7FF9778C8878]
       mov       ecx,[rcx+rdx*4]
       lea       rax,[M00_L01]
       add       rcx,rax
       jmp       rcx
M00_L105:
       cmp       r14d,1000
       je        near ptr M00_L107
       cmp       r14d,2000
       je        near ptr M00_L107
       lea       rcx,[rsp+128]
       call      qword ptr [7FF977C45E60]; AuroraScript.Runtime.ScriptDatum.get_Object()
       mov       r15,rax
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rdx,8001400408
       cmp       r15,[rdx]
       setne     r15b
       movzx     r15d,r15b
       jmp       near ptr M00_L110
       cmp       qword ptr [rsp+130],2
       sete      r15b
       movzx     r15d,r15b
       jmp       near ptr M00_L110
       mov       rcx,7FF8000000000001
       cmp       [rsp+130],rcx
       je        short M00_L106
       mov       rcx,8000000000000000
       cmp       [rsp+130],rcx
       je        short M00_L106
       mov       rcx,7FF8000000000004
       cmp       [rsp+130],rcx
       setne     r15b
       movzx     r15d,r15b
       jmp       short M00_L110
M00_L106:
       xor       r15d,r15d
       jmp       short M00_L110
M00_L107:
       cmp       qword ptr [rsp+130],0
       setne     r15b
       movzx     r15d,r15b
       jmp       short M00_L110
       mov       rdx,[rsp+128]
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rax,rax
       je        short M00_L108
       cmp       dword ptr [rax+8],0
       sete      cl
       movzx     ecx,cl
       jmp       short M00_L109
M00_L108:
       mov       ecx,1
M00_L109:
       test      ecx,ecx
       sete      r15b
       movzx     r15d,r15b
M00_L110:
       cmp       dword ptr [rdi+8],7
       jbe       near ptr M00_L247
       mov       [rdi+17],r15b
       jmp       near ptr M00_L27
M00_L111:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],7
       jg        short M00_L112
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,7
       call      qword ptr [7FF977C4CA80]
M00_L112:
       mov       rdi,[rdi+38]
       xor       ecx,ecx
       mov       [rsp+120],rcx
       mov       [rsp+110],r14
       mov       [rsp+118],r15
       cmp       qword ptr [rsp+110],0
       je        short M00_L113
       mov       rcx,[rsp+110]
       mov       rdx,8001400448
       cmp       rcx,[rdx]
       je        short M00_L117
       mov       rcx,[rsp+110]
       mov       rdx,8001400450
       cmp       rcx,[rdx]
       je        short M00_L117
       movsx     rcx,word ptr [rsp+118]
       jmp       short M00_L116
M00_L113:
       mov       rcx,[rsp+118]
       test      rcx,rcx
       je        short M00_L114
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L115
       mov       ecx,2
       jmp       short M00_L116
M00_L114:
       xor       ecx,ecx
       jmp       short M00_L116
M00_L115:
       mov       ecx,1
M00_L116:
       test      ecx,ecx
       jne       short M00_L117
       xor       ecx,ecx
       mov       [rsp+120],rcx
       jmp       near ptr M00_L123
M00_L117:
       cmp       qword ptr [rsp+110],0
       jne       short M00_L121
       cmp       qword ptr [rsp+118],2
       jbe       short M00_L121
       mov       rcx,[rsp+118]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L118
       vmovq     xmm0,rcx
       jmp       short M00_L120
M00_L118:
       cmp       rdx,2
       jbe       short M00_L119
       vmovsd    xmm0,qword ptr [7FF9778C8868]
       jmp       short M00_L120
M00_L119:
       vmovq     xmm0,rdx
M00_L120:
       vmovsd    qword ptr [rsp+120],xmm0
       mov       r14d,1
       jmp       short M00_L122
M00_L121:
       lea       rcx,[rsp+110]
       lea       rdx,[rsp+120]
       call      qword ptr [7FF977C4CA98]
       mov       r14d,eax
M00_L122:
       test      r14d,r14d
       jne       short M00_L123
       vmovsd    xmm0,qword ptr [7FF9778C8868]
       jmp       short M00_L124
M00_L123:
       vmovsd    xmm0,qword ptr [rsp+120]
M00_L124:
       cmp       dword ptr [rdi+8],7
       jbe       near ptr M00_L247
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       edx,7FFFFFFF
       vcvttsd2si ecx,xmm1
       vucomisd  xmm0,qword ptr [7FF9778C8870]
       cmovb     edx,ecx
       mov       [rdi+17],dl
       jmp       near ptr M00_L27
M00_L125:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],7
       jg        short M00_L126
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,7
       call      qword ptr [7FF977C4CA80]
M00_L126:
       mov       rdi,[rdi+38]
       xor       ecx,ecx
       mov       [rsp+108],rcx
       mov       [rsp+0F8],r14
       mov       [rsp+100],r15
       cmp       qword ptr [rsp+0F8],0
       je        short M00_L127
       mov       rcx,[rsp+0F8]
       mov       rdx,8001400448
       cmp       rcx,[rdx]
       je        short M00_L131
       mov       rcx,[rsp+0F8]
       mov       rdx,8001400450
       cmp       rcx,[rdx]
       je        short M00_L131
       movsx     rcx,word ptr [rsp+100]
       jmp       short M00_L130
M00_L127:
       mov       rcx,[rsp+100]
       test      rcx,rcx
       je        short M00_L128
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L129
       mov       ecx,2
       jmp       short M00_L130
M00_L128:
       xor       ecx,ecx
       jmp       short M00_L130
M00_L129:
       mov       ecx,1
M00_L130:
       test      ecx,ecx
       jne       short M00_L131
       xor       ecx,ecx
       mov       [rsp+108],rcx
       jmp       near ptr M00_L137
M00_L131:
       cmp       qword ptr [rsp+0F8],0
       jne       short M00_L135
       cmp       qword ptr [rsp+100],2
       jbe       short M00_L135
       mov       rcx,[rsp+100]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L132
       vmovq     xmm0,rcx
       jmp       short M00_L134
M00_L132:
       cmp       rdx,2
       jbe       short M00_L133
       vmovsd    xmm0,qword ptr [7FF9778C8868]
       jmp       short M00_L134
M00_L133:
       vmovq     xmm0,rdx
M00_L134:
       vmovsd    qword ptr [rsp+108],xmm0
       mov       r14d,1
       jmp       short M00_L136
M00_L135:
       lea       rcx,[rsp+0F8]
       lea       rdx,[rsp+108]
       call      qword ptr [7FF977C4CA98]
       mov       r14d,eax
M00_L136:
       test      r14d,r14d
       jne       short M00_L137
       vmovsd    xmm0,qword ptr [7FF9778C8868]
       jmp       short M00_L138
M00_L137:
       vmovsd    xmm0,qword ptr [rsp+108]
M00_L138:
       cmp       dword ptr [rdi+8],7
       jbe       near ptr M00_L247
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       edx,7FFFFFFF
       vcvttsd2si ecx,xmm1
       vucomisd  xmm0,qword ptr [7FF9778C8870]
       cmovb     edx,ecx
       mov       [rdi+1E],dx
       jmp       near ptr M00_L27
M00_L139:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],7
       jg        short M00_L140
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,7
       call      qword ptr [7FF977C4CA80]
M00_L140:
       mov       rdi,[rdi+38]
       xor       ecx,ecx
       mov       [rsp+0F0],rcx
       mov       [rsp+0E0],r14
       mov       [rsp+0E8],r15
       cmp       qword ptr [rsp+0E0],0
       je        short M00_L141
       mov       rcx,[rsp+0E0]
       mov       rdx,8001400448
       cmp       rcx,[rdx]
       je        short M00_L145
       mov       rcx,[rsp+0E0]
       mov       rdx,8001400450
       cmp       rcx,[rdx]
       je        short M00_L145
       movsx     rcx,word ptr [rsp+0E8]
       jmp       short M00_L144
M00_L141:
       mov       rcx,[rsp+0E8]
       test      rcx,rcx
       je        short M00_L142
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L143
       mov       ecx,2
       jmp       short M00_L144
M00_L142:
       xor       ecx,ecx
       jmp       short M00_L144
M00_L143:
       mov       ecx,1
M00_L144:
       test      ecx,ecx
       jne       short M00_L145
       xor       ecx,ecx
       mov       [rsp+0F0],rcx
       jmp       near ptr M00_L151
M00_L145:
       cmp       qword ptr [rsp+0E0],0
       jne       short M00_L149
       cmp       qword ptr [rsp+0E8],2
       jbe       short M00_L149
       mov       rcx,[rsp+0E8]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L146
       vmovq     xmm0,rcx
       jmp       short M00_L148
M00_L146:
       cmp       rdx,2
       jbe       short M00_L147
       vmovsd    xmm0,qword ptr [7FF9778C8868]
       jmp       short M00_L148
M00_L147:
       vmovq     xmm0,rdx
M00_L148:
       vmovsd    qword ptr [rsp+0F0],xmm0
       mov       r14d,1
       jmp       short M00_L150
M00_L149:
       lea       rcx,[rsp+0E0]
       lea       rdx,[rsp+0F0]
       call      qword ptr [7FF977C4CA98]
       mov       r14d,eax
M00_L150:
       test      r14d,r14d
       jne       short M00_L151
       vmovsd    xmm0,qword ptr [7FF9778C8868]
       jmp       short M00_L152
M00_L151:
       vmovsd    xmm0,qword ptr [rsp+0F0]
M00_L152:
       cmp       dword ptr [rdi+8],7
       jbe       near ptr M00_L247
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       edx,7FFFFFFF
       vcvttsd2si ecx,xmm1
       vucomisd  xmm0,qword ptr [7FF9778C8870]
       cmovb     edx,ecx
       mov       [rdi+1E],dx
       jmp       near ptr M00_L27
M00_L153:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],7
       jg        short M00_L154
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,7
       call      qword ptr [7FF977C4CA80]
M00_L154:
       mov       rdi,[rdi+38]
       xor       ecx,ecx
       mov       [rsp+0D8],rcx
       mov       [rsp+0C8],r14
       mov       [rsp+0D0],r15
       cmp       qword ptr [rsp+0C8],0
       je        short M00_L155
       mov       rcx,[rsp+0C8]
       mov       rdx,8001400448
       cmp       rcx,[rdx]
       je        short M00_L159
       mov       rcx,[rsp+0C8]
       mov       rdx,8001400450
       cmp       rcx,[rdx]
       je        short M00_L159
       movsx     rcx,word ptr [rsp+0D0]
       jmp       short M00_L158
M00_L155:
       mov       rcx,[rsp+0D0]
       test      rcx,rcx
       je        short M00_L156
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L157
       mov       ecx,2
       jmp       short M00_L158
M00_L156:
       xor       ecx,ecx
       jmp       short M00_L158
M00_L157:
       mov       ecx,1
M00_L158:
       test      ecx,ecx
       jne       short M00_L159
       xor       ecx,ecx
       mov       [rsp+0D8],rcx
       jmp       near ptr M00_L165
M00_L159:
       cmp       qword ptr [rsp+0C8],0
       jne       short M00_L163
       cmp       qword ptr [rsp+0D0],2
       jbe       short M00_L163
       mov       rcx,[rsp+0D0]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L160
       vmovq     xmm0,rcx
       jmp       short M00_L162
M00_L160:
       cmp       rdx,2
       jbe       short M00_L161
       vmovsd    xmm0,qword ptr [7FF9778C8868]
       jmp       short M00_L162
M00_L161:
       vmovq     xmm0,rdx
M00_L162:
       vmovsd    qword ptr [rsp+0D8],xmm0
       mov       r14d,1
       jmp       short M00_L164
M00_L163:
       lea       rcx,[rsp+0C8]
       lea       rdx,[rsp+0D8]
       call      qword ptr [7FF977C4CA98]
       mov       r14d,eax
M00_L164:
       test      r14d,r14d
       jne       short M00_L165
       vmovsd    xmm0,qword ptr [7FF9778C8868]
       jmp       short M00_L166
M00_L165:
       vmovsd    xmm0,qword ptr [rsp+0D8]
M00_L166:
       cmp       dword ptr [rdi+8],7
       jbe       near ptr M00_L247
       vxorps    xmm1,xmm1,xmm1
       vmaxsd    xmm1,xmm0,xmm1
       mov       edx,0FFFFFFFF
       vcvttsd2si rcx,xmm1
       vucomisd  xmm0,qword ptr [7FF9778C8890]
       cmovb     edx,ecx
       mov       [rdi+2C],edx
       jmp       near ptr M00_L27
M00_L167:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],7
       jg        short M00_L168
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,7
       call      qword ptr [7FF977C4CA80]
M00_L168:
       mov       rdi,[rdi+38]
       test      r14,r14
       je        short M00_L171
       mov       r8,8001400448
       cmp       r14,[r8]
       jne       short M00_L169
       mov       r8d,1000
       jmp       short M00_L174
M00_L169:
       mov       r8,8001400450
       cmp       r14,[r8]
       jne       short M00_L170
       mov       r8d,2000
       jmp       short M00_L174
M00_L170:
       movsx     r8,r15w
       jmp       short M00_L174
M00_L171:
       test      r15,r15
       je        short M00_L172
       lea       r8,[r15-1]
       cmp       r8,1
       jbe       short M00_L173
       mov       r8d,2
       jmp       short M00_L174
M00_L172:
       xor       r8d,r8d
       jmp       short M00_L174
M00_L173:
       mov       r8d,1
M00_L174:
       cmp       r8d,2
       je        short M00_L176
       cmp       r8d,1000
       je        short M00_L175
       cmp       r8d,2000
       jne       near ptr M00_L181
       test      r15,r15
       jl        near ptr M00_L181
M00_L175:
       mov       rax,r15
       jmp       near ptr M00_L182
M00_L176:
       mov       rax,8007FFFFFFFFFFFF
       add       rax,r15
       cmp       rax,3
       jbe       short M00_L177
       vmovq     xmm0,r15
       jmp       short M00_L179
M00_L177:
       cmp       rax,2
       jbe       short M00_L178
       vmovsd    xmm0,qword ptr [7FF9778C8868]
       jmp       short M00_L179
M00_L178:
       vmovq     xmm0,rax
M00_L179:
       vucomisd  xmm0,qword ptr [7FF9778C8898]
       jb        short M00_L181
       vmovsd    xmm1,qword ptr [7FF9778C88A0]
       vucomisd  xmm1,xmm0
       jbe       short M00_L181
       vroundsd  xmm1,xmm1,xmm0,0B
       vucomisd  xmm1,xmm0
       jp        short M00_L181
       jne       short M00_L181
       vxorps    xmm1,xmm1,xmm1
       vucomisd  xmm0,xmm1
       jp        short M00_L180
       jne       short M00_L180
       vmovq     r8,xmm0
       not       r8
       shr       r8,3F
       test      r8d,r8d
       je        short M00_L181
M00_L180:
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       r8,7FFFFFFFFFFFFFFF
       vcvttsd2si rcx,xmm1
       vucomisd  xmm0,qword ptr [7FF9778C88A0]
       cmovb     r8,rcx
       mov       rax,r8
       jmp       short M00_L182
M00_L181:
       mov       [rsp+28],r14
       mov       [rsp+30],r15
       lea       r8,[rsp+28]
       lea       rcx,[rsp+0B8]
       mov       edx,13
       call      qword ptr [7FF977C4CAB0]
       xor       eax,eax
M00_L182:
       cmp       dword ptr [rdi+8],7
       jbe       near ptr M00_L247
       mov       [rdi+48],rax
       jmp       near ptr M00_L27
M00_L183:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],7
       jg        short M00_L184
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,7
       call      qword ptr [7FF977C4CA80]
M00_L184:
       mov       rdi,[rdi+38]
       test      r14,r14
       je        short M00_L187
       mov       r8,8001400448
       cmp       r14,[r8]
       jne       short M00_L185
       mov       r8d,1000
       jmp       short M00_L190
M00_L185:
       mov       r8,8001400450
       cmp       r14,[r8]
       jne       short M00_L186
       mov       r8d,2000
       jmp       short M00_L190
M00_L186:
       movsx     r8,r15w
       jmp       short M00_L190
M00_L187:
       test      r15,r15
       je        short M00_L188
       lea       r8,[r15-1]
       cmp       r8,1
       jbe       short M00_L189
       mov       r8d,2
       jmp       short M00_L190
M00_L188:
       xor       r8d,r8d
       jmp       short M00_L190
M00_L189:
       mov       r8d,1
M00_L190:
       cmp       r8d,2
       je        short M00_L193
       cmp       r8d,1000
       je        short M00_L191
       cmp       r8d,2000
       jne       near ptr M00_L198
       jmp       short M00_L192
M00_L191:
       test      r15,r15
       jl        near ptr M00_L198
M00_L192:
       jmp       near ptr M00_L199
M00_L193:
       mov       r8,8007FFFFFFFFFFFF
       add       r8,r15
       cmp       r8,3
       jbe       short M00_L194
       vmovq     xmm0,r15
       jmp       short M00_L196
M00_L194:
       cmp       r8,2
       jbe       short M00_L195
       vmovsd    xmm0,qword ptr [7FF9778C8868]
       jmp       short M00_L196
M00_L195:
       vmovq     xmm0,r8
M00_L196:
       vxorps    xmm1,xmm1,xmm1
       vucomisd  xmm0,xmm1
       jb        short M00_L198
       vmovsd    xmm1,qword ptr [7FF9778C88A8]
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
       vxorps    xmm1,xmm1,xmm1
       vmaxsd    xmm1,xmm0,xmm1
       vmovsd    xmm2,qword ptr [7FF9778C88A8]
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
       jmp       short M00_L199
M00_L198:
       mov       [rsp+28],r14
       mov       [rsp+30],r15
       lea       r8,[rsp+28]
       lea       rcx,[rsp+0A8]
       mov       edx,14
       call      qword ptr [7FF977C4CAB0]
       xor       r15d,r15d
M00_L199:
       cmp       dword ptr [rdi+8],7
       jbe       near ptr M00_L247
       mov       [rdi+48],r15
       jmp       near ptr M00_L27
M00_L200:
       mov       rcx,r13
       mov       rax,[r13]
       mov       rax,[rax+58]
       call      qword ptr [rax+8]
       mov       edx,eax
       mov       ecx,7
       call      qword ptr [7FF977C4CA80]
M00_L201:
       mov       [rsp+28],r14
       mov       [rsp+30],r15
       lea       r8,[rsp+28]
       mov       rcx,r13
       mov       edx,7
       mov       rax,[r13]
       mov       rax,[rax+58]
       call      qword ptr [rax+18]
       jmp       near ptr M00_L27
M00_L202:
       mov       r13d,1000
       jmp       near ptr M00_L05
M00_L203:
       mov       r13d,2000
       jmp       near ptr M00_L05
M00_L204:
       mov       rcx,[rsp+0A0]
       test      rcx,rcx
       je        short M00_L205
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L206
       mov       r13d,2
       jmp       short M00_L207
M00_L205:
       xor       r13d,r13d
       jmp       short M00_L207
M00_L206:
       mov       r13d,1
M00_L207:
       jmp       near ptr M00_L05
M00_L208:
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       jmp       near ptr M00_L06
M00_L209:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BooleanValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001401510
       mov       rdi,[rcx]
       jmp       short M00_L211
M00_L210:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BooleanValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001401508
       mov       rdi,[rcx]
M00_L211:
       jmp       near ptr M00_L07
M00_L212:
       vmovq     xmm1,rcx
       jmp       short M00_L215
M00_L213:
       cmp       rax,2
       jbe       short M00_L214
       vmovsd    xmm1,qword ptr [7FF9778C8868]
       jmp       short M00_L215
M00_L214:
       vmovq     xmm1,rax
M00_L215:
       vucomisd  xmm1,xmm1
       jp        short M00_L216
       je        short M00_L217
M00_L216:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80014014F8
       mov       rdi,[rcx]
       jmp       near ptr M00_L229
M00_L217:
       vxorps    xmm0,xmm0,xmm0
       vucomisd  xmm1,xmm0
       jp        short M00_L218
       jne       short M00_L218
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80014014A8
       mov       rdi,[rcx]
       jmp       near ptr M00_L229
M00_L218:
       vucomisd  xmm1,qword ptr [7FF9778C88B0]
       jp        short M00_L219
       jne       short M00_L219
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80014014B0
       mov       rdi,[rcx]
       jmp       near ptr M00_L229
M00_L219:
       vucomisd  xmm1,qword ptr [7FF9778C88B8]
       jp        short M00_L220
       jne       short M00_L220
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80014014B8
       mov       rdi,[rcx]
       jmp       near ptr M00_L229
M00_L220:
       vucomisd  xmm1,qword ptr [7FF9778C88C0]
       jp        short M00_L221
       jne       short M00_L221
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80014014C0
       mov       rdi,[rcx]
       jmp       near ptr M00_L229
M00_L221:
       vucomisd  xmm1,qword ptr [7FF9778C88C8]
       jp        short M00_L222
       jne       short M00_L222
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80014014C8
       mov       rdi,[rcx]
       jmp       near ptr M00_L229
M00_L222:
       vucomisd  xmm1,qword ptr [7FF9778C88D0]
       jp        short M00_L223
       jne       short M00_L223
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80014014D0
       mov       rdi,[rcx]
       jmp       near ptr M00_L229
M00_L223:
       vucomisd  xmm1,qword ptr [7FF9778C88D8]
       jp        short M00_L224
       jne       short M00_L224
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80014014D8
       mov       rdi,[rcx]
       jmp       near ptr M00_L229
M00_L224:
       vucomisd  xmm1,qword ptr [7FF9778C88E0]
       jp        short M00_L225
       jne       short M00_L225
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80014014E0
       mov       rdi,[rcx]
       jmp       near ptr M00_L229
M00_L225:
       vucomisd  xmm1,qword ptr [7FF9778C88E8]
       jp        short M00_L226
       jne       short M00_L226
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80014014E8
       mov       rdi,[rcx]
       jmp       near ptr M00_L229
M00_L226:
       vucomisd  xmm1,qword ptr [7FF9778C88F0]
       jp        short M00_L227
       jne       short M00_L227
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80014014F0
       mov       rdi,[rcx]
       jmp       short M00_L229
M00_L227:
       vmovsd    qword ptr [rsp+90],xmm1
       vucomisd  xmm1,qword ptr [7FF9778C88F8]
       jp        short M00_L228
       jne       short M00_L228
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977845740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80014014A0
       mov       rdi,[rcx]
       jmp       short M00_L229
M00_L228:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      CORINFO_HELP_NEWSFAST
       mov       rdi,rax
       mov       rcx,rdi
       vmovsd    xmm1,qword ptr [rsp+90]
       call      qword ptr [7FF977C4CAC8]
M00_L229:
       jmp       near ptr M00_L07
M00_L230:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.Int64Value
       call      CORINFO_HELP_NEWSFAST
       mov       rdi,rax
       mov       rcx,rdi
       mov       rdx,[rsp+0A0]
       call      qword ptr [7FF977C45E00]
       jmp       near ptr M00_L07
M00_L231:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.UInt64Value
       call      CORINFO_HELP_NEWSFAST
       mov       rdi,rax
       mov       rcx,rdi
       mov       rdx,[rsp+0A0]
       call      qword ptr [7FF977C45E30]
       jmp       near ptr M00_L07
M00_L232:
       mov       rdx,[rsp+98]
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       short M00_L233
       xor       edi,edi
       jmp       near ptr M00_L07
M00_L233:
       cmp       dword ptr [rbp+8],0
       jne       short M00_L234
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001401490
       mov       rdi,[rcx]
       jmp       short M00_L235
M00_L234:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      CORINFO_HELP_NEWSFAST
       mov       rdi,rax
       mov       rcx,rdi
       mov       rdx,rbp
       call      qword ptr [7FF977C4C138]
M00_L235:
       jmp       near ptr M00_L07
M00_L236:
       mov       rcx,[rdi+8]
       test      rcx,rcx
       je        short M00_L239
       jmp       short M00_L238
M00_L237:
       mov       rcx,[rdi+18]
       cmp       [rcx+8],r8d
       jle       near ptr M00_L12
       mov       rcx,[rdi+18]
       cmp       r8d,[rcx+8]
       jae       near ptr M00_L247
       mov       edx,r8d
       mov       rcx,[rcx+rdx*8+10]
       jmp       near ptr M00_L13
M00_L238:
       lea       r8,[rsp+78]
       mov       rdx,rbp
       call      qword ptr [7FF977C45ED8]; AuroraScript.Runtime.Types.ScriptObject.TryResolveProperty(System.String, AuroraScript.Runtime.Property.PropertyDescriptor ByRef)
       test      eax,eax
       je        near ptr M00_L16
       jmp       near ptr M00_L14
M00_L239:
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rsp+78],xmm0
       vmovdqu   xmmword ptr [rsp+80],xmm0
       jmp       near ptr M00_L16
M00_L240:
       mov       rdx,[rdx+10]
       test      rdx,rdx
       je        near ptr M00_L15
       mov       r9,[rsp+78]
       mov       rcx,[r9+10]
       cmp       [rcx],cl
       mov       [rsp+28],r14
       mov       [rsp+30],r15
       lea       r9,[rsp+28]
       lea       rdx,[rsp+68]
       xor       r8d,r8d
       call      qword ptr [7FF977C4C168]
       jmp       near ptr M00_L27
M00_L241:
       mov       rax,[rax+40]
       test      rax,rax
       je        near ptr M00_L16
       mov       [rsp+28],r14
       mov       [rsp+30],r15
       lea       r9,[rsp+28]
       mov       r8,rdi
       xor       edx,edx
       mov       rcx,[rax+8]
       call      qword ptr [rax+18]
       jmp       near ptr M00_L27
M00_L242:
       call      qword ptr [7FF977A1E8E0]
       jmp       near ptr M00_L21
M00_L243:
       xor       ecx,ecx
       mov       [rsp+40],rcx
       jmp       near ptr M00_L25
M00_L244:
       call      qword ptr [7FF977A1E910]
       jmp       near ptr M00_L25
M00_L245:
       mov       rcx,rdi
       call      qword ptr [7FF977A1E940]; AuroraScript.Runtime.Types.ScriptObject.Resize()
       jmp       near ptr M00_L26
M00_L246:
       mov       [rsp+28],r14
       mov       [rsp+30],r15
       lea       r9,[rsp+28]
       mov       rcx,rdi
       xor       edx,edx
       mov       rax,[rdi]
       mov       rax,[rax+50]
       call      qword ptr [rax+20]
       jmp       near ptr M00_L27
M00_L247:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 7402
```
```assembly
; System.Number.<UInt32ToDecStrForKnownSmallNumber>g__CreateAndCacheString|50_0(UInt32)
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,20
       mov       ebx,ecx
       call      qword ptr [7FF96E839700]
       mov       rsi,[rax]
       mov       ecx,ebx
       call      qword ptr [7FF96E84D418]; Precode of System.Number.UInt32ToDecStr_NoSmallNumberCheck(UInt32)
       mov       rdi,rax
       cmp       ebx,[rsi+8]
       jae       short M01_L00
       mov       ecx,ebx
       lea       rcx,[rsi+rcx*8+10]
       mov       rdx,rdi
       call      qword ptr [7FF96E838FE8]; CORINFO_HELP_ASSIGN_REF
       mov       rax,rdi
       add       rsp,20
       pop       rbx
       pop       rsi
       pop       rdi
       ret
M01_L00:
       call      qword ptr [7FF96E838FD8]
       int       3
; Total bytes of code 68
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
       cmp       [rdx],rcx
       je        short M03_L02
       mov       rax,[rdx]
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
       mov       r8,8001400068
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
       call      qword ptr [7FF97784C330]; System.SpanHelpers.SequenceEqual(Byte ByRef, Byte ByRef, UIntPtr)
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
       call      qword ptr [7FF977AE0BB0]; Precode of System.Collections.Generic.Dictionary`2[[AuroraScript.Runtime.Property.TransitionKey, AuroraScript],[System.__Canon, System.Private.CoreLib]].TryGetValue(AuroraScript.Runtime.Property.TransitionKey, System.__Canon ByRef)
       test      eax,eax
       je        short M04_L06
       mov       rcx,[rsp+90]
       mov       rdx,rbp
       mov       r8,r14
       cmp       [rcx],ecx
       call      qword ptr [7FF977A1E838]; AuroraScript.Runtime.Property.HiddenClass.TryGet(System.String, AuroraScript.Runtime.Property.PropertyMeta ByRef)
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
       call      qword ptr [7FF977BC54E8]; System.Object.MemberwiseClone()
       mov       r15,rax
       mov       r8,[r14]
       lea       rcx,[rsp+48]
       mov       rdx,rbp
       call      qword ptr [7FF977A1EA18]; AuroraScript.Runtime.Property.HiddenProperty..ctor(System.String, AuroraScript.Runtime.Property.PropertyMeta)
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
       call      qword ptr [7FF97784F588]; System.Array.Copy(System.Array, System.Array, Int32)
M04_L08:
       mov       edi,[r15+8]
       dec       edi
       mov       r8,[r14]
       lea       rcx,[rsp+68]
       mov       rdx,rbp
       call      qword ptr [7FF977A1EA18]; AuroraScript.Runtime.Property.HiddenProperty..ctor(System.String, AuroraScript.Runtime.Property.PropertyMeta)
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
       call      qword ptr [7FF977A1EA30]; AuroraScript.Runtime.Property.HiddenClass..ctor(AuroraScript.Runtime.Property.HiddenProperty[], UInt16, AuroraScript.Runtime.Property.HiddenClass, System.String, Boolean)
       cmp       qword ptr [rbx+10],0
       jne       short M04_L10
       mov       rcx,offset MT_System.Collections.Generic.Dictionary<AuroraScript.Runtime.Property.TransitionKey, AuroraScript.Runtime.Property.HiddenClass>
       call      CORINFO_HELP_NEWSFAST
       mov       rdi,rax
       mov       rcx,rdi
       mov       edx,4
       xor       r8d,r8d
       call      qword ptr [7FF977A1EAD8]; System.Collections.Generic.Dictionary`2[[AuroraScript.Runtime.Property.TransitionKey, AuroraScript],[System.__Canon, System.Private.CoreLib]]..ctor(Int32, System.Collections.Generic.IEqualityComparer`1<AuroraScript.Runtime.Property.TransitionKey>)
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
       call      qword ptr [7FF977A1EBE0]; System.Collections.Generic.Dictionary`2[[AuroraScript.Runtime.Property.TransitionKey, AuroraScript],[System.__Canon, System.Private.CoreLib]].TryInsert(AuroraScript.Runtime.Property.TransitionKey, System.__Canon, System.Collections.Generic.InsertionBehavior)
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
       jmp       qword ptr [7FF977845C38]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBaseSlow(System.Runtime.CompilerServices.MethodTable*)
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
       jmp       qword ptr [7FF977BC5470]; System.Runtime.CompilerServices.CastHelpers.IsInstance_Helper(Void*, System.Object)
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
       mov       rcx,8001401490
       mov       rax,[rcx]
       jmp       short M07_L05
M07_L04:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      CORINFO_HELP_NEWSFAST
       mov       rsi,rax
       mov       rcx,rsi
       mov       rdx,rbx
       call      qword ptr [7FF977C4C138]
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
       push      rsi
       push      rbx
       sub       rsp,28
       mov       rbx,rcx
       mov       rcx,[rbx+20]
       mov       rsi,[rcx-10]
       mov       rcx,rsi
       test      cl,1
       jne       short M08_L00
       mov       rcx,7FF977CC023C
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rax,rsi
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
M08_L00:
       mov       rcx,7FF977CC0238
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rcx,rbx
       add       rsp,28
       pop       rbx
       pop       rsi
       jmp       qword ptr [7FF977A1FD50]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBaseSlow(System.Runtime.CompilerServices.MethodTable*)
; Total bytes of code 80
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
       mov       r8,8001400068
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
       call      qword ptr [7FF97784C330]; System.SpanHelpers.SequenceEqual(Byte ByRef, Byte ByRef, UIntPtr)
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
       call      qword ptr [7FF977A1ED90]; System.Array.Resize[[AuroraScript.Runtime.ScriptDatum, AuroraScript]](AuroraScript.Runtime.ScriptDatum[] ByRef, Int32)
       mov       rax,[rbp+10]
       cmp       qword ptr [rax+18],0
       je        short M10_L02
       mov       rax,[rbp+10]
       cmp       [rax],al
       mov       rax,[rbp+10]
       lea       rdx,[rax+18]
       mov       r8d,[rbp-4]
       mov       rcx,7FF977AE3990
       call      qword ptr [7FF977A1C600]; System.Array.Resize[[System.__Canon, System.Private.CoreLib]](System.__Canon[] ByRef, Int32)
M10_L02:
       nop
       add       rsp,30
       pop       rbp
       ret
; Total bytes of code 142
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
       js        near ptr M11_L04
       mov       r8,8001400068
       mov       r14,[r8]
M11_L00:
       mov       r8,[rbx+8]
       cmp       ebp,[r8+8]
       jae       near ptr M11_L06
       mov       edx,ebp
       shl       rdx,5
       mov       r8,[r8+rdx+10]
       cmp       r8,rsi
       je        short M11_L01
       test      r8,r8
       je        short M11_L03
       test      rsi,rsi
       je        short M11_L03
       mov       edx,[r8+8]
       cmp       edx,[rsi+8]
       jne       short M11_L03
       lea       rcx,[r8+0C]
       mov       r8d,[r8+8]
       add       r8d,r8d
       lea       rdx,[rsi+0C]
       call      qword ptr [7FF97784C330]; System.SpanHelpers.SequenceEqual(Byte ByRef, Byte ByRef, UIntPtr)
       test      eax,eax
       je        short M11_L03
M11_L01:
       mov       eax,ebp
M11_L02:
       test      eax,eax
       jl        short M11_L05
       mov       rcx,[rbx+8]
       cmp       eax,[rcx+8]
       jae       short M11_L06
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
M11_L03:
       dec       ebp
       jns       short M11_L00
M11_L04:
       mov       eax,0FFFFFFFF
       jmp       short M11_L02
M11_L05:
       xor       eax,eax
       mov       [rdi],rax
       add       rsp,20
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       ret
M11_L06:
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
       je        short M12_L00
       mov       r10d,[rcx+8]
       movzx     r8d,word ptr [r8]
       imul      r8,r10
       add       rax,r8
M12_L00:
       mov       rbx,[rbp+10]
       cmp       [rbx],bl
       add       rbx,8
       mov       rcx,[rbp-40]
       cmp       [rcx],cl
       add       rcx,8
       test      dword ptr [rdx],1000000
       jne       short M12_L02
       mov       rdx,rbx
       mov       r8,rax
       call      qword ptr [7FF96E84D928]; Precode of System.SpanHelpers.Memmove(Byte ByRef, Byte ByRef, UIntPtr)
M12_L01:
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
M12_L02:
       cmp       rax,4000
       jbe       short M12_L03
       mov       rdx,rbx
       mov       r8,rax
       call      qword ptr [7FF96E84A668]
       jmp       short M12_L01
M12_L03:
       mov       rdx,rbx
       mov       r8,rax
       call      qword ptr [7FF96E84A630]
       mov       rax,[System.Reflection.CustomAttributeExtensions.GetCustomAttribute[[System.__Canon, System.Private.CoreLib]](System.Reflection.Assembly)]
       cmp       dword ptr [rax],0
       je        short M12_L01
       call      qword ptr [7FF96E839040]; CORINFO_HELP_POLL_GC
       jmp       short M12_L01
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
       call      qword ptr [7FF977A1EA78]; AuroraScript.Runtime.ScriptDatum.FromString(System.String)
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
       jne       short M14_L01
       xor       ebp,ebp
M14_L00:
       test      rbx,rbx
       jne       short M14_L03
       xor       eax,eax
       jmp       short M14_L04
M14_L01:
       test      rbx,rbx
       je        short M14_L02
       mov       r14,[rsi]
       cmp       r14,[rbx]
       jne       short M14_L02
       cmp       dword ptr [r14+4],18
       jne       short M14_L02
       cmp       edi,[rsi+8]
       ja        short M14_L02
       cmp       edi,[rbx+8]
       jbe       short M14_L06
M14_L02:
       mov       rcx,rsi
       xor       edx,edx
       call      qword ptr [7FF96E84A390]; Precode of System.Array.GetLowerBound(Int32)
       mov       ebp,eax
       jmp       short M14_L00
M14_L03:
       mov       rcx,rbx
       xor       edx,edx
       call      qword ptr [7FF96E84A390]; Precode of System.Array.GetLowerBound(Int32)
M14_L04:
       mov       [rsp+20],edi
       xor       ecx,ecx
       mov       [rsp+28],ecx
       mov       rcx,rsi
       mov       edx,ebp
       mov       r8,rbx
       mov       r9d,eax
       call      qword ptr [7FF96E84A328]
M14_L05:
       nop
       add       rsp,30
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       ret
M14_L06:
       mov       r8d,edi
       movzx     ecx,word ptr [r14]
       imul      r8,rcx
       lea       rdx,[rsi+10]
       lea       rcx,[rbx+10]
       test      dword ptr [r14],1000000
       je        short M14_L08
       cmp       r8,4000
       ja        short M14_L07
       call      qword ptr [7FF96E84A630]
       mov       rax,[System.Reflection.CustomAttributeExtensions.GetCustomAttribute[[System.__Canon, System.Private.CoreLib]](System.Reflection.Assembly)]
       cmp       dword ptr [rax],0
       je        short M14_L05
       call      qword ptr [7FF96E839040]; CORINFO_HELP_POLL_GC
       jmp       short M14_L05
M14_L07:
       lea       rax,[System.Reflection.CustomAttributeExtensions.GetCustomAttribute[[System.__Canon, System.Private.CoreLib]](System.Reflection.Assembly)]
       add       rsp,30
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       jmp       qword ptr [rax]
M14_L08:
       cmp       r8,19
       jne       short M14_L09
       movups    xmm0,[rdx]
       movups    xmm1,[rdx+9]
       movups    [rcx],xmm0
       movups    [rcx+9],xmm1
       jmp       short M14_L05
M14_L09:
       call      qword ptr [7FF96E84D928]; Precode of System.SpanHelpers.Memmove(Byte ByRef, Byte ByRef, UIntPtr)
       jmp       short M14_L05
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
       call      qword ptr [7FF97784CB10]; System.Object..ctor()
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
       call      qword ptr [7FF977A1EAA8]; AuroraScript.Runtime.Property.HiddenClass.CreateEnumerableKeys(AuroraScript.Runtime.Property.HiddenProperty[])
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
       call      qword ptr [7FF97784CB10]; System.Object..ctor()
       cmp       dword ptr [rbp+18],0
       jge       short M16_L00
       mov       ecx,16
       call      qword ptr [7FF97784F348]
M16_L00:
       cmp       dword ptr [rbp+18],0
       jle       short M16_L01
       mov       rcx,[rbp+10]
       mov       edx,[rbp+18]
       call      qword ptr [7FF977A1EAF0]; System.Collections.Generic.Dictionary`2[[AuroraScript.Runtime.Property.TransitionKey, AuroraScript],[System.__Canon, System.Private.CoreLib]].Initialize(Int32)
M16_L01:
       cmp       qword ptr [rbp+20],0
       je        short M16_L02
       call      qword ptr [7FF977A1EB08]; System.Collections.Generic.EqualityComparer`1[[AuroraScript.Runtime.Property.TransitionKey, AuroraScript]].get_Default()
       cmp       rax,[rbp+20]
       je        short M16_L02
       mov       rax,[rbp+10]
       lea       rcx,[rax+18]
       mov       rdx,[rbp+20]
       call      CORINFO_HELP_ASSIGN_REF
M16_L02:
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
M17_L00:
       vmovdqa   xmmword ptr [rbp+rax-40],xmm4
       vmovdqa   xmmword ptr [rbp+rax-30],xmm4
       vmovdqa   xmmword ptr [rbp+rax-20],xmm4
       add       rax,30
       jne       short M17_L00
       mov       [rbp-40],rcx
       mov       [rbp+10],rcx
       mov       [rbp+18],rdx
       mov       [rbp+20],r8
       mov       [rbp+28],r9d
       mov       dword ptr [rbp-98],3E8
       mov       rax,[rbp+10]
       cmp       qword ptr [rax+8],0
       jne       short M17_L01
       mov       rcx,7FF977AE2880
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rcx,[rbp+10]
       xor       edx,edx
       call      qword ptr [7FF977A1EAF0]; System.Collections.Generic.Dictionary`2[[AuroraScript.Runtime.Property.TransitionKey, AuroraScript],[System.__Canon, System.Private.CoreLib]].Initialize(Int32)
M17_L01:
       mov       rax,[rbp+10]
       mov       rax,[rax+10]
       mov       [rbp-48],rax
       mov       rax,[rbp+10]
       mov       rax,[rax+18]
       mov       [rbp-50],rax
       cmp       qword ptr [rbp-50],0
       je        short M17_L02
       mov       rcx,7FF977AE2884
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rax,[rbp-50]
       mov       [rbp-80],rax
       mov       rcx,[rbp-80]
       mov       rdx,7FF977AE2888
       call      CORINFO_HELP_CLASSPROFILE32
       mov       rax,[rbp-80]
       mov       [rbp-0B0],rax
       mov       rax,[rbp+18]
       vmovdqu   xmm0,xmmword ptr [rax]
       vmovdqu   xmmword ptr [rbp-0A8],xmm0
       mov       rcx,[rbp-0B0]
       lea       rdx,[rbp-0A8]
       mov       r11,7FF977790168
       call      qword ptr [r11]
       mov       [rbp-70],eax
       jmp       short M17_L03
M17_L02:
       mov       rcx,7FF977AE2990
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rcx,[rbp+18]
       call      qword ptr [7FF977A1EC10]; AuroraScript.Runtime.Property.TransitionKey.GetHashCode()
       mov       [rbp-70],eax
M17_L03:
       mov       eax,[rbp-70]
       mov       [rbp-54],eax
       xor       eax,eax
       mov       [rbp-58],eax
       mov       rcx,[rbp+10]
       mov       edx,[rbp-54]
       call      qword ptr [7FF977A1EC28]; System.Collections.Generic.Dictionary`2[[AuroraScript.Runtime.Property.TransitionKey, AuroraScript],[System.__Canon, System.Private.CoreLib]].GetBucket(UInt32)
       mov       [rbp-60],rax
       mov       rax,[rbp-60]
       mov       eax,[rax]
       dec       eax
       mov       [rbp-64],eax
       cmp       qword ptr [rbp-50],0
       jne       short M17_L04
       jmp       near ptr M17_L10
M17_L04:
       mov       rcx,7FF977AE2994
       call      CORINFO_HELP_COUNTPROFILE32
       jmp       near ptr M17_L19
M17_L05:
       mov       rax,[rbp-48]
       mov       ecx,[rbp-64]
       cmp       ecx,[rax+8]
       jae       near ptr M17_L27
       mov       edx,ecx
       imul      rdx,20
       lea       rax,[rax+rdx+10]
       mov       eax,[rax+8]
       cmp       eax,[rbp-54]
       jne       near ptr M17_L09
       call      qword ptr [7FF977A1EB08]; System.Collections.Generic.EqualityComparer`1[[AuroraScript.Runtime.Property.TransitionKey, AuroraScript]].get_Default()
       mov       [rbp-88],rax
       mov       rcx,[rbp-88]
       mov       rdx,7FF977AE2998
       call      CORINFO_HELP_CLASSPROFILE32
       mov       rax,[rbp-88]
       mov       [rbp-0D8],rax
       mov       rax,[rbp-48]
       mov       ecx,[rbp-64]
       cmp       ecx,[rax+8]
       jae       near ptr M17_L27
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
       je        short M17_L06
       movzx     eax,byte ptr [rbp+28]
       cmp       eax,1
       jne       short M17_L07
       mov       rcx,7FF977AE2AA0
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rax,[rbp-48]
       mov       ecx,[rbp-64]
       cmp       ecx,[rax+8]
       jae       near ptr M17_L27
       mov       edx,ecx
       imul      rdx,20
       lea       rax,[rax+rdx+10]
       mov       rdx,[rbp+20]
       mov       rcx,rax
       call      CORINFO_HELP_CHECKED_ASSIGN_REF
       jmp       near ptr M17_L26
M17_L06:
       mov       rcx,7FF977AE2AA4
       call      CORINFO_HELP_COUNTPROFILE32
       jmp       short M17_L09
M17_L07:
       movzx     eax,byte ptr [rbp+28]
       cmp       eax,2
       jne       short M17_L08
       mov       rcx,7FF977AE2AA8
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rax,[rbp+18]
       vmovdqu   xmm0,xmmword ptr [rax]
       vmovdqu   xmmword ptr [rbp-0E8],xmm0
       lea       rcx,[rbp-0E8]
       call      qword ptr [7FF977A1EBF8]
M17_L08:
       mov       rcx,7FF977AE2AAC
       call      CORINFO_HELP_COUNTPROFILE32
       jmp       near ptr M17_L17
M17_L09:
       mov       rax,[rbp-48]
       mov       ecx,[rbp-64]
       cmp       ecx,[rax+8]
       jae       near ptr M17_L27
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
       jae       short M17_L12
       mov       rcx,7FF977AE2AB0
       call      CORINFO_HELP_COUNTPROFILE32
       call      qword ptr [7FF97784F480]
M17_L10:
       mov       eax,[rbp-98]
       dec       eax
       mov       [rbp-98],eax
       cmp       dword ptr [rbp-98],0
       jg        short M17_L11
       lea       rcx,[rbp-98]
       mov       edx,0ED
       call      CORINFO_HELP_PATCHPOINT
M17_L11:
       mov       rax,[rbp-48]
       mov       eax,[rax+8]
       cmp       eax,[rbp-64]
       ja        near ptr M17_L05
       mov       rcx,7FF977AE2AB4
       call      CORINFO_HELP_COUNTPROFILE32
       jmp       near ptr M17_L21
M17_L12:
       mov       rcx,7FF977AE2AB8
       call      CORINFO_HELP_COUNTPROFILE32
       jmp       short M17_L10
M17_L13:
       mov       rax,[rbp-48]
       mov       ecx,[rbp-64]
       cmp       ecx,[rax+8]
       jae       near ptr M17_L27
       mov       edx,ecx
       imul      rdx,20
       lea       rax,[rax+rdx+10]
       mov       eax,[rax+8]
       cmp       eax,[rbp-54]
       jne       near ptr M17_L18
       mov       rax,[rbp-50]
       mov       [rbp-90],rax
       mov       rcx,[rbp-90]
       mov       rdx,7FF977AE2AC0
       call      CORINFO_HELP_CLASSPROFILE32
       mov       rax,[rbp-90]
       mov       [rbp-110],rax
       mov       rax,[rbp-48]
       mov       ecx,[rbp-64]
       cmp       ecx,[rax+8]
       jae       near ptr M17_L27
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
       mov       r11,7FF977790160
       call      qword ptr [r11]
       test      eax,eax
       je        short M17_L14
       movzx     eax,byte ptr [rbp+28]
       cmp       eax,1
       jne       short M17_L15
       mov       rcx,7FF977AE2BC8
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rax,[rbp-48]
       mov       ecx,[rbp-64]
       cmp       ecx,[rax+8]
       jae       near ptr M17_L27
       mov       edx,ecx
       imul      rdx,20
       lea       rax,[rax+rdx+10]
       mov       rdx,[rbp+20]
       mov       rcx,rax
       call      CORINFO_HELP_CHECKED_ASSIGN_REF
       jmp       near ptr M17_L26
M17_L14:
       mov       rcx,7FF977AE2BCC
       call      CORINFO_HELP_COUNTPROFILE32
       jmp       short M17_L18
M17_L15:
       movzx     eax,byte ptr [rbp+28]
       cmp       eax,2
       jne       short M17_L16
       mov       rcx,7FF977AE2BD0
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rax,[rbp+18]
       vmovdqu   xmm0,xmmword ptr [rax]
       vmovdqu   xmmword ptr [rbp-120],xmm0
       lea       rcx,[rbp-120]
       call      qword ptr [7FF977A1EBF8]
M17_L16:
       mov       rcx,7FF977AE2BD4
       call      CORINFO_HELP_COUNTPROFILE32
M17_L17:
       xor       eax,eax
       add       rsp,130
       pop       rsi
       pop       rdi
       pop       rbp
       ret
M17_L18:
       mov       rax,[rbp-48]
       mov       ecx,[rbp-64]
       cmp       ecx,[rax+8]
       jae       near ptr M17_L27
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
       jae       near ptr M17_L22
       mov       rcx,7FF977AE2BD8
       call      CORINFO_HELP_COUNTPROFILE32
       call      qword ptr [7FF97784F480]
M17_L19:
       mov       eax,[rbp-98]
       dec       eax
       mov       [rbp-98],eax
       cmp       dword ptr [rbp-98],0
       jg        short M17_L20
       lea       rcx,[rbp-98]
       mov       edx,15A
       call      CORINFO_HELP_PATCHPOINT
M17_L20:
       mov       rax,[rbp-48]
       mov       eax,[rax+8]
       cmp       eax,[rbp-64]
       ja        near ptr M17_L13
M17_L21:
       mov       rax,[rbp+10]
       cmp       dword ptr [rax+40],0
       jle       short M17_L23
       mov       rax,[rbp+10]
       mov       eax,[rax+3C]
       mov       [rbp-68],eax
       mov       rax,[rbp+10]
       mov       eax,[rax+3C]
       mov       rcx,[rbp-48]
       cmp       eax,[rcx+8]
       jae       near ptr M17_L27
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
       jmp       near ptr M17_L25
M17_L22:
       mov       rcx,7FF977AE2BDC
       call      CORINFO_HELP_COUNTPROFILE32
       jmp       near ptr M17_L19
M17_L23:
       mov       rax,[rbp+10]
       mov       eax,[rax+38]
       mov       [rbp-6C],eax
       mov       rax,[rbp-48]
       mov       eax,[rax+8]
       cmp       eax,[rbp-6C]
       jne       short M17_L24
       mov       rcx,7FF977AE2BE0
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rcx,[rbp+10]
       call      qword ptr [7FF977A1EC58]
       mov       rcx,[rbp+10]
       mov       edx,[rbp-54]
       call      qword ptr [7FF977A1EC28]; System.Collections.Generic.Dictionary`2[[AuroraScript.Runtime.Property.TransitionKey, AuroraScript],[System.__Canon, System.Private.CoreLib]].GetBucket(UInt32)
       mov       [rbp-60],rax
M17_L24:
       mov       rcx,7FF977AE2BE4
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
M17_L25:
       mov       rax,[rbp-48]
       mov       ecx,[rbp-68]
       cmp       ecx,[rax+8]
       jae       short M17_L27
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
       mov       rcx,7FF977AE2BE8
       call      CORINFO_HELP_COUNTPROFILE32
M17_L26:
       mov       eax,1
       add       rsp,130
       pop       rsi
       pop       rdi
       pop       rbp
       ret
M17_L27:
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
M19_L00:
       lea       r10d,[r8+1]
       movsxd    r10,r10d
       lea       r10,[r10+r10*2]
       lea       r10,[rax+r10*8]
       mov       r9d,[r10]
       mov       r11,[r10+8]
       and       r9d,0FFFFFFFE
       cmp       r11,rdx
       jne       short M19_L02
       mov       r11,rbx
       xor       r11,[r10+10]
       cmp       r11,1
       ja        short M19_L02
       cmp       r9d,[r10]
       jne       short M19_L04
M19_L01:
       cmp       r11d,1
       jne       short M19_L03
       mov       rax,rsi
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
M19_L02:
       test      r9d,r9d
       je        short M19_L04
       inc       ecx
       add       r8d,ecx
       and       r8d,[rax+4]
       cmp       ecx,8
       jl        short M19_L00
       jmp       short M19_L04
M19_L03:
       test      r11d,r11d
       jne       short M19_L05
       xor       eax,eax
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
M19_L04:
       mov       r11d,2
       jmp       short M19_L01
M19_L05:
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
       jge       short M21_L00
       mov       ecx,45
       mov       edx,0D
       call      qword ptr [7FF977A1EE20]
M21_L00:
       mov       rax,[rbp+10]
       mov       rax,[rax]
       mov       [rbp-8],rax
       cmp       qword ptr [rbp-8],0
       jne       short M21_L01
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
M21_L01:
       mov       rax,[rbp-8]
       mov       eax,[rax+8]
       cmp       eax,[rbp+18]
       je        short M21_L02
       movsxd    rdx,dword ptr [rbp+18]
       mov       rcx,offset MT_AuroraScript.Runtime.ScriptDatum[]
       call      CORINFO_HELP_NEWARR_1_VC
       mov       [rbp-10],rax
       mov       rcx,[rbp-10]
       call      qword ptr [7FF977A1EDC0]; System.Runtime.InteropServices.MemoryMarshal.GetArrayDataReference[[AuroraScript.Runtime.ScriptDatum, AuroraScript]](AuroraScript.Runtime.ScriptDatum[])
       mov       [rbp-18],rax
       mov       rcx,[rbp-8]
       call      qword ptr [7FF977A1EDC0]; System.Runtime.InteropServices.MemoryMarshal.GetArrayDataReference[[AuroraScript.Runtime.ScriptDatum, AuroraScript]](AuroraScript.Runtime.ScriptDatum[])
       mov       [rbp-20],rax
       mov       rax,[rbp-8]
       mov       edx,[rax+8]
       mov       ecx,[rbp+18]
       call      qword ptr [7FF977A1EE38]; System.Math.Min(Int32, Int32)
       mov       eax,eax
       mov       [rbp-28],rax
       mov       r8,[rbp-28]
       mov       rdx,[rbp-20]
       mov       rcx,[rbp-18]
       call      qword ptr [7FF977A1EDF0]; System.Buffer.Memmove[[AuroraScript.Runtime.ScriptDatum, AuroraScript]](AuroraScript.Runtime.ScriptDatum ByRef, AuroraScript.Runtime.ScriptDatum ByRef, UIntPtr)
       mov       rcx,[rbp+10]
       mov       rdx,[rbp-10]
       call      CORINFO_HELP_CHECKED_ASSIGN_REF
M21_L02:
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
       jl        short M22_L03
       mov       rdi,[rsi]
       test      rdi,rdi
       je        short M22_L04
       mov       ebp,[rdi+8]
       cmp       ebp,ebx
       je        short M22_L02
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
       jbe       short M22_L00
       call      qword ptr [7FF96E84A668]
       jmp       short M22_L01
M22_L00:
       call      qword ptr [7FF96E84A630]
       mov       rax,[System.Reflection.CustomAttributeExtensions.GetCustomAttribute[[System.__Canon, System.Private.CoreLib]](System.Reflection.Assembly)]
       cmp       dword ptr [rax],0
       jne       short M22_L05
M22_L01:
       mov       rcx,rsi
       mov       rdx,r14
       call      qword ptr [7FF96E838FF0]; CORINFO_HELP_CHECKED_ASSIGN_REF
M22_L02:
       nop
       add       rsp,30
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       ret
M22_L03:
       mov       ecx,45
       mov       edx,0D
       call      qword ptr [7FF96E84F3E0]
       int       3
M22_L04:
       call      qword ptr [7FF96E83ED58]
       mov       rcx,rax
       mov       edx,ebx
       call      qword ptr [7FF96E839088]; CORINFO_HELP_NEWARR_1_DIRECT
       mov       rdx,rax
       mov       rcx,rsi
       call      qword ptr [7FF96E838FF0]; CORINFO_HELP_CHECKED_ASSIGN_REF
       jmp       short M22_L02
M22_L05:
       call      qword ptr [7FF96E839040]; CORINFO_HELP_POLL_GC
       jmp       short M22_L01
; Total bytes of code 195
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
; AuroraScript.Runtime.ScriptDatum.FromString(System.String)
       push      rbp
       sub       rsp,20
       lea       rbp,[rsp+20]
       mov       [rbp+10],rcx
       mov       [rbp+18],rdx
       mov       rcx,[rbp+10]
       mov       rdx,[rbp+18]
       call      qword ptr [7FF977A1EA90]; AuroraScript.Runtime.ScriptDatum.CreateString(System.String)
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
       jmp       short M26_L02
M26_L00:
       mov       rax,[rbp+10]
       mov       ecx,[rbp-50]
       cmp       ecx,[rax+8]
       jae       near ptr M26_L09
       mov       edx,ecx
       imul      rdx,20
       lea       rax,[rax+rdx+10]
       lea       rcx,[rax+8]
       call      qword ptr [7FF977A1EAC0]; AuroraScript.Runtime.Property.PropertyMeta.get_Enumerable()
       test      eax,eax
       je        short M26_L01
       mov       rcx,7FF977AE1D90
       call      CORINFO_HELP_COUNTPROFILE32
       mov       eax,[rbp-3C]
       inc       eax
       mov       [rbp-3C],eax
M26_L01:
       mov       rcx,7FF977AE1D94
       call      CORINFO_HELP_COUNTPROFILE32
       mov       eax,[rbp-50]
       inc       eax
       mov       [rbp-50],eax
M26_L02:
       mov       eax,[rbp-70]
       dec       eax
       mov       [rbp-70],eax
       cmp       dword ptr [rbp-70],0
       jg        short M26_L03
       lea       rcx,[rbp-70]
       mov       edx,21
       call      CORINFO_HELP_PATCHPOINT
M26_L03:
       mov       rax,[rbp+10]
       mov       eax,[rax+8]
       cmp       eax,[rbp-50]
       jg        short M26_L00
       cmp       dword ptr [rbp-3C],0
       jne       short M26_L04
       mov       rcx,7FF977AE1D98
       call      CORINFO_HELP_COUNTPROFILE32
       call      qword ptr [7FF977A1E100]; System.Array.Empty[[AuroraScript.Runtime.ScriptDatum, AuroraScript]]()
       nop
       add       rsp,80
       pop       rsi
       pop       rdi
       pop       rbp
       ret
M26_L04:
       movsxd    rdx,dword ptr [rbp-3C]
       mov       rcx,offset MT_AuroraScript.Runtime.ScriptDatum[]
       call      CORINFO_HELP_NEWARR_1_VC
       mov       [rbp-48],rax
       xor       eax,eax
       mov       [rbp-4C],eax
       xor       eax,eax
       mov       [rbp-54],eax
       jmp       near ptr M26_L07
M26_L05:
       mov       rax,[rbp+10]
       mov       ecx,[rbp-54]
       cmp       ecx,[rax+8]
       jae       near ptr M26_L09
       mov       edx,ecx
       imul      rdx,20
       lea       rax,[rax+rdx+10]
       lea       rcx,[rax+8]
       call      qword ptr [7FF977A1EAC0]; AuroraScript.Runtime.Property.PropertyMeta.get_Enumerable()
       test      eax,eax
       je        short M26_L06
       mov       rcx,7FF977AE1D9C
       call      CORINFO_HELP_COUNTPROFILE32
       mov       eax,[rbp-4C]
       mov       [rbp-58],eax
       mov       eax,[rbp-4C]
       inc       eax
       mov       [rbp-4C],eax
       mov       rax,[rbp+10]
       mov       ecx,[rbp-54]
       cmp       ecx,[rax+8]
       jae       near ptr M26_L09
       mov       edx,ecx
       imul      rdx,20
       lea       rax,[rax+rdx+10]
       vmovdqu   xmm0,xmmword ptr [rax+10]
       vmovdqu   xmmword ptr [rbp-68],xmm0
       mov       rax,[rbp-48]
       mov       ecx,[rbp-58]
       cmp       ecx,[rax+8]
       jae       short M26_L09
       mov       edx,ecx
       imul      rdx,10
       lea       rax,[rax+rdx+10]
       mov       rdi,rax
       lea       rsi,[rbp-68]
       call      CORINFO_HELP_ASSIGN_BYREF
       movsq
M26_L06:
       mov       rcx,7FF977AE1DA0
       call      CORINFO_HELP_COUNTPROFILE32
       mov       eax,[rbp-54]
       inc       eax
       mov       [rbp-54],eax
M26_L07:
       mov       eax,[rbp-70]
       dec       eax
       mov       [rbp-70],eax
       cmp       dword ptr [rbp-70],0
       jg        short M26_L08
       lea       rcx,[rbp-70]
       mov       edx,70
       call      CORINFO_HELP_PATCHPOINT
M26_L08:
       mov       rax,[rbp+10]
       mov       eax,[rax+8]
       cmp       eax,[rbp-54]
       jg        near ptr M26_L05
       mov       rcx,7FF977AE1DA4
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rax,[rbp-48]
       add       rsp,80
       pop       rsi
       pop       rdi
       pop       rbp
       ret
M26_L09:
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
       call      qword ptr [7FF977845A88]; System.Collections.HashHelpers.GetPrime(Int32)
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
       je        short M27_L00
       mov       rax,[rbp-38]
       mov       [rbp-28],rax
       jmp       short M27_L01
M27_L00:
       mov       rcx,[rbp-30]
       mov       rdx,7FF977AF07E0
       call      qword ptr [7FF97784F4B0]; System.Runtime.CompilerServices.GenericsHelpers.Class(IntPtr, IntPtr)
       mov       [rbp-28],rax
M27_L01:
       movsxd    rdx,dword ptr [rbp-0C]
       mov       rcx,[rbp-28]
       call      CORINFO_HELP_NEWARR_1_VC
       mov       [rbp-20],rax
       mov       rax,[rbp+10]
       mov       dword ptr [rax+3C],0FFFFFFFF
       mov       ecx,[rbp-0C]
       call      qword ptr [7FF97784F4C8]; System.Collections.HashHelpers.GetFastModMultiplier(UInt32)
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
       call      qword ptr [7FF977845728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rax,8001400468
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
       mov       rdx,7FF977AE3268
       call      qword ptr [7FF977A1ECA0]; System.HashCode.Add[[System.__Canon, System.Private.CoreLib]](System.__Canon)
       mov       rax,[rbp+10]
       movzx     edx,word ptr [rax+8]
       lea       rcx,[rbp-20]
       call      qword ptr [7FF977A1ECD0]; System.HashCode.Add[[AuroraScript.Runtime.Property.PropertyFlags, AuroraScript]](AuroraScript.Runtime.Property.PropertyFlags)
       lea       rcx,[rbp-20]
       call      qword ptr [7FF977A1ECE8]; System.HashCode.ToHashCode()
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
       call      qword ptr [7FF97784F540]; System.Collections.HashHelpers.FastMod(UInt32, UInt32, UInt64)
       mov       rcx,[rbp-8]
       cmp       eax,[rcx+8]
       jae       short M30_L00
       mov       edx,eax
       lea       rcx,[rcx+rdx*4+10]
       mov       rax,rcx
       add       rsp,30
       pop       rbp
       ret
M30_L00:
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
       call      qword ptr [7FF9778457A0]; System.Buffer.BulkMoveWithWriteBarrier(Byte ByRef, Byte ByRef, UIntPtr)
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
       jle       near ptr M00_L19
       mov       rdx,[rsi+38]
       test      rdx,rdx
       je        near ptr M00_L15
       lea       rcx,[rdx+10]
       xor       eax,eax
       mov       r8,[rdx]
       cmp       dword ptr [r8+4],18
       jne       near ptr M00_L16
M00_L00:
       mov       r10d,eax
       neg       r10d
       test      eax,eax
       jg        near ptr M00_L17
       test      r10d,r10d
       jl        near ptr M00_L17
       lea       eax,[r10+rdi]
       cmp       eax,[rdx+8]
       ja        near ptr M00_L17
       movzx     edx,word ptr [r8]
       mov       eax,r10d
       imul      rax,rdx
       add       rcx,rax
       mov       eax,edi
       imul      rdx,rax
       test      dword ptr [r8],1000000
       je        near ptr M00_L18
       shr       rdx,3
       call      qword ptr [7FF977C55DB8]; System.SpanHelpers.ClearWithReferences(IntPtr ByRef, UIntPtr)
M00_L01:
       xor       edx,edx
       mov       [rsi+34],edx
       mov       rbp,[rbx+38]
       mov       r14,[rbx+58]
       mov       r15,[rbx+60]
       test      r14,r14
       je        near ptr M00_L20
       mov       rdx,8001400450
       cmp       r14,[rdx]
       je        near ptr M00_L09
       mov       rdx,8001400458
       cmp       r14,[rdx]
       je        near ptr M00_L09
       movsx     rdx,r15w
M00_L02:
       cmp       edx,18
       jne       near ptr M00_L09
       mov       rsi,r14
       test      rsi,rsi
       je        short M00_L03
       mov       rdx,offset MT_AuroraScript.Runtime.Types.ScriptArray
       cmp       [rsi],rdx
       jne       near ptr M00_L23
M00_L03:
       test      rsi,rsi
       je        near ptr M00_L24
M00_L04:
       test      rsi,rsi
       je        near ptr M00_L09
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptArray
       cmp       [rsi],rcx
       jne       near ptr M00_L09
       mov       r14,[rsi+38]
       mov       esi,[rsi+34]
       test      r14,r14
       je        near ptr M00_L28
       cmp       [r14+8],esi
       jb        near ptr M00_L30
       add       r14,10
M00_L05:
       cmp       [rbp],bpl
       test      esi,esi
       je        short M00_L08
       mov       edi,esi
       add       edi,[rbp+34]
       jo        near ptr M00_L35
       mov       rcx,rbp
       mov       edx,edi
       call      qword ptr [7FF977C55DA0]; AuroraScript.Runtime.Types.ScriptArray.EnsureCapacity(Int32)
       mov       rax,[rbp+38]
       mov       ecx,[rbp+34]
       test      rax,rax
       je        near ptr M00_L29
       mov       edx,[rax+8]
       cmp       edx,ecx
       jb        near ptr M00_L30
       mov       r8d,ecx
       shl       r8,4
       lea       rax,[rax+r8+10]
       sub       edx,ecx
M00_L06:
       cmp       esi,edx
       jg        near ptr M00_L33
       mov       r8d,esi
       shl       r8,4
       cmp       r8,4000
       ja        near ptr M00_L32
       mov       rcx,rax
       mov       rdx,r14
       call      00007FF9D74435E0
       cmp       dword ptr [7FF9D7784A90],0
       jne       near ptr M00_L31
M00_L07:
       mov       [rbp+34],edi
M00_L08:
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
M00_L09:
       mov       rdx,r14
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptPackedArray
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r13,rax
       test      r13,r13
       je        short M00_L13
       xor       r14d,r14d
       mov       rcx,r13
       mov       rax,[r13]
       mov       r15,[rax+58]
       call      qword ptr [r15+8]
       test      eax,eax
       jle       short M00_L08
       jmp       short M00_L12
M00_L10:
       cmp       edx,[rax+8]
       jae       near ptr M00_L34
       mov       ecx,edx
       shl       rcx,4
       lea       rdi,[rax+rcx+10]
       lea       rsi,[rsp+30]
       call      CORINFO_HELP_ASSIGN_BYREF
       movsq
       inc       edx
       mov       [rbp+34],edx
M00_L11:
       inc       r14d
       mov       rcx,r13
       call      qword ptr [r15+8]
       cmp       eax,r14d
       jle       short M00_L08
M00_L12:
       lea       rdx,[rsp+30]
       mov       rcx,r13
       mov       r8d,r14d
       call      qword ptr [r15+10]
       mov       edx,[rbp+34]
       mov       rax,[rbp+38]
       cmp       [rax+8],edx
       ja        short M00_L10
       lea       rdx,[rsp+30]
       mov       rcx,rbp
       call      qword ptr [7FF977C5C240]
       jmp       short M00_L11
M00_L13:
       mov       esi,[rbp+34]
       mov       rcx,[rbp+38]
       cmp       [rcx+8],esi
       jbe       short M00_L14
       mov       rcx,[rbp+38]
       cmp       esi,[rcx+8]
       jae       near ptr M00_L34
       mov       edx,esi
       shl       rdx,4
       lea       rdi,[rcx+rdx+10]
       mov       rcx,rdi
       mov       rdx,r14
       call      CORINFO_HELP_ASSIGN_REF
       mov       [rdi+8],r15
       inc       esi
       mov       [rbp+34],esi
       jmp       near ptr M00_L08
M00_L14:
       mov       [rsp+20],r14
       mov       [rsp+28],r15
       lea       rdx,[rsp+20]
       mov       rcx,rbp
       call      qword ptr [7FF977C5C240]
       jmp       near ptr M00_L08
M00_L15:
       mov       ecx,2
       call      qword ptr [7FF97785FD38]
       int       3
M00_L16:
       mov       r10d,[r8+4]
       add       r10d,0FFFFFFE8
       shr       r10d,3
       movsxd    rax,r10d
       mov       eax,[rcx+rax*4]
       shl       r10d,3
       movsxd    r10,r10d
       add       rcx,r10
       jmp       near ptr M00_L00
M00_L17:
       call      qword ptr [7FF977C5C120]
       int       3
M00_L18:
       call      qword ptr [7FF9778557E8]; System.SpanHelpers.ClearWithoutReferences(Byte ByRef, UIntPtr)
       jmp       near ptr M00_L01
M00_L19:
       test      edi,edi
       jge       near ptr M00_L01
       mov       rcx,rsi
       xor       edx,edx
       call      qword ptr [7FF977C55DA0]; AuroraScript.Runtime.Types.ScriptArray.EnsureCapacity(Int32)
       mov       r8d,[rsi+34]
       neg       r8d
       mov       edx,[rsi+34]
       mov       rcx,[rsi+38]
       call      qword ptr [7FF977C55D88]; System.Array.Clear(System.Array, Int32, Int32)
       jmp       near ptr M00_L01
M00_L20:
       test      r15,r15
       je        short M00_L22
       lea       rdx,[r15-1]
       mov       ecx,1
       mov       eax,2
       cmp       rdx,1
       cmova     ecx,eax
       mov       edx,ecx
M00_L21:
       jmp       near ptr M00_L02
M00_L22:
       xor       edx,edx
       jmp       short M00_L21
M00_L23:
       mov       rdx,r14
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rsi,rax
       jmp       near ptr M00_L03
M00_L24:
       mov       rdx,r14
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       short M00_L25
       xor       esi,esi
       jmp       near ptr M00_L04
M00_L25:
       cmp       dword ptr [rdi+8],0
       jne       short M00_L26
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,80014014A0
       mov       rsi,[rcx]
       jmp       short M00_L27
M00_L26:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      CORINFO_HELP_NEWSFAST
       mov       rsi,rax
       mov       rcx,rsi
       mov       rdx,rdi
       call      qword ptr [7FF977C5C168]
M00_L27:
       jmp       near ptr M00_L04
M00_L28:
       test      esi,esi
       jne       short M00_L30
       xor       r14d,r14d
       xor       esi,esi
       jmp       near ptr M00_L05
M00_L29:
       test      ecx,ecx
       jne       short M00_L30
       xor       eax,eax
       xor       edx,edx
       jmp       near ptr M00_L06
M00_L30:
       call      qword ptr [7FF977A27D50]
       int       3
M00_L31:
       call      CORINFO_HELP_POLL_GC
       jmp       near ptr M00_L07
M00_L32:
       mov       rcx,rax
       mov       rdx,r14
       call      qword ptr [7FF977C5C0A8]
       jmp       near ptr M00_L07
M00_L33:
       call      qword ptr [7FF977C56040]
       int       3
M00_L34:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
M00_L35:
       call      CORINFO_HELP_OVERFLOW
       int       3
; Total bytes of code 1062
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
; AuroraScript.Runtime.Types.ScriptArray.EnsureCapacity(Int32)
       push      rsi
       push      rbx
       sub       rsp,28
       mov       rbx,rcx
       mov       rcx,[rbx+38]
       cmp       [rcx+8],edx
       jl        short M02_L01
M02_L00:
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
M02_L01:
       mov       rcx,[rbx+38]
       cmp       dword ptr [rcx+8],0
       je        short M02_L02
       mov       rcx,[rbx+38]
       mov       ecx,[rcx+8]
       add       ecx,ecx
       jmp       short M02_L03
M02_L02:
       mov       ecx,4
M02_L03:
       cmp       ecx,edx
       cmovl     ecx,edx
       movsxd    rdx,ecx
       mov       rcx,offset MT_AuroraScript.Runtime.ScriptDatum[]
       call      CORINFO_HELP_NEWARR_1_VC
       mov       rsi,rax
       cmp       dword ptr [rbx+34],0
       jle       short M02_L04
       mov       rcx,[rbx+38]
       mov       r8d,[rbx+34]
       mov       rdx,rsi
       call      qword ptr [7FF97785F588]; System.Array.Copy(System.Array, System.Array, Int32)
M02_L04:
       lea       rcx,[rbx+38]
       mov       rdx,rsi
       call      CORINFO_HELP_ASSIGN_REF
       jmp       short M02_L00
; Total bytes of code 114
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
; System.SpanHelpers.ClearWithoutReferences(Byte ByRef, UIntPtr)
       push      rbx
       test      rdx,rdx
       je        near ptr M04_L04
       lea       rbx,[rcx+rdx]
       cmp       rdx,10
       jbe       near ptr M04_L07
       cmp       rdx,40
       jbe       short M04_L02
       cmp       rdx,400
       ja        near ptr M04_L05
       cmp       rdx,100
       jb        short M04_L00
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
M04_L00:
       mov       rax,rdx
       shr       rax,6
M04_L01:
       xorps     xmm0,xmm0
       movups    [rcx],xmm0
       movups    [rcx+10],xmm0
       movups    [rcx+20],xmm0
       movups    [rcx+30],xmm0
       add       rcx,40
       dec       rax
       jne       short M04_L01
       and       rdx,3F
       cmp       rdx,10
       jbe       short M04_L03
M04_L02:
       xorps     xmm0,xmm0
       movups    [rcx],xmm0
       cmp       rdx,20
       jbe       short M04_L03
       xorps     xmm0,xmm0
       movups    [rcx+10],xmm0
       cmp       rdx,30
       ja        short M04_L06
M04_L03:
       xorps     xmm0,xmm0
       movups    [rbx-10],xmm0
M04_L04:
       pop       rbx
       ret
M04_L05:
       cmp       [rcx],cl
       lea       rax,[System.Reflection.CustomAttributeExtensions.GetCustomAttribute[[System.__Canon, System.Private.CoreLib]](System.Reflection.Assembly)]
       pop       rbx
       jmp       qword ptr [rax]
M04_L06:
       xorps     xmm0,xmm0
       movups    [rcx+20],xmm0
       jmp       short M04_L03
M04_L07:
       test      dl,18
       je        short M04_L08
       xor       edx,edx
       mov       [rcx],rdx
       mov       [rbx-8],rdx
       jmp       short M04_L04
M04_L08:
       test      dl,4
       je        short M04_L09
       xor       edx,edx
       mov       [rcx],edx
       mov       [rbx-4],edx
       jmp       short M04_L04
M04_L09:
       mov       byte ptr [rcx],0
       test      dl,2
       je        short M04_L04
       mov       word ptr [rbx-2],0
       jmp       short M04_L04
; Total bytes of code 236
```
```assembly
; System.Array.Clear(System.Array, Int32, Int32)
       sub       rsp,28
       test      rcx,rcx
       je        short M05_L01
       lea       rax,[rcx+10]
       xor       r10d,r10d
       mov       r9,[rcx]
       cmp       dword ptr [r9+4],18
       jne       short M05_L02
M05_L00:
       mov       r11d,edx
       sub       r11d,r10d
       cmp       edx,r10d
       jl        short M05_L03
       mov       edx,r11d
       or        edx,r8d
       jl        short M05_L03
       lea       edx,[r11+r8]
       cmp       edx,[rcx+8]
       ja        short M05_L03
       movzx     edx,word ptr [r9]
       mov       ecx,r11d
       imul      rcx,rdx
       add       rcx,rax
       mov       eax,r8d
       imul      rdx,rax
       test      dword ptr [r9],1000000
       je        short M05_L04
       shr       rdx,3
       add       rsp,28
       jmp       qword ptr [7FF977C55DB8]; System.SpanHelpers.ClearWithReferences(IntPtr ByRef, UIntPtr)
M05_L01:
       mov       ecx,2
       call      qword ptr [7FF97785FD38]
       int       3
M05_L02:
       mov       r11d,[r9+4]
       add       r11d,0FFFFFFE8
       shr       r11d,3
       movsxd    r10,r11d
       mov       r10d,[rax+r10*4]
       shl       r11d,3
       movsxd    r11,r11d
       add       rax,r11
       jmp       short M05_L00
M05_L03:
       call      qword ptr [7FF977C5C120]
       int       3
M05_L04:
       call      qword ptr [7FF9778557E8]; System.SpanHelpers.ClearWithoutReferences(Byte ByRef, UIntPtr)
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
       jne       short M06_L00
       ret
M06_L00:
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
       jne       short M07_L01
       xor       ebp,ebp
M07_L00:
       test      rbx,rbx
       jne       short M07_L03
       xor       eax,eax
       jmp       short M07_L04
M07_L01:
       test      rbx,rbx
       je        short M07_L02
       mov       r14,[rsi]
       cmp       r14,[rbx]
       jne       short M07_L02
       cmp       dword ptr [r14+4],18
       jne       short M07_L02
       cmp       edi,[rsi+8]
       ja        short M07_L02
       cmp       edi,[rbx+8]
       jbe       short M07_L06
M07_L02:
       mov       rcx,rsi
       xor       edx,edx
       call      qword ptr [7FF96E84A390]; Precode of System.Array.GetLowerBound(Int32)
       mov       ebp,eax
       jmp       short M07_L00
M07_L03:
       mov       rcx,rbx
       xor       edx,edx
       call      qword ptr [7FF96E84A390]; Precode of System.Array.GetLowerBound(Int32)
M07_L04:
       mov       [rsp+20],edi
       xor       ecx,ecx
       mov       [rsp+28],ecx
       mov       rcx,rsi
       mov       edx,ebp
       mov       r8,rbx
       mov       r9d,eax
       call      qword ptr [7FF96E84A328]
M07_L05:
       nop
       add       rsp,30
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       ret
M07_L06:
       mov       r8d,edi
       movzx     ecx,word ptr [r14]
       imul      r8,rcx
       lea       rdx,[rsi+10]
       lea       rcx,[rbx+10]
       test      dword ptr [r14],1000000
       je        short M07_L08
       cmp       r8,4000
       ja        short M07_L07
       call      qword ptr [7FF96E84A630]
       mov       rax,[System.Reflection.CustomAttributeExtensions.GetCustomAttribute[[System.__Canon, System.Private.CoreLib]](System.Reflection.Assembly)]
       cmp       dword ptr [rax],0
       je        short M07_L05
       call      qword ptr [7FF96E839040]; CORINFO_HELP_POLL_GC
       jmp       short M07_L05
M07_L07:
       lea       rax,[System.Reflection.CustomAttributeExtensions.GetCustomAttribute[[System.__Canon, System.Private.CoreLib]](System.Reflection.Assembly)]
       add       rsp,30
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       jmp       qword ptr [rax]
M07_L08:
       cmp       r8,19
       jne       short M07_L09
       movups    xmm0,[rdx]
       movups    xmm1,[rdx+9]
       movups    [rcx],xmm0
       movups    [rcx+9],xmm1
       jmp       short M07_L05
M07_L09:
       call      qword ptr [7FF96E84D928]; Precode of System.SpanHelpers.Memmove(Byte ByRef, Byte ByRef, UIntPtr)
       jmp       short M07_L05
; Total bytes of code 243
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
       je        near ptr M00_L13
       mov       rdx,8001400450
       cmp       r15,[rdx]
       je        near ptr M00_L09
       mov       rdx,8001400458
       cmp       r15,[rdx]
       je        near ptr M00_L09
       movsx     rdx,r13w
M00_L00:
       cmp       edx,18
       jne       near ptr M00_L09
       mov       rsi,r15
       test      rsi,rsi
       je        short M00_L01
       mov       rdx,offset MT_AuroraScript.Runtime.Types.ScriptArray
       cmp       [rsi],rdx
       jne       near ptr M00_L16
M00_L01:
       test      rsi,rsi
       je        near ptr M00_L17
M00_L02:
       test      rsi,rsi
       je        near ptr M00_L09
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptArray
       cmp       [rsi],rcx
       jne       near ptr M00_L09
       mov       rbp,[rsi+38]
       mov       r15d,[rsi+34]
       test      rbp,rbp
       je        near ptr M00_L21
       cmp       [rbp+8],r15d
       jb        near ptr M00_L24
       add       rbp,10
M00_L03:
       cmp       [r14+8],r15d
       jl        near ptr M00_L22
M00_L04:
       test      r14,r14
       je        near ptr M00_L23
       cmp       [r14+8],r15d
       jl        near ptr M00_L24
       lea       rcx,[r14+10]
       mov       esi,r15d
M00_L05:
       cmp       r15d,esi
       jg        near ptr M00_L27
       mov       r8d,r15d
       shl       r8,4
       cmp       r8,4000
       ja        near ptr M00_L26
       mov       rdx,rbp
       call      00007FF9D74435E0
       cmp       dword ptr [7FF9D7784A90],0
       jne       near ptr M00_L25
M00_L06:
       mov       ebp,r15d
       mov       [rsp+34],ebp
M00_L07:
       mov       rdx,r14
       mov       ebp,[rsp+34]
M00_L08:
       lea       rcx,[rbx+40]
       call      CORINFO_HELP_ASSIGN_REF
       mov       eax,ebp
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
M00_L09:
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptPackedArray
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r12,rax
       test      r12,r12
       je        near ptr M00_L12
       mov       rcx,r12
       mov       rax,[r12]
       mov       rax,[rax+58]
       call      qword ptr [rax+8]
       mov       edx,eax
       mov       rcx,r14
       xor       r8d,r8d
       call      qword ptr [7FF977C35D70]; AuroraScript.Runtime.CallOps.EnsureCapacity(AuroraScript.Runtime.ScriptDatum[], Int32, Int32)
       mov       r14,rax
       xor       r15d,r15d
       mov       rcx,r12
       mov       rax,[r12]
       mov       r13,[rax+58]
       call      qword ptr [r13+8]
       test      eax,eax
       jle       short M00_L11
M00_L10:
       lea       edx,[rbp+1]
       mov       [rsp+34],edx
       lea       rdx,[rsp+20]
       mov       rcx,r12
       mov       r8d,r15d
       call      qword ptr [r13+10]
       cmp       ebp,[r14+8]
       jae       near ptr M00_L28
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
       jle       near ptr M00_L07
       mov       ebp,[rsp+34]
       jmp       short M00_L10
M00_L11:
       mov       [rsp+34],ebp
       jmp       near ptr M00_L07
M00_L12:
       mov       rcx,r14
       mov       edx,1
       xor       r8d,r8d
       call      qword ptr [7FF977C35D70]; AuroraScript.Runtime.CallOps.EnsureCapacity(AuroraScript.Runtime.ScriptDatum[], Int32, Int32)
       mov       rsi,rax
       mov       ebp,1
       cmp       dword ptr [rsi+8],0
       jbe       near ptr M00_L28
       lea       rdi,[rsi+10]
       mov       rcx,rdi
       mov       rdx,r15
       call      CORINFO_HELP_ASSIGN_REF
       mov       [rdi+8],r13
       mov       rdx,rsi
       jmp       near ptr M00_L08
M00_L13:
       test      r13,r13
       je        short M00_L15
       lea       rdx,[r13-1]
       mov       ecx,1
       mov       eax,2
       cmp       rdx,1
       cmova     ecx,eax
       mov       edx,ecx
M00_L14:
       jmp       near ptr M00_L00
M00_L15:
       xor       edx,edx
       jmp       short M00_L14
M00_L16:
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rsi,rax
       jmp       near ptr M00_L01
M00_L17:
       mov       rdx,r15
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       short M00_L18
       xor       esi,esi
       jmp       near ptr M00_L02
M00_L18:
       cmp       dword ptr [rdi+8],0
       jne       short M00_L19
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001401498
       mov       rsi,[rcx]
       jmp       short M00_L20
M00_L19:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      CORINFO_HELP_NEWSFAST
       mov       rsi,rax
       mov       rcx,rsi
       mov       rdx,rdi
       call      qword ptr [7FF977C37E70]
M00_L20:
       jmp       near ptr M00_L02
M00_L21:
       test      r15d,r15d
       jne       short M00_L24
       xor       ebp,ebp
       xor       r15d,r15d
       jmp       near ptr M00_L03
M00_L22:
       mov       rcx,8001400B38
       mov       rcx,[rcx]
       mov       edx,r15d
       call      qword ptr [7FF977C02DB8]; Precode of System.Buffers.SharedArrayPool`1[[AuroraScript.Runtime.ScriptDatum, AuroraScript]].Rent(Int32)
       mov       rsi,rax
       mov       rcx,r14
       mov       rdx,rsi
       xor       r8d,r8d
       call      qword ptr [7FF97783F588]; System.Array.Copy(System.Array, System.Array, Int32)
       mov       rcx,8001400B38
       mov       rcx,[rcx]
       mov       rdx,r14
       xor       r8d,r8d
       call      qword ptr [7FF977C02DC0]; System.Buffers.SharedArrayPool`1[[AuroraScript.Runtime.ScriptDatum, AuroraScript]].Return(AuroraScript.Runtime.ScriptDatum[], Boolean)
       mov       r14,rsi
       jmp       near ptr M00_L04
M00_L23:
       test      r15d,r15d
       jne       short M00_L24
       xor       ecx,ecx
       xor       esi,esi
       jmp       near ptr M00_L05
M00_L24:
       call      qword ptr [7FF977A07D50]
       int       3
M00_L25:
       call      CORINFO_HELP_POLL_GC
       jmp       near ptr M00_L06
M00_L26:
       mov       rdx,rbp
       call      qword ptr [7FF977C37E58]
       jmp       near ptr M00_L06
M00_L27:
       call      qword ptr [7FF977C35EF0]
       int       3
M00_L28:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 875
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
       mov       rcx,8001400B38
       mov       rcx,[rcx]
       call      qword ptr [7FF977C02DB8]; Precode of System.Buffers.SharedArrayPool`1[[AuroraScript.Runtime.ScriptDatum, AuroraScript]].Rent(Int32)
       mov       rdi,rax
       mov       rcx,rbx
       mov       rdx,rdi
       mov       r8d,esi
       call      qword ptr [7FF97783F588]; System.Array.Copy(System.Array, System.Array, Int32)
       test      esi,esi
       jle       short M02_L01
       mov       rcx,rbx
       mov       r8d,esi
       xor       edx,edx
       call      qword ptr [7FF977C37F00]; System.Array.Clear(System.Array, Int32, Int32)
M02_L01:
       mov       rcx,8001400B38
       mov       rcx,[rcx]
       mov       rdx,rbx
       xor       r8d,r8d
       call      qword ptr [7FF977C02DC0]; System.Buffers.SharedArrayPool`1[[AuroraScript.Runtime.ScriptDatum, AuroraScript]].Return(AuroraScript.Runtime.ScriptDatum[], Boolean)
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
       jmp       qword ptr [7FF977835C38]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBaseSlow(System.Runtime.CompilerServices.MethodTable*)
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
       call      qword ptr [7FF97783FD38]
M05_L01:
       mov       rax,[rbp+18]
       mov       ecx,[rax+8]
       call      qword ptr [7FF977BBCB28]; System.Buffers.Utilities.SelectBucketIndex(Int32)
       mov       [rbp-14],eax
       mov       ecx,5
       call      qword ptr [7FF977BBCB40]; System.Runtime.CompilerServices.StaticsHelpers.GetOptimizedGCThreadStaticBase(Int32)
       mov       rax,[rax+10]
       mov       [rbp-40],rax
       mov       rax,[rbp-40]
       mov       [rbp-48],rax
       cmp       qword ptr [rbp-40],0
       jne       short M05_L02
       mov       rcx,[rbp+10]
       call      qword ptr [7FF977D0DC68]; System.Buffers.SharedArrayPool`1[[AuroraScript.Runtime.ScriptDatum, AuroraScript]].InitializeTlsBucketsAndTrimming()
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
       call      qword ptr [7FF977C3D3F8]
M05_L03:
       mov       rax,[rbp+18]
       mov       eax,[rax+8]
       mov       [rbp-0C0],eax
       mov       ecx,[rbp-14]
       call      qword ptr [7FF977BBCBB8]; System.Buffers.Utilities.GetMaxSizeForBucket(Int32)
       cmp       eax,[rbp-0C0]
       je        short M05_L04
       mov       rcx,offset MT_System.ArgumentException
       call      CORINFO_HELP_NEWSFAST
       mov       [rbp-0F0],rax
       call      qword ptr [7FF977D06400]
       mov       [rbp-0F8],rax
       mov       ecx,29B
       mov       rdx,7FF977774000
       call      qword ptr [7FF97783F210]
       mov       [rbp-100],rax
       mov       rdx,[rbp-0F8]
       mov       r8,[rbp-100]
       mov       rcx,[rbp-0F0]
       call      qword ptr [7FF977B2D128]
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
       call      qword ptr [7FF977D06418]; System.Buffers.SharedArrayPoolThreadLocalArray..ctor(System.Array)
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
       call      qword ptr [7FF977D0DC80]
       mov       [rbp-0E8],rax
M05_L05:
       mov       rcx,[rbp-0E8]
       mov       rdx,[rbp-38]
       cmp       [rcx],ecx
       call      qword ptr [7FF977D06448]
       mov       [rbp-28],eax
M05_L06:
       mov       rax,80014002B8
       mov       rax,[rax]
       mov       [rbp-30],rax
       mov       rcx,[rbp-30]
       cmp       [rcx],ecx
       call      qword ptr [7FF977BBCB58]; System.Diagnostics.Tracing.EventSource.IsEnabled()
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
       call      qword ptr [7FF977BBCB70]
       mov       [rbp-104],eax
       mov       r9d,[rbp-104]
       mov       edx,[rbp-4C]
       mov       r8d,[rbp-50]
       mov       rcx,[rbp-30]
       cmp       [rcx],ecx
       call      qword ptr [7FF977D06460]
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
       call      qword ptr [7FF977BBCB70]
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
       call      qword ptr [7FF977D06478]
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
       call      qword ptr [7FF977BBCB40]; System.Runtime.CompilerServices.StaticsHelpers.GetOptimizedGCThreadStaticBase(Int32)
       lea       rcx,[rax+10]
       mov       rdx,[rbp-8]
       call      CORINFO_HELP_ASSIGN_REF
       mov       rax,[rbp+10]
       mov       rcx,[rax+8]
       mov       rdx,[rbp-8]
       xor       r8d,r8d
       cmp       [rcx],ecx
       call      qword ptr [7FF977A05F80]; System.Runtime.CompilerServices.ConditionalWeakTable`2[[System.__Canon, System.Private.CoreLib],[System.__Canon, System.Private.CoreLib]].Add(System.__Canon, System.__Canon)
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
       mov       rax,80014016E8
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
       mov       rax,80014016E0
       mov       rax,[rax]
       mov       [rbp-28],rax
       mov       rdx,[rbp-28]
       mov       rcx,[rbp-20]
       mov       r8,7FF977D09C98
       call      qword ptr [7FF977836BB0]; System.MulticastDelegate.CtorClosed(System.Object, IntPtr)
       mov       rcx,offset MT_System.Buffers.SharedArrayPool<AuroraScript.Runtime.ScriptDatum>+<>c
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rdx,[rbp-20]
       mov       rcx,80014016E8
       call      CORINFO_HELP_ASSIGN_REF
       mov       rax,[rbp-20]
       mov       [rbp-18],rax
M11_L00:
       mov       rcx,[rbp-18]
       mov       rdx,[rbp+10]
       call      qword ptr [7FF977D064C0]; System.Gen2GcCallback.Register(System.Func`2<System.Object,Boolean>, System.Object)
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
       mov       rcx,7FF977CC1F24
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
       mov       rcx,7FF977CC1F20
       call      CORINFO_HELP_COUNTPROFILE32
       call      qword ptr [7FF977C3F048]
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
       jmp       short M00_L03
M00_L00:
       vmovq     rax,xmm0
       test      rax,rax
       jl        short M00_L05
M00_L01:
       vxorps    xmm1,xmm1,xmm1
       vmaxsd    xmm1,xmm0,xmm1
       mov       eax,0FFFFFFFF
       vcvttsd2si rcx,xmm1
       vucomisd  xmm0,qword ptr [7FF9778BFAA8]
       cmovb     eax,ecx
       mov       ebp,eax
M00_L02:
       xor       esi,ebp
       inc       edi
       cmp       edi,200
       jge       short M00_L04
M00_L03:
       mov       rax,[rbx+10]
       cmp       edi,[rax+8]
       jae       short M00_L06
       vmovsd    xmm0,qword ptr [rax+rdi*8+10]
       vxorps    xmm1,xmm1,xmm1
       vucomisd  xmm0,xmm1
       jb        short M00_L05
       vmovsd    xmm1,qword ptr [7FF9778BFAB0]
       vucomisd  xmm1,xmm0
       jb        short M00_L05
       vroundsd  xmm1,xmm1,xmm0,0B
       vucomisd  xmm1,xmm0
       jp        short M00_L05
       jne       short M00_L05
       vxorps    xmm1,xmm1,xmm1
       vucomisd  xmm0,xmm1
       jp        short M00_L01
       jne       short M00_L01
       jmp       short M00_L00
M00_L04:
       mov       eax,esi
       add       rsp,28
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       ret
M00_L05:
       call      qword ptr [7FF977C45D10]
       mov       ebp,eax
       jmp       short M00_L02
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
       push      r13
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,40
       vxorps    xmm4,xmm4,xmm4
       vmovdqu   ymmword ptr [rsp+20],ymm4
       mov       rbx,rcx
       xor       esi,esi
       xor       edi,edi
M00_L00:
       mov       rcx,[rbx+20]
       cmp       edi,[rcx+8]
       jae       near ptr M00_L16
       mov       rax,rdi
       shl       rax,4
       lea       rcx,[rcx+rax+10]
       mov       rbp,[rcx]
       mov       r14,[rcx+8]
       test      rbp,rbp
       jne       near ptr M00_L05
       test      r14,r14
       je        near ptr M00_L08
       lea       rcx,[r14-1]
       cmp       rcx,1
       jbe       near ptr M00_L09
       mov       r15d,2
M00_L01:
       cmp       r15d,2
       jne       near ptr M00_L10
       mov       r8,8007FFFFFFFFFFFF
       add       r8,r14
       cmp       r8,3
       jbe       near ptr M00_L12
       vmovq     xmm0,r14
M00_L02:
       vucomisd  xmm0,qword ptr [7FF9778A0410]
       jb        near ptr M00_L15
       vmovsd    xmm1,qword ptr [7FF9778A0418]
       vucomisd  xmm1,xmm0
       jbe       near ptr M00_L15
       vroundsd  xmm1,xmm1,xmm0,0B
       vucomisd  xmm1,xmm0
       jp        near ptr M00_L15
       jne       near ptr M00_L15
       vxorps    xmm1,xmm1,xmm1
       vucomisd  xmm0,xmm1
       jp        short M00_L03
       je        near ptr M00_L14
M00_L03:
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       r8,7FFFFFFFFFFFFFFF
       vcvttsd2si rcx,xmm1
       vucomisd  xmm0,qword ptr [7FF9778A0418]
       cmovb     r8,rcx
       mov       r13,r8
M00_L04:
       xor       rsi,r13
       inc       edi
       cmp       edi,200
       jl        near ptr M00_L00
       mov       rax,rsi
       add       rsp,40
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r13
       pop       r14
       pop       r15
       ret
M00_L05:
       mov       rcx,offset MT_AuroraScript.Runtime.ScriptDatum
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001400450
       cmp       rbp,[rcx]
       jne       short M00_L06
       mov       r15d,1000
       jmp       near ptr M00_L01
M00_L06:
       mov       rcx,offset MT_AuroraScript.Runtime.ScriptDatum
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       r8,8001400458
       cmp       rbp,[r8]
       jne       short M00_L07
       mov       r15d,2000
       jmp       near ptr M00_L01
M00_L07:
       movsx     r15,r14w
       jmp       near ptr M00_L01
M00_L08:
       xor       r15d,r15d
       jmp       near ptr M00_L01
M00_L09:
       mov       r15d,1
       jmp       near ptr M00_L01
M00_L10:
       cmp       r15d,1000
       je        short M00_L11
       cmp       r15d,2000
       jne       short M00_L15
       test      r14,r14
       jl        short M00_L15
M00_L11:
       mov       r13,r14
       jmp       near ptr M00_L04
M00_L12:
       cmp       r8,2
       jbe       short M00_L13
       vmovsd    xmm0,qword ptr [7FF9778A0420]
       jmp       near ptr M00_L02
M00_L13:
       vmovq     xmm0,r8
       jmp       near ptr M00_L02
M00_L14:
       vmovq     r8,xmm0
       not       r8
       shr       r8,3F
       test      r8d,r8d
       jne       near ptr M00_L03
M00_L15:
       mov       [rsp+20],rbp
       mov       [rsp+28],r14
       lea       r8,[rsp+20]
       lea       rcx,[rsp+30]
       mov       edx,13
       call      qword ptr [7FF977C25D88]
       xor       r13d,r13d
       jmp       near ptr M00_L04
M00_L16:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 521
```
```assembly
; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       push      rsi
       push      rbx
       sub       rsp,28
       mov       rbx,rcx
       mov       rcx,[rbx+20]
       mov       rsi,[rcx-18]
       mov       rcx,rsi
       test      cl,1
       jne       short M01_L00
       mov       rcx,7FF977C82764
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rax,rsi
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
M01_L00:
       mov       rcx,7FF977C82760
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rcx,rbx
       add       rsp,28
       pop       rbx
       pop       rsi
       jmp       qword ptr [7FF977825C38]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBaseSlow(System.Runtime.CompilerServices.MethodTable*)
; Total bytes of code 80
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
       push      r13
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,40
       vxorps    xmm4,xmm4,xmm4
       vmovdqu   ymmword ptr [rsp+20],ymm4
       mov       rbx,rcx
       xor       esi,esi
       xor       edi,edi
M00_L00:
       mov       rcx,[rbx+20]
       cmp       edi,[rcx+8]
       jae       near ptr M00_L17
       mov       rax,rdi
       shl       rax,4
       lea       rcx,[rcx+rax+10]
       mov       rbp,[rcx]
       mov       r14,[rcx+8]
       test      rbp,rbp
       jne       near ptr M00_L05
       test      r14,r14
       je        near ptr M00_L08
       lea       rcx,[r14-1]
       cmp       rcx,1
       jbe       near ptr M00_L09
       mov       r15d,2
M00_L01:
       cmp       r15d,2
       jne       near ptr M00_L10
       mov       r8,8007FFFFFFFFFFFF
       add       r8,r14
       cmp       r8,3
       jbe       near ptr M00_L13
       vmovq     xmm0,r14
M00_L02:
       vxorps    xmm1,xmm1,xmm1
       vucomisd  xmm0,xmm1
       jb        near ptr M00_L16
       vmovsd    xmm1,qword ptr [7FF9778904A8]
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
       vxorps    xmm1,xmm1,xmm1
       vmaxsd    xmm1,xmm0,xmm1
       vmovsd    xmm2,qword ptr [7FF9778904A8]
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
       mov       r13,rdx
M00_L04:
       xor       rsi,r13
       inc       edi
       cmp       edi,200
       jl        near ptr M00_L00
       mov       rax,rsi
       add       rsp,40
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r13
       pop       r14
       pop       r15
       ret
M00_L05:
       mov       rcx,offset MT_AuroraScript.Runtime.ScriptDatum
       call      qword ptr [7FF977815728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,8001400450
       cmp       rbp,[rcx]
       jne       short M00_L06
       mov       r15d,1000
       jmp       near ptr M00_L01
M00_L06:
       mov       rcx,offset MT_AuroraScript.Runtime.ScriptDatum
       call      qword ptr [7FF977815728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       r8,8001400458
       cmp       rbp,[r8]
       jne       short M00_L07
       mov       r15d,2000
       jmp       near ptr M00_L01
M00_L07:
       movsx     r15,r14w
       jmp       near ptr M00_L01
M00_L08:
       xor       r15d,r15d
       jmp       near ptr M00_L01
M00_L09:
       mov       r15d,1
       jmp       near ptr M00_L01
M00_L10:
       cmp       r15d,1000
       je        short M00_L11
       cmp       r15d,2000
       jne       short M00_L16
       jmp       short M00_L12
M00_L11:
       test      r14,r14
       jl        short M00_L16
M00_L12:
       mov       r13,r14
       jmp       near ptr M00_L04
M00_L13:
       cmp       r8,2
       jbe       short M00_L14
       vmovsd    xmm0,qword ptr [7FF9778904B0]
       jmp       near ptr M00_L02
M00_L14:
       vmovq     xmm0,r8
       jmp       near ptr M00_L02
M00_L15:
       vmovq     r8,xmm0
       not       r8
       shr       r8,3F
       test      r8d,r8d
       jne       near ptr M00_L03
M00_L16:
       mov       [rsp+20],rbp
       mov       [rsp+28],r14
       lea       r8,[rsp+20]
       lea       rcx,[rsp+30]
       mov       edx,14
       call      qword ptr [7FF977C15DA0]
       xor       r13d,r13d
       jmp       near ptr M00_L04
M00_L17:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 548
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
       jmp       qword ptr [7FF977815C38]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBaseSlow(System.Runtime.CompilerServices.MethodTable*)
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

