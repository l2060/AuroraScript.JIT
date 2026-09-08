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
       vucomisd  xmm0,qword ptr [7FF9778A1DA8]
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
       vmovsd    xmm0,qword ptr [7FF9778A1DB0]
       jmp       near ptr M00_L03
M00_L14:
       lea       rcx,[rsp+70]
       lea       rdx,[rsp+68]
       call      qword ptr [7FF977C25DB8]
       test      eax,eax
       je        short M00_L17
       jmp       near ptr M00_L04
M00_L15:
       lea       r8,[rsp+58]
       mov       rcx,r14
       call      qword ptr [7FF977C25E18]
       jmp       near ptr M00_L05
M00_L16:
       lea       rdx,[rsp+80]
       mov       rcx,r14
       mov       r8d,[rsp+68]
       mov       r11,7FF9777704E0
       call      qword ptr [r11]
       jmp       near ptr M00_L100
M00_L17:
       mov       rdx,rdi
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptPackedArray
       call      qword ptr [7FF977826850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
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
       vmovsd    xmm0,qword ptr [7FF9778A1DB0]
       jmp       short M00_L20
M00_L19:
       vmovq     xmm0,rdx
M00_L20:
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       rcx,7FFFFFFFFFFFFFFF
       vcvttsd2si rdx,xmm1
       vucomisd  xmm0,qword ptr [7FF9778A1DA8]
       cmovb     rcx,rdx
       mov       [rsp+68],rcx
       jmp       short M00_L22
M00_L21:
       lea       rcx,[rsp+70]
       lea       rdx,[rsp+68]
       call      qword ptr [7FF977C25DB8]
       test      eax,eax
       je        near ptr M00_L70
M00_L22:
       mov       edi,[rsp+68]
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt32Array
       call      qword ptr [7FF977826850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       near ptr M00_L23
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt8Array
       call      qword ptr [7FF977826850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       near ptr M00_L27
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptFloat32Array
       call      qword ptr [7FF977826850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       near ptr M00_L31
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptFloat64Array
       call      qword ptr [7FF977826850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       near ptr M00_L37
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptBooleanArray
       call      qword ptr [7FF977826850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       near ptr M00_L43
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptUInt8Array
       call      qword ptr [7FF977826850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       near ptr M00_L47
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt16Array
       call      qword ptr [7FF977826850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       near ptr M00_L51
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptUInt16Array
       call      qword ptr [7FF977826850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       near ptr M00_L55
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptUInt32Array
       call      qword ptr [7FF977826850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       near ptr M00_L59
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt64Array
       call      qword ptr [7FF977826850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       near ptr M00_L63
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptUInt64Array
       call      qword ptr [7FF977826850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
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
       call      qword ptr [7FF977C2C5A0]
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
       call      qword ptr [7FF977C2C5A0]
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
       call      qword ptr [7FF977C2C5A0]
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
       call      qword ptr [7FF977C2C5A0]
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
       call      qword ptr [7FF977C2C5A0]
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
       call      qword ptr [7FF977C2C5A0]
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
       call      qword ptr [7FF977C2C5A0]
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
       call      qword ptr [7FF977C2C5A0]
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
       call      qword ptr [7FF977C2C5A0]
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
       call      qword ptr [7FF977C2C5A0]
M00_L64:
       mov       rcx,[rbp+38]
       cmp       edi,[rcx+8]
       jae       near ptr M00_L102
       mov       eax,edi
       mov       r15,[rcx+rax*8+10]
       mov       rcx,offset MT_AuroraScript.Runtime.ScriptDatum
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       jmp       short M00_L69
M00_L65:
       mov       rdx,[rbp+38]
       cmp       [rdx+8],edi
       ja        short M00_L66
       mov       rdx,[rbp+38]
       mov       edx,[rdx+8]
       mov       ecx,edi
       call      qword ptr [7FF977C2C5A0]
M00_L66:
       mov       rcx,[rbp+38]
       cmp       edi,[rcx+8]
       jae       near ptr M00_L102
       mov       eax,edi
       mov       r15,[rcx+rax*8+10]
       mov       rcx,offset MT_AuroraScript.Runtime.ScriptDatum
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       jmp       short M00_L69
M00_L67:
       mov       rcx,r15
       mov       rax,[r15]
       mov       rax,[rax+58]
       call      qword ptr [rax+8]
       mov       edx,eax
       mov       ecx,edi
       call      qword ptr [7FF977C2C5A0]
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
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0F5B9C00450
       cmp       r15,[rcx]
       jne       short M00_L71
       mov       eax,1000
       jmp       short M00_L76
M00_L71:
       mov       rdi,[rsp+38]
       mov       rcx,offset MT_AuroraScript.Runtime.ScriptDatum
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0F5B9C00458
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
       lea       rax,[7FF9778A1DB8]
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
       call      qword ptr [7FF977C2C5B8]
       jmp       near ptr M00_L99
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0F5B9C00410
       mov       rdi,[rcx]
       jmp       near ptr M00_L99
       cmp       qword ptr [rsp+40],2
       je        short M00_L78
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BooleanValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0F5B9C01500
       mov       rdi,[rcx]
       jmp       short M00_L79
M00_L78:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BooleanValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0F5B9C014F8
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
       vmovsd    xmm1,qword ptr [7FF9778A1DB0]
       jmp       short M00_L82
M00_L81:
       vmovq     xmm1,rax
M00_L82:
       vucomisd  xmm1,xmm1
       jp        short M00_L83
       je        short M00_L84
M00_L83:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0F5B9C014E8
       mov       rdi,[rcx]
       jmp       near ptr M00_L96
M00_L84:
       vxorps    xmm0,xmm0,xmm0
       vucomisd  xmm1,xmm0
       jp        short M00_L85
       jne       short M00_L85
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0F5B9C01498
       mov       rdi,[rcx]
       jmp       near ptr M00_L96
M00_L85:
       vucomisd  xmm1,qword ptr [7FF9778A1DD0]
       jp        short M00_L86
       jne       short M00_L86
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0F5B9C014A0
       mov       rdi,[rcx]
       jmp       near ptr M00_L96
M00_L86:
       vucomisd  xmm1,qword ptr [7FF9778A1DD8]
       jp        short M00_L87
       jne       short M00_L87
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0F5B9C014A8
       mov       rdi,[rcx]
       jmp       near ptr M00_L96
M00_L87:
       vucomisd  xmm1,qword ptr [7FF9778A1DE0]
       jp        short M00_L88
       jne       short M00_L88
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0F5B9C014B0
       mov       rdi,[rcx]
       jmp       near ptr M00_L96
M00_L88:
       vucomisd  xmm1,qword ptr [7FF9778A1DE8]
       jp        short M00_L89
       jne       short M00_L89
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0F5B9C014B8
       mov       rdi,[rcx]
       jmp       near ptr M00_L96
M00_L89:
       vucomisd  xmm1,qword ptr [7FF9778A1DF0]
       jp        short M00_L90
       jne       short M00_L90
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0F5B9C014C0
       mov       rdi,[rcx]
       jmp       near ptr M00_L96
M00_L90:
       vucomisd  xmm1,qword ptr [7FF9778A1DF8]
       jp        short M00_L91
       jne       short M00_L91
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0F5B9C014C8
       mov       rdi,[rcx]
       jmp       near ptr M00_L96
M00_L91:
       vucomisd  xmm1,qword ptr [7FF9778A1E00]
       jp        short M00_L92
       jne       short M00_L92
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0F5B9C014D0
       mov       rdi,[rcx]
       jmp       near ptr M00_L96
M00_L92:
       vucomisd  xmm1,qword ptr [7FF9778A1E08]
       jp        short M00_L93
       jne       short M00_L93
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0F5B9C014D8
       mov       rdi,[rcx]
       jmp       near ptr M00_L96
M00_L93:
       vucomisd  xmm1,qword ptr [7FF9778A1E10]
       jp        short M00_L94
       jne       short M00_L94
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0F5B9C014E0
       mov       rdi,[rcx]
       jmp       short M00_L96
M00_L94:
       vmovsd    qword ptr [rsp+30],xmm1
       vucomisd  xmm1,qword ptr [7FF9778A1E18]
       jp        short M00_L95
       jne       short M00_L95
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0F5B9C01490
       mov       rdi,[rcx]
       jmp       short M00_L96
M00_L95:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      CORINFO_HELP_NEWSFAST
       mov       rdi,rax
       mov       rcx,rdi
       vmovsd    xmm1,qword ptr [rsp+30]
       call      qword ptr [7FF977C2C5D0]
M00_L96:
       jmp       short M00_L99
M00_L97:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.Int64Value
       call      CORINFO_HELP_NEWSFAST
       mov       rdi,rax
       mov       rcx,rdi
       mov       rdx,[rsp+40]
       call      qword ptr [7FF977C2C5E8]
       jmp       short M00_L99
       lea       rcx,[rsp+38]
       call      qword ptr [7FF977C2C600]
       mov       rdi,rax
       jmp       short M00_L99
M00_L98:
       lea       rcx,[rsp+38]
       call      qword ptr [7FF977C2C618]
       mov       rdi,rax
M00_L99:
       vmovdqu   xmm0,xmmword ptr [rsp+70]
       vmovdqu   xmmword ptr [rsp+20],xmm0
       lea       rcx,[rsp+20]
       call      qword ptr [7FF977C25DA0]
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
       vmovsd    xmm0,qword ptr [7FF9778A1DB0]
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
       jmp       qword ptr [7FF977BA5488]; System.Runtime.CompilerServices.CastHelpers.IsInstance_Helper(Void*, System.Object)
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
       mov       rcx,7FF977C8887C
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rax,rsi
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
M04_L00:
       mov       rcx,7FF977C88878
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rcx,rbx
       add       rsp,28
       pop       rbx
       pop       rsi
       jmp       qword ptr [7FF9779FFD68]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBaseSlow(System.Runtime.CompilerServices.MethodTable*)
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
       sub       rsp,160
       vmovaps   [rsp+150],xmm6
       xor       eax,eax
       mov       [rsp+28],rax
       vxorps    xmm4,xmm4,xmm4
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
       jne       near ptr M00_L81
M00_L03:
       test      r13,r13
       je        near ptr M00_L10
       cmp       qword ptr [rsp+140],0
       jne       near ptr M00_L83
       cmp       qword ptr [rsp+148],2
       jbe       near ptr M00_L83
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
       vucomisd  xmm0,qword ptr [7FF9778A3B30]
       cmovb     rcx,rdx
       mov       [rsp+138],rcx
M00_L05:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptArray
       cmp       [r13],rcx
       jne       near ptr M00_L85
       mov       rcx,r13
       mov       edx,[rsp+138]
       mov       [rsp+128],r14
       mov       [rsp+130],r15
       cmp       edx,[rcx+34]
       jae       near ptr M00_L84
       mov       rcx,[rcx+38]
       cmp       edx,[rcx+8]
       jae       near ptr M00_L277
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
       jae       near ptr M00_L277
       shl       rax,4
       vmovdqu   xmm0,xmmword ptr [rcx+rax+10]
       vmovdqu   xmmword ptr [rsp+140],xmm0
       lea       ecx,[rsi+1]
       and       ecx,1F
       cmp       ecx,r8d
       jae       near ptr M00_L277
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
       vmovaps   xmm6,[rsp+150]
       add       rsp,160
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
       ja        near ptr M00_L82
       vmovq     xmm0,rdx
       jmp       near ptr M00_L04
M00_L10:
       mov       rdx,rdi
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptPackedArray
       call      qword ptr [7FF977826850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r13,rax
       test      r13,r13
       jne       near ptr M00_L86
M00_L11:
       test      rdi,rdi
       je        short M00_L15
       test      byte ptr [7FF977A69DD0],1
       je        near ptr M00_L253
M00_L12:
       mov       rcx,0DA53000448
       cmp       rdi,[rcx]
       je        short M00_L14
       mov       rcx,0DA53000450
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
       ja        near ptr M00_L28
       mov       ecx,eax
       lea       rax,[7FF9778A3B38]
       mov       eax,[rax+rcx*4]
       lea       rdx,[M00_L01]
       add       rax,rdx
       jmp       rax
       test      rdi,rdi
       je        near ptr M00_L23
       mov       rcx,offset MT_System.String
       cmp       [rdi],rcx
       jne       near ptr M00_L23
       cmp       dword ptr [rdi+8],0
       jne       short M00_L21
       test      byte ptr [7FF977BDE998],1
       je        near ptr M00_L273
M00_L19:
       mov       rcx,0DA53001528
       mov       r13,[rcx]
M00_L20:
       jmp       near ptr M00_L25
M00_L21:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      CORINFO_HELP_NEWSFAST
       mov       r13,rax
       test      byte ptr [7FF977C71F58],1
       je        near ptr M00_L274
M00_L22:
       mov       rcx,0DA53001538
       mov       rbp,[rcx]
       mov       rcx,0DA4A3C2290
       mov       [r13+10],rcx
       mov       rcx,0DA530003E0
       mov       rdx,[rcx]
       lea       rcx,[r13+20]
       call      CORINFO_HELP_ASSIGN_REF
       lea       rcx,[r13+8]
       mov       rdx,rbp
       call      CORINFO_HELP_ASSIGN_REF
       mov       dword ptr [r13+30],2
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
       jmp       near ptr M00_L34
M00_L26:
       mov       r13,rax
       jmp       near ptr M00_L34
       test      byte ptr [7FF977AABD00],1
       je        near ptr M00_L255
M00_L27:
       mov       rdx,0DA53000408
       mov       r13,[rdx]
       jmp       near ptr M00_L34
M00_L28:
       cmp       eax,1000
       je        near ptr M00_L79
       cmp       eax,2000
       je        near ptr M00_L77
       mov       rdx,rdi
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       call      qword ptr [7FF977826850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rax,rax
       jne       short M00_L26
       test      rdi,rdi
       je        near ptr M00_L33
       mov       rcx,offset MT_System.String
       cmp       [rdi],rcx
       jne       near ptr M00_L33
       mov       r13,rdi
       cmp       dword ptr [r13+8],0
       jne       short M00_L31
       test      byte ptr [7FF977BDE998],1
       je        near ptr M00_L275
M00_L29:
       mov       rcx,0DA53001528
       mov       r13,[rcx]
M00_L30:
       jmp       short M00_L34
M00_L31:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      CORINFO_HELP_NEWSFAST
       mov       rbp,rax
       test      byte ptr [7FF977C71F58],1
       je        near ptr M00_L276
M00_L32:
       mov       rcx,0DA53001538
       mov       rdi,[rcx]
       mov       rcx,0DA4A3C2290
       mov       [rbp+10],rcx
       mov       rcx,0DA530003E0
       mov       rdx,[rcx]
       lea       rcx,[rbp+20]
       call      CORINFO_HELP_ASSIGN_REF
       lea       rcx,[rbp+8]
       mov       rdx,rdi
       call      CORINFO_HELP_ASSIGN_REF
       mov       dword ptr [rbp+30],2
       or        dword ptr [rbp+30],1
       lea       rcx,[rbp+38]
       mov       rdx,r13
       call      CORINFO_HELP_ASSIGN_REF
       mov       r13,rbp
       jmp       short M00_L30
M00_L33:
       xor       r13d,r13d
M00_L34:
       vmovdqu   xmm0,xmmword ptr [rsp+140]
       vmovdqu   xmmword ptr [rsp+28],xmm0
       lea       rcx,[rsp+28]
       call      qword ptr [7FF977C25D40]
       mov       r8,rax
       mov       [rsp+28],r14
       mov       [rsp+30],r15
       lea       r9,[rsp+28]
       mov       rcx,r13
       xor       edx,edx
       mov       rax,[r13]
       mov       rax,[rax+50]
       call      qword ptr [rax+20]
       jmp       near ptr M00_L06
       mov       rcx,8007FFFFFFFFFFFF
       add       rcx,rbp
       cmp       rcx,3
       jbe       near ptr M00_L47
       vmovq     xmm6,rbp
M00_L35:
       vucomisd  xmm6,xmm6
       jp        near ptr M00_L70
       jne       near ptr M00_L70
       vxorps    xmm0,xmm0,xmm0
       vucomisd  xmm6,xmm0
       jp        short M00_L36
       je        near ptr M00_L68
M00_L36:
       vucomisd  xmm6,qword ptr [7FF9778A3B50]
       jp        short M00_L37
       je        near ptr M00_L66
M00_L37:
       vucomisd  xmm6,qword ptr [7FF9778A3B58]
       jp        short M00_L38
       je        near ptr M00_L64
M00_L38:
       vucomisd  xmm6,qword ptr [7FF9778A3B60]
       jp        short M00_L39
       je        near ptr M00_L62
M00_L39:
       vucomisd  xmm6,qword ptr [7FF9778A3B68]
       jp        short M00_L40
       je        near ptr M00_L60
M00_L40:
       vucomisd  xmm6,qword ptr [7FF9778A3B70]
       jp        short M00_L41
       je        near ptr M00_L58
M00_L41:
       vucomisd  xmm6,qword ptr [7FF9778A3B78]
       jp        short M00_L42
       je        near ptr M00_L56
M00_L42:
       vucomisd  xmm6,qword ptr [7FF9778A3B80]
       jp        short M00_L43
       je        near ptr M00_L54
M00_L43:
       vucomisd  xmm6,qword ptr [7FF9778A3B88]
       jp        short M00_L44
       je        near ptr M00_L52
M00_L44:
       vucomisd  xmm6,qword ptr [7FF9778A3B90]
       jp        short M00_L45
       je        near ptr M00_L50
M00_L45:
       vucomisd  xmm6,qword ptr [7FF9778A3B98]
       jp        short M00_L48
       jne       short M00_L48
       test      byte ptr [7FF977BDD4B0],1
       je        near ptr M00_L270
M00_L46:
       mov       rcx,0DA53001488
       mov       r13,[rcx]
       jmp       near ptr M00_L72
M00_L47:
       cmp       rcx,2
       ja        near ptr M00_L258
       vmovq     xmm6,rcx
       jmp       near ptr M00_L35
M00_L48:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      CORINFO_HELP_NEWSFAST
       mov       r13,rax
       test      byte ptr [7FF977C71D18],1
       je        near ptr M00_L271
M00_L49:
       mov       rcx,0DA53001508
       mov       rbp,[rcx]
       mov       rcx,0DA4A3C2290
       mov       [r13+10],rcx
       mov       rcx,0DA530003E0
       mov       rdx,[rcx]
       lea       rcx,[r13+20]
       call      CORINFO_HELP_ASSIGN_REF
       lea       rcx,[r13+8]
       mov       rdx,rbp
       call      CORINFO_HELP_ASSIGN_REF
       mov       dword ptr [r13+30],2
       or        dword ptr [r13+30],1
       vmovsd    qword ptr [r13+38],xmm6
       jmp       near ptr M00_L72
M00_L50:
       test      byte ptr [7FF977BDD4B0],1
       je        near ptr M00_L269
M00_L51:
       mov       rcx,0DA530014D8
       mov       r13,[rcx]
       jmp       near ptr M00_L72
M00_L52:
       test      byte ptr [7FF977BDD4B0],1
       je        near ptr M00_L268
M00_L53:
       mov       rcx,0DA530014D0
       mov       r13,[rcx]
       jmp       near ptr M00_L72
M00_L54:
       test      byte ptr [7FF977BDD4B0],1
       je        near ptr M00_L267
M00_L55:
       mov       rcx,0DA530014C8
       mov       r13,[rcx]
       jmp       near ptr M00_L72
M00_L56:
       test      byte ptr [7FF977BDD4B0],1
       je        near ptr M00_L266
M00_L57:
       mov       rcx,0DA530014C0
       mov       r13,[rcx]
       jmp       near ptr M00_L72
M00_L58:
       test      byte ptr [7FF977BDD4B0],1
       je        near ptr M00_L265
M00_L59:
       mov       rcx,0DA530014B8
       mov       r13,[rcx]
       jmp       near ptr M00_L72
M00_L60:
       test      byte ptr [7FF977BDD4B0],1
       je        near ptr M00_L264
M00_L61:
       mov       rcx,0DA530014B0
       mov       r13,[rcx]
       jmp       near ptr M00_L72
M00_L62:
       test      byte ptr [7FF977BDD4B0],1
       je        near ptr M00_L263
M00_L63:
       mov       rcx,0DA530014A8
       mov       r13,[rcx]
       jmp       short M00_L72
M00_L64:
       test      byte ptr [7FF977BDD4B0],1
       je        near ptr M00_L262
M00_L65:
       mov       rcx,0DA530014A0
       mov       r13,[rcx]
       jmp       short M00_L72
M00_L66:
       test      byte ptr [7FF977BDD4B0],1
       je        near ptr M00_L261
M00_L67:
       mov       rcx,0DA53001498
       mov       r13,[rcx]
       jmp       short M00_L72
M00_L68:
       test      byte ptr [7FF977BDD4B0],1
       je        near ptr M00_L260
M00_L69:
       mov       rcx,0DA53001490
       mov       r13,[rcx]
       jmp       short M00_L72
M00_L70:
       test      byte ptr [7FF977BDD4B0],1
       je        near ptr M00_L259
M00_L71:
       mov       rcx,0DA530014E0
       mov       r13,[rcx]
M00_L72:
       jmp       near ptr M00_L34
       cmp       rbp,2
       je        short M00_L75
       test      byte ptr [7FF977BDDDB8],1
       je        near ptr M00_L256
M00_L73:
       mov       rcx,0DA530014F8
       mov       r13,[rcx]
M00_L74:
       jmp       near ptr M00_L34
M00_L75:
       test      byte ptr [7FF977BDDDB8],1
       je        near ptr M00_L257
M00_L76:
       mov       rcx,0DA530014F0
       mov       r13,[rcx]
       jmp       short M00_L74
M00_L77:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.UInt64Value
       call      CORINFO_HELP_NEWSFAST
       mov       r13,rax
       test      byte ptr [7FF977C71E98],1
       je        near ptr M00_L254
M00_L78:
       mov       rcx,0DA53001518
       mov       rdi,[rcx]
       mov       rcx,0DA4A3C2290
       mov       [r13+10],rcx
       mov       rcx,0DA530003E0
       mov       rdx,[rcx]
       lea       rcx,[r13+20]
       call      CORINFO_HELP_ASSIGN_REF
       lea       rcx,[r13+8]
       mov       rdx,rdi
       call      CORINFO_HELP_ASSIGN_REF
       mov       dword ptr [r13+30],2
       or        dword ptr [r13+30],1
       mov       [r13+38],rbp
       jmp       near ptr M00_L34
M00_L79:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.Int64Value
       call      CORINFO_HELP_NEWSFAST
       mov       r13,rax
       test      byte ptr [7FF977C71DD8],1
       je        near ptr M00_L272
M00_L80:
       mov       rcx,0DA53001510
       mov       rdi,[rcx]
       mov       rcx,0DA4A3C2290
       mov       [r13+10],rcx
       mov       rcx,0DA530003E0
       mov       rdx,[rcx]
       lea       rcx,[r13+20]
       call      CORINFO_HELP_ASSIGN_REF
       lea       rcx,[r13+8]
       mov       rdx,rdi
       call      CORINFO_HELP_ASSIGN_REF
       mov       dword ptr [r13+30],2
       or        dword ptr [r13+30],1
       mov       [r13+38],rbp
       jmp       near ptr M00_L34
M00_L81:
       mov       rdx,rdi
       mov       rcx,offset MT_AuroraScript.Runtime.Types.IAuroraNativeIndexer
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfInterface(Void*, System.Object)
       mov       r13,rax
       jmp       near ptr M00_L03
M00_L82:
       vmovsd    xmm0,qword ptr [7FF9778A3BA0]
       jmp       near ptr M00_L04
M00_L83:
       lea       rcx,[rsp+140]
       lea       rdx,[rsp+138]
       call      qword ptr [7FF977C25D88]
       test      eax,eax
       je        near ptr M00_L10
       jmp       near ptr M00_L05
M00_L84:
       lea       r8,[rsp+128]
       call      qword ptr [7FF977BAC108]
       jmp       near ptr M00_L06
M00_L85:
       mov       edx,[rsp+138]
       mov       [rsp+28],r14
       mov       [rsp+30],r15
       lea       r8,[rsp+28]
       mov       rcx,r13
       mov       r11,7FF9777704E0
       call      qword ptr [r11]
       jmp       near ptr M00_L06
M00_L86:
       cmp       qword ptr [rsp+140],0
       jne       short M00_L90
       cmp       qword ptr [rsp+148],2
       jbe       short M00_L90
       mov       rcx,[rsp+148]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L87
       vmovq     xmm0,rcx
       jmp       short M00_L89
M00_L87:
       cmp       rdx,2
       jbe       short M00_L88
       vmovsd    xmm0,qword ptr [7FF9778A3BA0]
       jmp       short M00_L89
M00_L88:
       vmovq     xmm0,rdx
M00_L89:
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       rcx,7FFFFFFFFFFFFFFF
       vcvttsd2si rdx,xmm1
       vucomisd  xmm0,qword ptr [7FF9778A3B30]
       cmovb     rcx,rdx
       mov       [rsp+138],rcx
       jmp       short M00_L91
M00_L90:
       lea       rcx,[rsp+140]
       lea       rdx,[rsp+138]
       call      qword ptr [7FF977C25D88]
       test      eax,eax
       je        near ptr M00_L11
M00_L91:
       mov       edi,[rsp+138]
       mov       rdx,r13
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt32Array
       call      qword ptr [7FF977826850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       near ptr M00_L92
       mov       rdx,r13
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt8Array
       call      qword ptr [7FF977826850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       near ptr M00_L106
       mov       rdx,r13
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptFloat32Array
       call      qword ptr [7FF977826850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       near ptr M00_L120
       mov       rdx,r13
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptFloat64Array
       call      qword ptr [7FF977826850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       near ptr M00_L134
       mov       rdx,r13
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptBooleanArray
       call      qword ptr [7FF977826850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       near ptr M00_L148
       mov       rdx,r13
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptUInt8Array
       call      qword ptr [7FF977826850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       near ptr M00_L162
       mov       rdx,r13
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt16Array
       call      qword ptr [7FF977826850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       near ptr M00_L176
       mov       rdx,r13
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptUInt16Array
       call      qword ptr [7FF977826850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       near ptr M00_L190
       mov       rdx,r13
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptUInt32Array
       call      qword ptr [7FF977826850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       near ptr M00_L204
       mov       rdx,r13
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt64Array
       call      qword ptr [7FF977826850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       near ptr M00_L218
       mov       rdx,r13
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptUInt64Array
       call      qword ptr [7FF977826850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       near ptr M00_L234
       mov       rcx,r13
       mov       rax,[r13]
       mov       rax,[rax+58]
       call      qword ptr [rax+8]
       cmp       eax,edi
       ja        near ptr M00_L252
       jmp       near ptr M00_L251
M00_L92:
       mov       rdx,[rbp+38]
       cmp       [rdx+8],edi
       ja        short M00_L93
       mov       rdx,[rbp+38]
       mov       edx,[rdx+8]
       mov       ecx,edi
       call      qword ptr [7FF977C2C540]
M00_L93:
       mov       rbp,[rbp+38]
       xor       ecx,ecx
       mov       [rsp+120],rcx
       mov       [rsp+110],r14
       mov       [rsp+118],r15
       cmp       qword ptr [rsp+110],0
       je        short M00_L94
       mov       r14,[rsp+110]
       mov       rcx,offset MT_AuroraScript.Runtime.ScriptDatum
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0DA53000448
       cmp       r14,[rcx]
       je        short M00_L98
       mov       rcx,[rsp+110]
       mov       rdx,0DA53000450
       cmp       rcx,[rdx]
       je        short M00_L98
       movsx     rcx,word ptr [rsp+118]
       jmp       short M00_L97
M00_L94:
       mov       rcx,[rsp+118]
       test      rcx,rcx
       je        short M00_L95
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L96
       mov       ecx,2
       jmp       short M00_L97
M00_L95:
       xor       ecx,ecx
       jmp       short M00_L97
M00_L96:
       mov       ecx,1
M00_L97:
       test      ecx,ecx
       jne       short M00_L98
       xor       ecx,ecx
       mov       [rsp+120],rcx
       jmp       near ptr M00_L104
M00_L98:
       cmp       qword ptr [rsp+110],0
       jne       short M00_L102
       cmp       qword ptr [rsp+118],2
       jbe       short M00_L102
       mov       rcx,[rsp+118]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L99
       vmovq     xmm0,rcx
       jmp       short M00_L101
M00_L99:
       cmp       rdx,2
       jbe       short M00_L100
       vmovsd    xmm0,qword ptr [7FF9778A3BA0]
       jmp       short M00_L101
M00_L100:
       vmovq     xmm0,rdx
M00_L101:
       vmovsd    qword ptr [rsp+120],xmm0
       mov       r15d,1
       jmp       short M00_L103
M00_L102:
       lea       rcx,[rsp+110]
       lea       rdx,[rsp+120]
       call      qword ptr [7FF977C2C558]
       mov       r15d,eax
M00_L103:
       test      r15d,r15d
       jne       short M00_L104
       vmovsd    xmm0,qword ptr [7FF9778A3BA0]
       jmp       short M00_L105
M00_L104:
       vmovsd    xmm0,qword ptr [rsp+120]
M00_L105:
       cmp       edi,[rbp+8]
       jae       near ptr M00_L277
       mov       ecx,edi
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       eax,7FFFFFFF
       vcvttsd2si edx,xmm1
       vucomisd  xmm0,qword ptr [7FF9778A3BA8]
       cmovb     eax,edx
       mov       [rbp+rcx*4+10],eax
       jmp       near ptr M00_L06
M00_L106:
       mov       rdx,[rbp+38]
       cmp       [rdx+8],edi
       ja        short M00_L107
       mov       rdx,[rbp+38]
       mov       edx,[rdx+8]
       mov       ecx,edi
       call      qword ptr [7FF977C2C540]
M00_L107:
       mov       rbp,[rbp+38]
       xor       ecx,ecx
       mov       [rsp+108],rcx
       mov       [rsp+0F8],r14
       mov       [rsp+100],r15
       cmp       qword ptr [rsp+0F8],0
       je        short M00_L108
       mov       r14,[rsp+0F8]
       mov       rcx,offset MT_AuroraScript.Runtime.ScriptDatum
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0DA53000448
       cmp       r14,[rcx]
       je        short M00_L112
       mov       rcx,[rsp+0F8]
       mov       rdx,0DA53000450
       cmp       rcx,[rdx]
       je        short M00_L112
       movsx     rcx,word ptr [rsp+100]
       jmp       short M00_L111
M00_L108:
       mov       rcx,[rsp+100]
       test      rcx,rcx
       je        short M00_L109
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L110
       mov       ecx,2
       jmp       short M00_L111
M00_L109:
       xor       ecx,ecx
       jmp       short M00_L111
M00_L110:
       mov       ecx,1
M00_L111:
       test      ecx,ecx
       jne       short M00_L112
       xor       ecx,ecx
       mov       [rsp+108],rcx
       jmp       near ptr M00_L118
M00_L112:
       cmp       qword ptr [rsp+0F8],0
       jne       short M00_L116
       cmp       qword ptr [rsp+100],2
       jbe       short M00_L116
       mov       rcx,[rsp+100]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L113
       vmovq     xmm0,rcx
       jmp       short M00_L115
M00_L113:
       cmp       rdx,2
       jbe       short M00_L114
       vmovsd    xmm0,qword ptr [7FF9778A3BA0]
       jmp       short M00_L115
M00_L114:
       vmovq     xmm0,rdx
M00_L115:
       vmovsd    qword ptr [rsp+108],xmm0
       mov       r15d,1
       jmp       short M00_L117
M00_L116:
       lea       rcx,[rsp+0F8]
       lea       rdx,[rsp+108]
       call      qword ptr [7FF977C2C558]
       mov       r15d,eax
M00_L117:
       test      r15d,r15d
       jne       short M00_L118
       vmovsd    xmm0,qword ptr [7FF9778A3BA0]
       jmp       short M00_L119
M00_L118:
       vmovsd    xmm0,qword ptr [rsp+108]
M00_L119:
       cmp       edi,[rbp+8]
       jae       near ptr M00_L277
       mov       edx,edi
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       ecx,7FFFFFFF
       vcvttsd2si eax,xmm1
       vucomisd  xmm0,qword ptr [7FF9778A3BA8]
       cmovb     ecx,eax
       mov       [rbp+rdx+10],cl
       jmp       near ptr M00_L06
M00_L120:
       mov       rdx,[rbp+38]
       cmp       [rdx+8],edi
       ja        short M00_L121
       mov       rdx,[rbp+38]
       mov       edx,[rdx+8]
       mov       ecx,edi
       call      qword ptr [7FF977C2C540]
M00_L121:
       mov       rbp,[rbp+38]
       xor       ecx,ecx
       mov       [rsp+0F0],rcx
       mov       [rsp+0E0],r14
       mov       [rsp+0E8],r15
       cmp       qword ptr [rsp+0E0],0
       je        short M00_L122
       mov       r14,[rsp+0E0]
       mov       rcx,offset MT_AuroraScript.Runtime.ScriptDatum
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0DA53000448
       cmp       r14,[rcx]
       je        short M00_L126
       mov       rcx,[rsp+0E0]
       mov       rdx,0DA53000450
       cmp       rcx,[rdx]
       je        short M00_L126
       movsx     rcx,word ptr [rsp+0E8]
       jmp       short M00_L125
M00_L122:
       mov       rcx,[rsp+0E8]
       test      rcx,rcx
       je        short M00_L123
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L124
       mov       ecx,2
       jmp       short M00_L125
M00_L123:
       xor       ecx,ecx
       jmp       short M00_L125
M00_L124:
       mov       ecx,1
M00_L125:
       test      ecx,ecx
       jne       short M00_L126
       xor       ecx,ecx
       mov       [rsp+0F0],rcx
       jmp       near ptr M00_L132
M00_L126:
       cmp       qword ptr [rsp+0E0],0
       jne       short M00_L130
       cmp       qword ptr [rsp+0E8],2
       jbe       short M00_L130
       mov       rcx,[rsp+0E8]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L127
       vmovq     xmm0,rcx
       jmp       short M00_L129
M00_L127:
       cmp       rdx,2
       jbe       short M00_L128
       vmovsd    xmm0,qword ptr [7FF9778A3BA0]
       jmp       short M00_L129
M00_L128:
       vmovq     xmm0,rdx
M00_L129:
       vmovsd    qword ptr [rsp+0F0],xmm0
       mov       r15d,1
       jmp       short M00_L131
M00_L130:
       lea       rcx,[rsp+0E0]
       lea       rdx,[rsp+0F0]
       call      qword ptr [7FF977C2C558]
       mov       r15d,eax
M00_L131:
       test      r15d,r15d
       jne       short M00_L132
       vmovsd    xmm0,qword ptr [7FF9778A3BA0]
       jmp       short M00_L133
M00_L132:
       vmovsd    xmm0,qword ptr [rsp+0F0]
M00_L133:
       cmp       edi,[rbp+8]
       jae       near ptr M00_L277
       mov       edx,edi
       vcvtsd2ss xmm0,xmm0,xmm0
       vmovss    dword ptr [rbp+rdx*4+10],xmm0
       jmp       near ptr M00_L06
M00_L134:
       mov       rdx,[rbp+38]
       cmp       [rdx+8],edi
       ja        short M00_L135
       mov       rdx,[rbp+38]
       mov       edx,[rdx+8]
       mov       ecx,edi
       call      qword ptr [7FF977C2C540]
M00_L135:
       mov       rbp,[rbp+38]
       xor       ecx,ecx
       mov       [rsp+0D8],rcx
       mov       [rsp+0C8],r14
       mov       [rsp+0D0],r15
       cmp       qword ptr [rsp+0C8],0
       je        short M00_L136
       mov       r14,[rsp+0C8]
       mov       rcx,offset MT_AuroraScript.Runtime.ScriptDatum
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0DA53000448
       cmp       r14,[rcx]
       je        short M00_L140
       mov       rcx,[rsp+0C8]
       mov       rdx,0DA53000450
       cmp       rcx,[rdx]
       je        short M00_L140
       movsx     rcx,word ptr [rsp+0D0]
       jmp       short M00_L139
M00_L136:
       mov       rcx,[rsp+0D0]
       test      rcx,rcx
       je        short M00_L137
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L138
       mov       ecx,2
       jmp       short M00_L139
M00_L137:
       xor       ecx,ecx
       jmp       short M00_L139
M00_L138:
       mov       ecx,1
M00_L139:
       test      ecx,ecx
       jne       short M00_L140
       xor       ecx,ecx
       mov       [rsp+0D8],rcx
       jmp       near ptr M00_L146
M00_L140:
       cmp       qword ptr [rsp+0C8],0
       jne       short M00_L144
       cmp       qword ptr [rsp+0D0],2
       jbe       short M00_L144
       mov       rcx,[rsp+0D0]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L141
       vmovq     xmm0,rcx
       jmp       short M00_L143
M00_L141:
       cmp       rdx,2
       jbe       short M00_L142
       vmovsd    xmm0,qword ptr [7FF9778A3BA0]
       jmp       short M00_L143
M00_L142:
       vmovq     xmm0,rdx
M00_L143:
       vmovsd    qword ptr [rsp+0D8],xmm0
       mov       r15d,1
       jmp       short M00_L145
M00_L144:
       lea       rcx,[rsp+0C8]
       lea       rdx,[rsp+0D8]
       call      qword ptr [7FF977C2C558]
       mov       r15d,eax
M00_L145:
       test      r15d,r15d
       jne       short M00_L146
       vmovsd    xmm0,qword ptr [7FF9778A3BA0]
       jmp       short M00_L147
M00_L146:
       vmovsd    xmm0,qword ptr [rsp+0D8]
M00_L147:
       cmp       edi,[rbp+8]
       jae       near ptr M00_L277
       mov       edx,edi
       vmovsd    qword ptr [rbp+rdx*8+10],xmm0
       jmp       near ptr M00_L06
M00_L148:
       mov       rdx,[rbp+38]
       cmp       [rdx+8],edi
       ja        short M00_L149
       mov       rdx,[rbp+38]
       mov       edx,[rdx+8]
       mov       ecx,edi
       call      qword ptr [7FF977C2C540]
M00_L149:
       mov       rbp,[rbp+38]
       mov       [rsp+0B8],r14
       mov       [rsp+0C0],r15
       cmp       qword ptr [rsp+0B8],0
       je        short M00_L152
       mov       r14,[rsp+0B8]
       mov       rcx,offset MT_AuroraScript.Runtime.ScriptDatum
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rdx,0DA53000448
       cmp       r14,[rdx]
       jne       short M00_L150
       mov       eax,1000
       jmp       short M00_L155
M00_L150:
       mov       rdx,[rsp+0B8]
       mov       rcx,0DA53000450
       cmp       rdx,[rcx]
       jne       short M00_L151
       mov       eax,2000
       jmp       short M00_L155
M00_L151:
       movsx     rax,word ptr [rsp+0C0]
       jmp       short M00_L155
M00_L152:
       mov       rax,[rsp+0C0]
       test      rax,rax
       je        short M00_L153
       dec       rax
       cmp       rax,1
       jbe       short M00_L154
       mov       eax,2
       jmp       short M00_L155
M00_L153:
       xor       eax,eax
       jmp       short M00_L155
M00_L154:
       mov       eax,1
M00_L155:
       cmp       eax,4
       ja        short M00_L156
       mov       edx,eax
       lea       rcx,[7FF9778A3BB0]
       mov       ecx,[rcx+rdx*4]
       lea       rax,[M00_L01]
       add       rcx,rax
       jmp       rcx
M00_L156:
       cmp       eax,1000
       je        near ptr M00_L158
       cmp       eax,2000
       je        near ptr M00_L158
       lea       rcx,[rsp+0B8]
       call      qword ptr [7FF977C2C570]
       mov       r15,rax
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rdx,0DA53000408
       cmp       r15,[rdx]
       setne     r15b
       movzx     r15d,r15b
       jmp       near ptr M00_L161
       cmp       qword ptr [rsp+0C0],2
       sete      r15b
       movzx     r15d,r15b
       jmp       near ptr M00_L161
       mov       rcx,7FF8000000000001
       cmp       [rsp+0C0],rcx
       je        short M00_L157
       mov       rcx,8000000000000000
       cmp       [rsp+0C0],rcx
       je        short M00_L157
       mov       rcx,7FF8000000000004
       cmp       [rsp+0C0],rcx
       setne     r15b
       movzx     r15d,r15b
       jmp       short M00_L161
M00_L157:
       xor       r15d,r15d
       jmp       short M00_L161
M00_L158:
       cmp       qword ptr [rsp+0C0],0
       setne     r15b
       movzx     r15d,r15b
       jmp       short M00_L161
       mov       rdx,[rsp+0B8]
       mov       rcx,offset MT_System.String
       call      qword ptr [7FF977826850]; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rax,rax
       je        short M00_L159
       cmp       dword ptr [rax+8],0
       sete      cl
       movzx     ecx,cl
       jmp       short M00_L160
M00_L159:
       mov       ecx,1
M00_L160:
       test      ecx,ecx
       sete      r15b
       movzx     r15d,r15b
M00_L161:
       cmp       edi,[rbp+8]
       jae       near ptr M00_L277
       mov       edx,edi
       mov       [rbp+rdx+10],r15b
       jmp       near ptr M00_L06
M00_L162:
       mov       rdx,[rbp+38]
       cmp       [rdx+8],edi
       ja        short M00_L163
       mov       rdx,[rbp+38]
       mov       edx,[rdx+8]
       mov       ecx,edi
       call      qword ptr [7FF977C2C540]
M00_L163:
       mov       rbp,[rbp+38]
       xor       ecx,ecx
       mov       [rsp+0B0],rcx
       mov       [rsp+0A0],r14
       mov       [rsp+0A8],r15
       cmp       qword ptr [rsp+0A0],0
       je        short M00_L164
       mov       r14,[rsp+0A0]
       mov       rcx,offset MT_AuroraScript.Runtime.ScriptDatum
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0DA53000448
       cmp       r14,[rcx]
       je        short M00_L168
       mov       rcx,[rsp+0A0]
       mov       rdx,0DA53000450
       cmp       rcx,[rdx]
       je        short M00_L168
       movsx     rcx,word ptr [rsp+0A8]
       jmp       short M00_L167
M00_L164:
       mov       rcx,[rsp+0A8]
       test      rcx,rcx
       je        short M00_L165
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L166
       mov       ecx,2
       jmp       short M00_L167
M00_L165:
       xor       ecx,ecx
       jmp       short M00_L167
M00_L166:
       mov       ecx,1
M00_L167:
       test      ecx,ecx
       jne       short M00_L168
       xor       ecx,ecx
       mov       [rsp+0B0],rcx
       jmp       near ptr M00_L174
M00_L168:
       cmp       qword ptr [rsp+0A0],0
       jne       short M00_L172
       cmp       qword ptr [rsp+0A8],2
       jbe       short M00_L172
       mov       rcx,[rsp+0A8]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L169
       vmovq     xmm0,rcx
       jmp       short M00_L171
M00_L169:
       cmp       rdx,2
       jbe       short M00_L170
       vmovsd    xmm0,qword ptr [7FF9778A3BA0]
       jmp       short M00_L171
M00_L170:
       vmovq     xmm0,rdx
M00_L171:
       vmovsd    qword ptr [rsp+0B0],xmm0
       mov       r15d,1
       jmp       short M00_L173
M00_L172:
       lea       rcx,[rsp+0A0]
       lea       rdx,[rsp+0B0]
       call      qword ptr [7FF977C2C558]
       mov       r15d,eax
M00_L173:
       test      r15d,r15d
       jne       short M00_L174
       vmovsd    xmm0,qword ptr [7FF9778A3BA0]
       jmp       short M00_L175
M00_L174:
       vmovsd    xmm0,qword ptr [rsp+0B0]
M00_L175:
       cmp       edi,[rbp+8]
       jae       near ptr M00_L277
       mov       edx,edi
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       ecx,7FFFFFFF
       vcvttsd2si eax,xmm1
       vucomisd  xmm0,qword ptr [7FF9778A3BA8]
       cmovb     ecx,eax
       mov       [rbp+rdx+10],cl
       jmp       near ptr M00_L06
M00_L176:
       mov       rdx,[rbp+38]
       cmp       [rdx+8],edi
       ja        short M00_L177
       mov       rdx,[rbp+38]
       mov       edx,[rdx+8]
       mov       ecx,edi
       call      qword ptr [7FF977C2C540]
M00_L177:
       mov       rbp,[rbp+38]
       xor       ecx,ecx
       mov       [rsp+98],rcx
       mov       [rsp+88],r14
       mov       [rsp+90],r15
       cmp       qword ptr [rsp+88],0
       je        short M00_L178
       mov       r14,[rsp+88]
       mov       rcx,offset MT_AuroraScript.Runtime.ScriptDatum
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0DA53000448
       cmp       r14,[rcx]
       je        short M00_L182
       mov       rcx,[rsp+88]
       mov       rdx,0DA53000450
       cmp       rcx,[rdx]
       je        short M00_L182
       movsx     rcx,word ptr [rsp+90]
       jmp       short M00_L181
M00_L178:
       mov       rcx,[rsp+90]
       test      rcx,rcx
       je        short M00_L179
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L180
       mov       ecx,2
       jmp       short M00_L181
M00_L179:
       xor       ecx,ecx
       jmp       short M00_L181
M00_L180:
       mov       ecx,1
M00_L181:
       test      ecx,ecx
       jne       short M00_L182
       xor       ecx,ecx
       mov       [rsp+98],rcx
       jmp       near ptr M00_L188
M00_L182:
       cmp       qword ptr [rsp+88],0
       jne       short M00_L186
       cmp       qword ptr [rsp+90],2
       jbe       short M00_L186
       mov       rcx,[rsp+90]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L183
       vmovq     xmm0,rcx
       jmp       short M00_L185
M00_L183:
       cmp       rdx,2
       jbe       short M00_L184
       vmovsd    xmm0,qword ptr [7FF9778A3BA0]
       jmp       short M00_L185
M00_L184:
       vmovq     xmm0,rdx
M00_L185:
       vmovsd    qword ptr [rsp+98],xmm0
       mov       r15d,1
       jmp       short M00_L187
M00_L186:
       lea       rcx,[rsp+88]
       lea       rdx,[rsp+98]
       call      qword ptr [7FF977C2C558]
       mov       r15d,eax
M00_L187:
       test      r15d,r15d
       jne       short M00_L188
       vmovsd    xmm0,qword ptr [7FF9778A3BA0]
       jmp       short M00_L189
M00_L188:
       vmovsd    xmm0,qword ptr [rsp+98]
M00_L189:
       cmp       edi,[rbp+8]
       jae       near ptr M00_L277
       mov       edx,edi
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       ecx,7FFFFFFF
       vcvttsd2si eax,xmm1
       vucomisd  xmm0,qword ptr [7FF9778A3BA8]
       cmovb     ecx,eax
       mov       [rbp+rdx*2+10],cx
       jmp       near ptr M00_L06
M00_L190:
       mov       rdx,[rbp+38]
       cmp       [rdx+8],edi
       ja        short M00_L191
       mov       rdx,[rbp+38]
       mov       edx,[rdx+8]
       mov       ecx,edi
       call      qword ptr [7FF977C2C540]
M00_L191:
       mov       rbp,[rbp+38]
       xor       ecx,ecx
       mov       [rsp+80],rcx
       mov       [rsp+70],r14
       mov       [rsp+78],r15
       cmp       qword ptr [rsp+70],0
       je        short M00_L192
       mov       r14,[rsp+70]
       mov       rcx,offset MT_AuroraScript.Runtime.ScriptDatum
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0DA53000448
       cmp       r14,[rcx]
       je        short M00_L196
       mov       rcx,[rsp+70]
       mov       rdx,0DA53000450
       cmp       rcx,[rdx]
       je        short M00_L196
       movsx     rcx,word ptr [rsp+78]
       jmp       short M00_L195
M00_L192:
       mov       rcx,[rsp+78]
       test      rcx,rcx
       je        short M00_L193
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L194
       mov       ecx,2
       jmp       short M00_L195
M00_L193:
       xor       ecx,ecx
       jmp       short M00_L195
M00_L194:
       mov       ecx,1
M00_L195:
       test      ecx,ecx
       jne       short M00_L196
       xor       ecx,ecx
       mov       [rsp+80],rcx
       jmp       short M00_L202
M00_L196:
       cmp       qword ptr [rsp+70],0
       jne       short M00_L200
       cmp       qword ptr [rsp+78],2
       jbe       short M00_L200
       mov       rcx,[rsp+78]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L197
       vmovq     xmm0,rcx
       jmp       short M00_L199
M00_L197:
       cmp       rdx,2
       jbe       short M00_L198
       vmovsd    xmm0,qword ptr [7FF9778A3BA0]
       jmp       short M00_L199
M00_L198:
       vmovq     xmm0,rdx
M00_L199:
       vmovsd    qword ptr [rsp+80],xmm0
       mov       r15d,1
       jmp       short M00_L201
M00_L200:
       lea       rcx,[rsp+70]
       lea       rdx,[rsp+80]
       call      qword ptr [7FF977C2C558]
       mov       r15d,eax
M00_L201:
       test      r15d,r15d
       jne       short M00_L202
       vmovsd    xmm0,qword ptr [7FF9778A3BA0]
       jmp       short M00_L203
M00_L202:
       vmovsd    xmm0,qword ptr [rsp+80]
M00_L203:
       cmp       edi,[rbp+8]
       jae       near ptr M00_L277
       mov       edx,edi
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       ecx,7FFFFFFF
       vcvttsd2si eax,xmm1
       vucomisd  xmm0,qword ptr [7FF9778A3BA8]
       cmovb     ecx,eax
       mov       [rbp+rdx*2+10],cx
       jmp       near ptr M00_L06
M00_L204:
       mov       rdx,[rbp+38]
       cmp       [rdx+8],edi
       ja        short M00_L205
       mov       rdx,[rbp+38]
       mov       edx,[rdx+8]
       mov       ecx,edi
       call      qword ptr [7FF977C2C540]
M00_L205:
       mov       rbp,[rbp+38]
       xor       ecx,ecx
       mov       [rsp+68],rcx
       mov       [rsp+58],r14
       mov       [rsp+60],r15
       cmp       qword ptr [rsp+58],0
       je        short M00_L206
       mov       r14,[rsp+58]
       mov       rcx,offset MT_AuroraScript.Runtime.ScriptDatum
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0DA53000448
       cmp       r14,[rcx]
       je        short M00_L210
       mov       rcx,[rsp+58]
       mov       rdx,0DA53000450
       cmp       rcx,[rdx]
       je        short M00_L210
       movsx     rcx,word ptr [rsp+60]
       jmp       short M00_L209
M00_L206:
       mov       rcx,[rsp+60]
       test      rcx,rcx
       je        short M00_L207
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L208
       mov       ecx,2
       jmp       short M00_L209
M00_L207:
       xor       ecx,ecx
       jmp       short M00_L209
M00_L208:
       mov       ecx,1
M00_L209:
       test      ecx,ecx
       jne       short M00_L210
       xor       ecx,ecx
       mov       [rsp+68],rcx
       jmp       short M00_L216
M00_L210:
       cmp       qword ptr [rsp+58],0
       jne       short M00_L214
       cmp       qword ptr [rsp+60],2
       jbe       short M00_L214
       mov       rcx,[rsp+60]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L211
       vmovq     xmm0,rcx
       jmp       short M00_L213
M00_L211:
       cmp       rdx,2
       jbe       short M00_L212
       vmovsd    xmm0,qword ptr [7FF9778A3BA0]
       jmp       short M00_L213
M00_L212:
       vmovq     xmm0,rdx
M00_L213:
       vmovsd    qword ptr [rsp+68],xmm0
       mov       r15d,1
       jmp       short M00_L215
M00_L214:
       lea       rcx,[rsp+58]
       lea       rdx,[rsp+68]
       call      qword ptr [7FF977C2C558]
       mov       r15d,eax
M00_L215:
       test      r15d,r15d
       jne       short M00_L216
       vmovsd    xmm0,qword ptr [7FF9778A3BA0]
       jmp       short M00_L217
M00_L216:
       vmovsd    xmm0,qword ptr [rsp+68]
M00_L217:
       cmp       edi,[rbp+8]
       jae       near ptr M00_L277
       mov       edx,edi
       vxorps    xmm1,xmm1,xmm1
       vmaxsd    xmm1,xmm0,xmm1
       mov       ecx,0FFFFFFFF
       vcvttsd2si rax,xmm1
       vucomisd  xmm0,qword ptr [7FF9778A3BC8]
       cmovb     ecx,eax
       mov       [rbp+rdx*4+10],ecx
       jmp       near ptr M00_L06
M00_L218:
       mov       rdx,[rbp+38]
       cmp       [rdx+8],edi
       ja        short M00_L219
       mov       rdx,[rbp+38]
       mov       edx,[rdx+8]
       mov       ecx,edi
       call      qword ptr [7FF977C2C540]
M00_L219:
       mov       rbp,[rbp+38]
       test      r14,r14
       je        short M00_L222
       mov       rcx,offset MT_AuroraScript.Runtime.ScriptDatum
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       r8,0DA53000448
       cmp       r14,[r8]
       jne       short M00_L220
       mov       r8d,1000
       jmp       short M00_L225
M00_L220:
       mov       r8,0DA53000450
       cmp       r14,[r8]
       jne       short M00_L221
       mov       r8d,2000
       jmp       short M00_L225
M00_L221:
       movsx     r8,r15w
       jmp       short M00_L225
M00_L222:
       test      r15,r15
       je        short M00_L223
       lea       r8,[r15-1]
       cmp       r8,1
       jbe       short M00_L224
       mov       r8d,2
       jmp       short M00_L225
M00_L223:
       xor       r8d,r8d
       jmp       short M00_L225
M00_L224:
       mov       r8d,1
M00_L225:
       cmp       r8d,2
       je        short M00_L227
       cmp       r8d,1000
       je        short M00_L226
       cmp       r8d,2000
       jne       near ptr M00_L232
       test      r15,r15
       jl        near ptr M00_L232
M00_L226:
       mov       rax,r15
       jmp       near ptr M00_L233
M00_L227:
       mov       rax,8007FFFFFFFFFFFF
       add       rax,r15
       cmp       rax,3
       jbe       short M00_L228
       vmovq     xmm0,r15
       jmp       short M00_L230
M00_L228:
       cmp       rax,2
       jbe       short M00_L229
       vmovsd    xmm0,qword ptr [7FF9778A3BA0]
       jmp       short M00_L230
M00_L229:
       vmovq     xmm0,rax
M00_L230:
       vucomisd  xmm0,qword ptr [7FF9778A3BD0]
       jb        short M00_L232
       vmovsd    xmm1,qword ptr [7FF9778A3B30]
       vucomisd  xmm1,xmm0
       jbe       short M00_L232
       vroundsd  xmm1,xmm1,xmm0,0B
       vucomisd  xmm1,xmm0
       jp        short M00_L232
       jne       short M00_L232
       vxorps    xmm1,xmm1,xmm1
       vucomisd  xmm0,xmm1
       jp        short M00_L231
       jne       short M00_L231
       vmovq     r8,xmm0
       not       r8
       shr       r8,3F
       test      r8d,r8d
       je        short M00_L232
M00_L231:
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       r8,7FFFFFFFFFFFFFFF
       vcvttsd2si rcx,xmm1
       vucomisd  xmm0,qword ptr [7FF9778A3B30]
       cmovb     r8,rcx
       mov       rax,r8
       jmp       short M00_L233
M00_L232:
       mov       [rsp+28],r14
       mov       [rsp+30],r15
       lea       r8,[rsp+28]
       lea       rcx,[rsp+48]
       mov       edx,13
       call      qword ptr [7FF977C2C588]
       xor       eax,eax
M00_L233:
       cmp       edi,[rbp+8]
       jae       near ptr M00_L277
       mov       edx,edi
       mov       [rbp+rdx*8+10],rax
       jmp       near ptr M00_L06
M00_L234:
       mov       rdx,[rbp+38]
       cmp       [rdx+8],edi
       ja        short M00_L235
       mov       rdx,[rbp+38]
       mov       edx,[rdx+8]
       mov       ecx,edi
       call      qword ptr [7FF977C2C540]
M00_L235:
       mov       rbp,[rbp+38]
       test      r14,r14
       je        short M00_L238
       mov       rcx,offset MT_AuroraScript.Runtime.ScriptDatum
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       r8,0DA53000448
       cmp       r14,[r8]
       jne       short M00_L236
       mov       r8d,1000
       jmp       short M00_L241
M00_L236:
       mov       r8,0DA53000450
       cmp       r14,[r8]
       jne       short M00_L237
       mov       r8d,2000
       jmp       short M00_L241
M00_L237:
       movsx     r8,r15w
       jmp       short M00_L241
M00_L238:
       test      r15,r15
       je        short M00_L239
       lea       r8,[r15-1]
       cmp       r8,1
       jbe       short M00_L240
       mov       r8d,2
       jmp       short M00_L241
M00_L239:
       xor       r8d,r8d
       jmp       short M00_L241
M00_L240:
       mov       r8d,1
M00_L241:
       cmp       r8d,2
       je        short M00_L244
       cmp       r8d,1000
       je        short M00_L242
       cmp       r8d,2000
       jne       near ptr M00_L249
       jmp       short M00_L243
M00_L242:
       test      r15,r15
       jl        near ptr M00_L249
M00_L243:
       jmp       near ptr M00_L250
M00_L244:
       mov       r8,8007FFFFFFFFFFFF
       add       r8,r15
       cmp       r8,3
       jbe       short M00_L245
       vmovq     xmm0,r15
       jmp       short M00_L247
M00_L245:
       cmp       r8,2
       jbe       short M00_L246
       vmovsd    xmm0,qword ptr [7FF9778A3BA0]
       jmp       short M00_L247
M00_L246:
       vmovq     xmm0,r8
M00_L247:
       vxorps    xmm1,xmm1,xmm1
       vucomisd  xmm0,xmm1
       jb        short M00_L249
       vmovsd    xmm1,qword ptr [7FF9778A3BD8]
       vucomisd  xmm1,xmm0
       jbe       short M00_L249
       vroundsd  xmm1,xmm1,xmm0,0B
       vucomisd  xmm1,xmm0
       jp        short M00_L249
       jne       short M00_L249
       vxorps    xmm1,xmm1,xmm1
       vucomisd  xmm0,xmm1
       jp        short M00_L248
       jne       short M00_L248
       vmovq     r8,xmm0
       not       r8
       shr       r8,3F
       test      r8d,r8d
       je        short M00_L249
M00_L248:
       vxorps    xmm1,xmm1,xmm1
       vmaxsd    xmm1,xmm0,xmm1
       vmovsd    xmm2,qword ptr [7FF9778A3BD8]
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
       jmp       short M00_L250
M00_L249:
       mov       [rsp+28],r14
       mov       [rsp+30],r15
       lea       r8,[rsp+28]
       lea       rcx,[rsp+38]
       mov       edx,14
       call      qword ptr [7FF977C2C588]
       xor       r15d,r15d
M00_L250:
       cmp       edi,[rbp+8]
       jae       near ptr M00_L277
       mov       edx,edi
       mov       [rbp+rdx*8+10],r15
       jmp       near ptr M00_L06
M00_L251:
       mov       rcx,r13
       mov       rax,[r13]
       mov       rax,[rax+58]
       call      qword ptr [rax+8]
       mov       edx,eax
       mov       ecx,edi
       call      qword ptr [7FF977C2C540]
M00_L252:
       mov       [rsp+28],r14
       mov       [rsp+30],r15
       lea       r8,[rsp+28]
       mov       edx,edi
       mov       rcx,r13
       mov       rax,[r13]
       mov       rax,[rax+58]
       call      qword ptr [rax+18]
       jmp       near ptr M00_L06
M00_L253:
       mov       rcx,offset MT_AuroraScript.Runtime.ScriptDatum
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       jmp       near ptr M00_L12
M00_L254:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.UInt64Value+NativePrototypeHolder
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       jmp       near ptr M00_L78
M00_L255:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       jmp       near ptr M00_L27
M00_L256:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BooleanValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       jmp       near ptr M00_L73
M00_L257:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BooleanValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       jmp       near ptr M00_L76
M00_L258:
       vmovsd    xmm6,qword ptr [7FF9778A3BA0]
       jmp       near ptr M00_L35
M00_L259:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       jmp       near ptr M00_L71
M00_L260:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       jmp       near ptr M00_L69
M00_L261:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       jmp       near ptr M00_L67
M00_L262:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       jmp       near ptr M00_L65
M00_L263:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       jmp       near ptr M00_L63
M00_L264:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       jmp       near ptr M00_L61
M00_L265:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       jmp       near ptr M00_L59
M00_L266:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       jmp       near ptr M00_L57
M00_L267:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       jmp       near ptr M00_L55
M00_L268:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       jmp       near ptr M00_L53
M00_L269:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       jmp       near ptr M00_L51
M00_L270:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       jmp       near ptr M00_L46
M00_L271:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue+NativePrototypeHolder
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       jmp       near ptr M00_L49
M00_L272:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.Int64Value+NativePrototypeHolder
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       jmp       near ptr M00_L80
M00_L273:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       jmp       near ptr M00_L19
M00_L274:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue+NativePrototypeHolder
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       jmp       near ptr M00_L22
M00_L275:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       jmp       near ptr M00_L29
M00_L276:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue+NativePrototypeHolder
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       jmp       near ptr M00_L32
M00_L277:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 7696
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
       mov       rcx,7FF977C88ABC
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rax,rsi
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
M04_L00:
       mov       rcx,7FF977C88AB8
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
       sub       rsp,60
       vmovaps   [rsp+50],xmm6
       vxorps    xmm4,xmm4,xmm4
       vmovdqu   ymmword ptr [rsp+20],ymm4
       vmovdqa   xmmword ptr [rsp+40],xmm4
       mov       rbx,rcx
       vxorps    xmm6,xmm6,xmm6
       mov       esi,200
       jmp       near ptr M00_L05
M00_L00:
       xor       ecx,ecx
M00_L01:
       test      rcx,rcx
       jne       near ptr M00_L08
       mov       r14,rdi
       test      r14,r14
       je        short M00_L02
       mov       rdx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       cmp       [r14],rdx
       jne       near ptr M00_L09
       xor       r14d,r14d
M00_L02:
       test      r14,r14
       jne       near ptr M00_L10
       mov       [rsp+20],rdi
       mov       [rsp+28],rbp
       lea       rdx,[rsp+20]
       lea       rcx,[rsp+40]
       vmovsd    xmm2,qword ptr [7FF9778A98A8]
       call      qword ptr [7FF977C25D58]; AuroraScript.Runtime.ObjectOps.GetNumericProperty(AuroraScript.Runtime.ScriptDatum, Double)
M00_L03:
       mov       rax,[rsp+48]
       mov       rcx,8007FFFFFFFFFFFF
       add       rcx,rax
       cmp       rcx,3
       jbe       near ptr M00_L58
       vmovq     xmm0,rax
M00_L04:
       vaddsd    xmm6,xmm6,xmm0
       dec       esi
       je        short M00_L06
M00_L05:
       mov       rdi,[rbx+68]
       mov       rbp,[rbx+70]
       mov       rcx,rdi
       test      rcx,rcx
       je        near ptr M00_L01
       mov       rdx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       cmp       [rcx],rdx
       je        near ptr M00_L00
       jmp       short M00_L07
M00_L06:
       vmovaps   xmm0,xmm6
       vmovaps   xmm6,[rsp+50]
       add       rsp,60
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       ret
M00_L07:
       mov       rdx,rdi
       mov       rcx,offset MT_AuroraScript.Runtime.Types.IAuroraNativeIndexer
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfInterface(Void*, System.Object)
       mov       rcx,rax
       jmp       near ptr M00_L01
M00_L08:
       lea       rdx,[rsp+40]
       mov       r11,7FF9777704E0
       mov       r8d,1
       call      qword ptr [r11]
       jmp       near ptr M00_L03
M00_L09:
       mov       rdx,rdi
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptPackedArray
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r14,rax
       jmp       near ptr M00_L02
M00_L10:
       mov       rdx,r14
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt32Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       near ptr M00_L11
       mov       rdx,r14
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt8Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       near ptr M00_L15
       mov       rdx,r14
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptFloat32Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       near ptr M00_L19
       mov       rdx,r14
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptFloat64Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       near ptr M00_L25
       mov       rdx,r14
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptBooleanArray
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       near ptr M00_L31
       mov       rdx,r14
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptUInt8Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       near ptr M00_L35
       mov       rdx,r14
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt16Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       near ptr M00_L39
       mov       rdx,r14
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptUInt16Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       near ptr M00_L43
       mov       rdx,r14
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptUInt32Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       near ptr M00_L47
       mov       rdx,r14
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt64Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       near ptr M00_L51
       mov       rdx,r14
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptUInt64Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       near ptr M00_L53
       mov       rcx,r14
       mov       rax,[r14]
       mov       rax,[rax+58]
       call      qword ptr [rax+8]
       cmp       eax,1
       ja        near ptr M00_L56
       jmp       near ptr M00_L55
M00_L11:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jg        short M00_L12
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,1
       call      qword ptr [7FF977C2D458]
M00_L12:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jbe       near ptr M00_L60
       mov       edx,[rdx+14]
       test      edx,edx
       je        short M00_L13
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2sd xmm0,xmm0,edx
       vmovq     r14,xmm0
       jmp       short M00_L14
M00_L13:
       mov       r14,7FF8000000000001
M00_L14:
       xor       ebp,ebp
       jmp       near ptr M00_L57
M00_L15:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jg        short M00_L16
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,1
       call      qword ptr [7FF977C2D458]
M00_L16:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jbe       near ptr M00_L60
       movsx     rdx,byte ptr [rdx+11]
       test      edx,edx
       je        short M00_L17
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2sd xmm0,xmm0,edx
       vmovq     r14,xmm0
       jmp       short M00_L18
M00_L17:
       mov       r14,7FF8000000000001
M00_L18:
       xor       ebp,ebp
       jmp       near ptr M00_L57
M00_L19:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jg        short M00_L20
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,1
       call      qword ptr [7FF977C2D458]
M00_L20:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jbe       near ptr M00_L60
       vcvtss2sd xmm0,xmm0,dword ptr [rdx+14]
       vucomisd  xmm0,xmm0
       jp        short M00_L21
       je        short M00_L22
M00_L21:
       mov       r14,7FF8000000000004
       jmp       short M00_L24
M00_L22:
       vmovq     r14,xmm0
       cmp       r14,2
       jbe       short M00_L23
       jmp       short M00_L24
M00_L23:
       mov       rdx,7FF8000000000001
       add       r14,rdx
M00_L24:
       xor       ebp,ebp
       jmp       near ptr M00_L57
M00_L25:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jg        short M00_L26
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,1
       call      qword ptr [7FF977C2D458]
M00_L26:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jbe       near ptr M00_L60
       vmovsd    xmm0,qword ptr [rdx+18]
       vucomisd  xmm0,xmm0
       jp        short M00_L27
       je        short M00_L28
M00_L27:
       mov       r14,7FF8000000000004
       jmp       short M00_L30
M00_L28:
       vmovq     r14,xmm0
       cmp       r14,2
       jbe       short M00_L29
       jmp       short M00_L30
M00_L29:
       mov       rdx,7FF8000000000001
       add       r14,rdx
M00_L30:
       xor       ebp,ebp
       jmp       near ptr M00_L57
M00_L31:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jg        short M00_L32
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,1
       call      qword ptr [7FF977C2D458]
M00_L32:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jbe       near ptr M00_L60
       movzx     edx,byte ptr [rdx+11]
       test      edx,edx
       jne       short M00_L33
       mov       r14d,1
       jmp       short M00_L34
M00_L33:
       mov       r14d,2
M00_L34:
       xor       ebp,ebp
       jmp       near ptr M00_L57
M00_L35:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jg        short M00_L36
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,1
       call      qword ptr [7FF977C2D458]
M00_L36:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jbe       near ptr M00_L60
       movzx     edx,byte ptr [rdx+11]
       test      edx,edx
       je        short M00_L37
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2sd xmm0,xmm0,edx
       vmovq     r14,xmm0
       jmp       short M00_L38
M00_L37:
       mov       r14,7FF8000000000001
M00_L38:
       xor       ebp,ebp
       jmp       near ptr M00_L57
M00_L39:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jg        short M00_L40
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,1
       call      qword ptr [7FF977C2D458]
M00_L40:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jbe       near ptr M00_L60
       movsx     rdx,word ptr [rdx+12]
       test      edx,edx
       je        short M00_L41
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2sd xmm0,xmm0,edx
       vmovq     r14,xmm0
       jmp       short M00_L42
M00_L41:
       mov       r14,7FF8000000000001
M00_L42:
       xor       ebp,ebp
       jmp       near ptr M00_L57
M00_L43:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jg        short M00_L44
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,1
       call      qword ptr [7FF977C2D458]
M00_L44:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jbe       near ptr M00_L60
       movzx     edx,word ptr [rdx+12]
       test      edx,edx
       je        short M00_L45
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2sd xmm0,xmm0,edx
       vmovq     r14,xmm0
       jmp       short M00_L46
M00_L45:
       mov       r14,7FF8000000000001
M00_L46:
       xor       ebp,ebp
       jmp       near ptr M00_L57
M00_L47:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jg        short M00_L48
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,1
       call      qword ptr [7FF977C2D458]
M00_L48:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jbe       near ptr M00_L60
       mov       edx,[rdx+14]
       test      edx,edx
       je        short M00_L49
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2sd xmm0,xmm0,rdx
       vmovq     r14,xmm0
       jmp       short M00_L50
M00_L49:
       mov       r14,7FF8000000000001
M00_L50:
       xor       ebp,ebp
       jmp       near ptr M00_L57
M00_L51:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jg        short M00_L52
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,1
       call      qword ptr [7FF977C2D458]
M00_L52:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jbe       near ptr M00_L60
       mov       r14,[rdx+18]
       mov       rdx,0C20B400450
       mov       rbp,[rdx]
       jmp       short M00_L57
M00_L53:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jg        short M00_L54
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,1
       call      qword ptr [7FF977C2D458]
M00_L54:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],1
       jbe       short M00_L60
       mov       r14,[rdx+18]
       mov       rdx,0C20B400458
       mov       rbp,[rdx]
       jmp       short M00_L57
M00_L55:
       mov       rcx,r14
       mov       rax,[r14]
       mov       rax,[rax+58]
       call      qword ptr [rax+8]
       mov       edx,eax
       mov       ecx,1
       call      qword ptr [7FF977C2D458]
M00_L56:
       lea       rdx,[rsp+30]
       mov       rcx,r14
       mov       r8d,1
       mov       rax,[r14]
       mov       rax,[rax+58]
       call      qword ptr [rax+10]
       mov       rbp,[rsp+30]
       mov       r14,[rsp+38]
M00_L57:
       mov       [rsp+40],rbp
       mov       [rsp+48],r14
       jmp       near ptr M00_L03
M00_L58:
       cmp       rcx,2
       jbe       short M00_L59
       vmovsd    xmm0,qword ptr [7FF9778A98B0]
       jmp       near ptr M00_L04
M00_L59:
       vmovq     xmm0,rcx
       jmp       near ptr M00_L04
M00_L60:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 1666
```
```assembly
; AuroraScript.Runtime.ObjectOps.GetNumericProperty(AuroraScript.Runtime.ScriptDatum, Double)
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,40
       xor       eax,eax
       mov       [rsp+30],rax
       vmovsd    qword ptr [rsp+70],xmm2
       mov       rbx,rcx
M01_L00:
       vmovdqu   xmm1,xmmword ptr [rdx]
       vmovdqu   xmmword ptr [rsp+30],xmm1
       cmp       qword ptr [rsp+30],0
       je        near ptr M01_L07
       mov       rcx,[rsp+30]
       mov       rax,0C20B400450
       cmp       rcx,[rax]
       je        near ptr M01_L06
       mov       rcx,[rsp+30]
       mov       rax,0C20B400458
       movsx     rdx,word ptr [rsp+38]
       mov       r8d,2000
       cmp       rcx,[rax]
       cmove     edx,r8d
M01_L01:
       cmp       edx,4
       jbe       near ptr M01_L05
       cmp       edx,1000
       je        near ptr M01_L30
       cmp       edx,2000
       je        near ptr M01_L31
       mov       rdx,[rsp+30]
       mov       rsi,rdx
       test      rsi,rsi
       je        short M01_L02
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       cmp       [rsi],rcx
       jne       near ptr M01_L10
M01_L02:
       test      rsi,rsi
       je        near ptr M01_L32
M01_L03:
       call      qword ptr [7FF97782D2A8]; System.Globalization.NumberFormatInfo.get_CurrentInfo()
       mov       r8,rax
       vmovsd    xmm0,qword ptr [rsp+70]
       xor       edx,edx
       call      qword ptr [7FF977BA7120]; System.Number.FormatFloat[[System.Double, System.Private.CoreLib]](Double, System.String, System.Globalization.NumberFormatInfo)
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       cmp       [rsi],rcx
       jne       near ptr M01_L36
       mov       rcx,rsi
       mov       rdx,rbx
       mov       r9,rax
       xor       r8d,r8d
       call      qword ptr [7FF977C25F68]; AuroraScript.Runtime.Types.ScriptObject.InternalGetPropertyDatum(AuroraScript.Runtime.ScriptContext, System.String)
M01_L04:
       mov       rax,rbx
       add       rsp,40
       pop       rbx
       pop       rsi
       pop       rdi
       ret
M01_L05:
       mov       ecx,edx
       lea       rax,[7FF9778A8C00]
       mov       eax,[rax+rcx*4]
       lea       rdx,[M01_L00]
       add       rax,rdx
       jmp       rax
M01_L06:
       mov       edx,1000
       jmp       near ptr M01_L01
M01_L07:
       mov       rdx,[rsp+38]
       test      rdx,rdx
       je        short M01_L09
       dec       rdx
       mov       ecx,1
       mov       eax,2
       cmp       rdx,1
       cmova     ecx,eax
       mov       edx,ecx
M01_L08:
       jmp       near ptr M01_L01
M01_L09:
       xor       edx,edx
       jmp       short M01_L08
M01_L10:
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rsi,rax
       jmp       near ptr M01_L02
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rax,0C20B400410
       mov       rsi,[rax]
       jmp       near ptr M01_L03
       cmp       qword ptr [rsp+38],2
       je        short M01_L11
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BooleanValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0C20B401518
       mov       rsi,[rcx]
       jmp       short M01_L12
M01_L11:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BooleanValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0C20B401510
       mov       rsi,[rcx]
M01_L12:
       jmp       near ptr M01_L03
       mov       rcx,[rsp+38]
       mov       rax,8007FFFFFFFFFFFF
       add       rax,rcx
       cmp       rax,3
       jbe       short M01_L13
       vmovq     xmm1,rcx
       jmp       short M01_L15
M01_L13:
       cmp       rax,2
       jbe       short M01_L14
       vmovsd    xmm1,qword ptr [7FF9778A8C18]
       jmp       short M01_L15
M01_L14:
       vmovq     xmm1,rax
M01_L15:
       vucomisd  xmm1,xmm1
       jp        short M01_L16
       je        short M01_L17
M01_L16:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0C20B401500
       mov       rsi,[rcx]
       jmp       near ptr M01_L29
M01_L17:
       vxorps    xmm2,xmm2,xmm2
       vucomisd  xmm1,xmm2
       jp        short M01_L18
       jne       short M01_L18
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0C20B4014B0
       mov       rsi,[rcx]
       jmp       near ptr M01_L29
M01_L18:
       vucomisd  xmm1,qword ptr [7FF9778A8C20]
       jp        short M01_L19
       jne       short M01_L19
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0C20B4014B8
       mov       rsi,[rcx]
       jmp       near ptr M01_L29
M01_L19:
       vucomisd  xmm1,qword ptr [7FF9778A8C28]
       jp        short M01_L20
       jne       short M01_L20
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0C20B4014C0
       mov       rsi,[rcx]
       jmp       near ptr M01_L29
M01_L20:
       vucomisd  xmm1,qword ptr [7FF9778A8C30]
       jp        short M01_L21
       jne       short M01_L21
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0C20B4014C8
       mov       rsi,[rcx]
       jmp       near ptr M01_L29
M01_L21:
       vucomisd  xmm1,qword ptr [7FF9778A8C38]
       jp        short M01_L22
       jne       short M01_L22
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0C20B4014D0
       mov       rsi,[rcx]
       jmp       near ptr M01_L29
M01_L22:
       vucomisd  xmm1,qword ptr [7FF9778A8C40]
       jp        short M01_L23
       jne       short M01_L23
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0C20B4014D8
       mov       rsi,[rcx]
       jmp       near ptr M01_L29
M01_L23:
       vucomisd  xmm1,qword ptr [7FF9778A8C48]
       jp        short M01_L24
       jne       short M01_L24
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0C20B4014E0
       mov       rsi,[rcx]
       jmp       near ptr M01_L29
M01_L24:
       vucomisd  xmm1,qword ptr [7FF9778A8C50]
       jp        short M01_L25
       jne       short M01_L25
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0C20B4014E8
       mov       rsi,[rcx]
       jmp       near ptr M01_L29
M01_L25:
       vucomisd  xmm1,qword ptr [7FF9778A8C58]
       jp        short M01_L26
       jne       short M01_L26
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0C20B4014F0
       mov       rsi,[rcx]
       jmp       near ptr M01_L29
M01_L26:
       vucomisd  xmm1,qword ptr [7FF9778A8C60]
       jp        short M01_L27
       jne       short M01_L27
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0C20B4014F8
       mov       rsi,[rcx]
       jmp       short M01_L29
M01_L27:
       vmovsd    qword ptr [rsp+28],xmm1
       vucomisd  xmm1,qword ptr [7FF9778A8C68]
       jp        short M01_L28
       jne       short M01_L28
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977825740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0C20B4014A8
       mov       rsi,[rcx]
       jmp       short M01_L29
M01_L28:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      CORINFO_HELP_NEWSFAST
       mov       rsi,rax
       mov       rcx,rsi
       vmovsd    xmm1,qword ptr [rsp+28]
       call      qword ptr [7FF977C2D470]
M01_L29:
       jmp       near ptr M01_L03
M01_L30:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.Int64Value
       call      CORINFO_HELP_NEWSFAST
       mov       rsi,rax
       mov       rcx,rsi
       mov       rdx,[rsp+38]
       call      qword ptr [7FF977C25E18]
       jmp       near ptr M01_L03
M01_L31:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.UInt64Value
       call      CORINFO_HELP_NEWSFAST
       mov       rsi,rax
       mov       rcx,rsi
       mov       rdx,[rsp+38]
       call      qword ptr [7FF977C25E48]
       jmp       near ptr M01_L03
       lea       rcx,[rsp+30]
       call      qword ptr [7FF977C25E60]
       mov       rsi,rax
       jmp       near ptr M01_L03
M01_L32:
       mov       rdx,[rsp+30]
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       short M01_L33
       xor       esi,esi
       jmp       near ptr M01_L03
M01_L33:
       cmp       dword ptr [rdi+8],0
       jne       short M01_L34
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0C20B401498
       mov       rsi,[rcx]
       jmp       short M01_L35
M01_L34:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      CORINFO_HELP_NEWSFAST
       mov       rsi,rax
       mov       rcx,rsi
       mov       rdx,rdi
       call      qword ptr [7FF977C2CA68]
M01_L35:
       jmp       near ptr M01_L03
M01_L36:
       mov       rcx,rsi
       mov       rdx,rbx
       mov       r9,rax
       xor       r8d,r8d
       mov       rax,[rsi]
       mov       rax,[rax+50]
       call      qword ptr [rax+10]
       jmp       near ptr M01_L04
; Total bytes of code 1306
```
```assembly
; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfInterface(Void*, System.Object)
       test      rdx,rdx
       je        short M02_L01
       mov       rax,[rdx]
       movzx     r8d,word ptr [rax+0E]
       test      r8,r8
       jne       short M02_L02
M02_L00:
       test      dword ptr [rax],500C0000
       jne       short M02_L05
       xor       edx,edx
M02_L01:
       mov       rax,rdx
       ret
M02_L02:
       mov       r10,[rax+38]
       cmp       r8,4
       jl        short M02_L04
M02_L03:
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
       jge       short M02_L03
       test      r8,r8
       je        short M02_L00
M02_L04:
       cmp       [r10],rcx
       je        short M02_L01
       add       r10,8
       dec       r8
       test      r8,r8
       jg        short M02_L04
       jmp       short M02_L00
M02_L05:
       jmp       qword ptr [7FF977BA5488]; System.Runtime.CompilerServices.CastHelpers.IsInstance_Helper(Void*, System.Object)
; Total bytes of code 109
```
```assembly
; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rdx,rdx
       jne       short M03_L01
M03_L00:
       mov       rax,rdx
       ret
M03_L01:
       cmp       [rdx],rcx
       je        short M03_L00
       mov       rax,[rdx]
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M03_L00
M03_L02:
       test      rax,rax
       je        short M03_L03
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M03_L00
       test      rax,rax
       jne       short M03_L04
M03_L03:
       xor       edx,edx
       jmp       short M03_L00
M03_L04:
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M03_L00
       test      rax,rax
       je        short M03_L03
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M03_L00
       test      rax,rax
       je        short M03_L03
       mov       rax,[rax+10]
       cmp       rax,rcx
       je        short M03_L00
       jmp       short M03_L02
; Total bytes of code 88
```
```assembly
; System.Globalization.NumberFormatInfo.get_CurrentInfo()
       sub       rsp,28
       mov       rcx,gs:[58]
       mov       rcx,[rcx+48]
       cmp       dword ptr [rcx+238],2
       jle       short M04_L02
       mov       rcx,[rcx+240]
       mov       rax,[rcx+10]
       test      rax,rax
       je        short M04_L02
M04_L00:
       mov       rcx,[rax+10]
       test      rcx,rcx
       jne       short M04_L01
       mov       rcx,0C20B400190
       mov       rcx,[rcx]
       test      rcx,rcx
       jne       short M04_L01
       mov       rcx,0C20B400170
       mov       rcx,[rcx]
       test      rcx,rcx
       jne       short M04_L01
       call      qword ptr [7FF97782D4B8]; System.Globalization.CultureInfo.InitializeUserDefaultCulture()
       mov       rcx,rax
M04_L01:
       cmp       byte ptr [rcx+61],0
       jne       short M04_L03
       mov       rax,[rcx+18]
       test      rax,rax
       je        short M04_L03
       add       rsp,28
       ret
M04_L02:
       mov       ecx,2
       call      qword ptr [7FF977BACB40]; System.Runtime.CompilerServices.StaticsHelpers.GetOptimizedGCThreadStaticBase(Int32)
       jmp       short M04_L00
M04_L03:
       mov       rdx,0E23DE71518
       mov       rax,[rcx]
       mov       rax,[rax+50]
       call      qword ptr [rax]
       mov       rdx,rax
       mov       rcx,offset MT_System.Globalization.NumberFormatInfo
       call      qword ptr [7FF977826328]; System.Runtime.CompilerServices.CastHelpers.ChkCastClass(Void*, System.Object)
       nop
       add       rsp,28
       ret
; Total bytes of code 173
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
       mov       rax,26DEB65D3D4B
       mov       [rbp],rax
       mov       r9,r8
       test      [rsp],esp
       sub       rsp,40
       lea       r8,[rsp+20]
       mov       [rbp+28],r8
       mov       dword ptr [rbp+30],20
       test      rdx,rdx
       jne       near ptr M05_L04
       xor       r8d,r8d
       xor       ecx,ecx
M05_L00:
       mov       [rbp+8],r8
       mov       [rbp+10],ecx
       lea       r8,[rbp+8]
       lea       rcx,[rbp+18]
       vmovaps   xmm1,xmm0
       call      qword ptr [7FF977BA7138]; System.Number.FormatFloat[[System.Double, System.Private.CoreLib],[System.Char, System.Private.CoreLib]](System.Collections.Generic.ValueListBuilder`1<Char> ByRef, Double, System.ReadOnlySpan`1<Char>, System.Globalization.NumberFormatInfo)
       mov       rbx,rax
       test      rbx,rbx
       jne       short M05_L01
       mov       ecx,[rbp+20]
       cmp       ecx,[rbp+30]
       ja        short M05_L05
       mov       rax,[rbp+28]
       mov       [rbp+8],rax
       mov       [rbp+10],ecx
       lea       rcx,[rbp+8]
       call      System.String.Ctor(System.ReadOnlySpan`1<Char>)
       mov       rbx,rax
M05_L01:
       mov       rdx,[rbp+18]
       test      rdx,rdx
       je        short M05_L02
       xor       ecx,ecx
       mov       [rbp+18],rcx
       mov       rcx,0C20B4002B0
       mov       rcx,[rcx]
       xor       r8d,r8d
       call      qword ptr [7FF977A19840]; Precode of System.Buffers.SharedArrayPool`1[[System.Char, System.Private.CoreLib]].Return(Char[], Boolean)
M05_L02:
       mov       rax,rbx
       mov       r8,26DEB65D3D4B
       cmp       [rbp],r8
       je        short M05_L03
       call      CORINFO_HELP_FAIL_FAST
M05_L03:
       nop
       lea       rsp,[rbp+38]
       pop       rbx
       pop       rbp
       ret
M05_L04:
       lea       r8,[rdx+0C]
       mov       ecx,[rdx+8]
       jmp       near ptr M05_L00
M05_L05:
       call      qword ptr [7FF9779F7D50]
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
       js        near ptr M06_L10
M06_L00:
       mov       r8,[r14+8]
       cmp       r15d,[r8+8]
       jae       near ptr M06_L43
       mov       edx,r15d
       shl       rdx,5
       mov       r8,[r8+rdx+10]
       cmp       r8,rdi
       je        short M06_L02
       test      r8,r8
       je        short M06_L01
       test      rdi,rdi
       je        short M06_L01
       mov       edx,[r8+8]
       cmp       edx,[rdi+8]
       jne       short M06_L01
       lea       rcx,[r8+0C]
       mov       r8d,[r8+8]
       add       r8d,r8d
       lea       rdx,[rdi+0C]
       call      qword ptr [7FF97782C330]; System.SpanHelpers.SequenceEqual(Byte ByRef, Byte ByRef, UIntPtr)
       test      eax,eax
       jne       short M06_L02
M06_L01:
       dec       r15d
       jns       short M06_L00
       jmp       near ptr M06_L10
M06_L02:
       mov       r8d,r15d
M06_L03:
       test      r8d,r8d
       jl        near ptr M06_L11
       mov       rcx,[r14+8]
       cmp       r8d,[rcx+8]
       jae       near ptr M06_L43
       mov       r8d,r8d
       shl       r8,5
       movzx     r8d,word ptr [rcx+r8+18]
       cmp       qword ptr [rsi+18],0
       jne       near ptr M06_L12
M06_L04:
       xor       ecx,ecx
M06_L05:
       mov       rdx,[rsi+10]
       cmp       r8d,[rdx+8]
       jae       near ptr M06_L43
       mov       r8d,r8d
       shl       r8,4
       vmovdqu   xmm0,xmmword ptr [rdx+r8+10]
       vmovdqu   xmmword ptr [rsp+28],xmm0
       vmovdqu   xmm0,xmmword ptr [rsp+20]
       vmovdqu   xmmword ptr [rsp+60],xmm0
       mov       r8,[rsp+30]
       mov       [rsp+70],r8
       mov       [rsp+60],rcx
M06_L06:
       mov       rdx,[rsp+60]
       test      rdx,rdx
       jne       near ptr M06_L15
M06_L07:
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
       jne       near ptr M06_L16
M06_L08:
       mov       rdx,r14
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BondingFunction
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rax,rax
       jne       near ptr M06_L17
       mov       [rbx],rbp
       mov       [rbx+8],rdi
M06_L09:
       mov       rax,rbx
       add       rsp,78
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r14
       pop       r15
       ret
M06_L10:
       mov       r8d,0FFFFFFFF
       jmp       near ptr M06_L03
M06_L11:
       mov       rcx,[rsi+8]
       test      rcx,rcx
       je        short M06_L14
       jmp       short M06_L13
M06_L12:
       mov       rcx,[rsi+18]
       cmp       [rcx+8],r8d
       jle       near ptr M06_L04
       mov       rcx,[rsi+18]
       cmp       r8d,[rcx+8]
       jae       near ptr M06_L43
       mov       edx,r8d
       mov       rcx,[rcx+rdx*8+10]
       jmp       near ptr M06_L05
M06_L13:
       lea       r8,[rsp+60]
       mov       rdx,rdi
       call      qword ptr [7FF977C25FF8]; AuroraScript.Runtime.Types.ScriptObject.TryResolveProperty(System.String, AuroraScript.Runtime.Property.PropertyDescriptor ByRef)
       test      eax,eax
       je        near ptr M06_L36
       jmp       near ptr M06_L06
M06_L14:
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rsp+60],xmm0
       vmovdqu   xmmword ptr [rsp+68],xmm0
       jmp       near ptr M06_L36
M06_L15:
       mov       rdx,[rdx+8]
       test      rdx,rdx
       je        near ptr M06_L07
       lea       rcx,[rsp+60]
       call      qword ptr [7FF977C26010]; AuroraScript.Runtime.Property.PropertyDescriptor.get_Getter()
       cmp       [rax],al
       mov       rcx,rax
       mov       rdx,rbx
       mov       r8,rbp
       call      qword ptr [7FF977C2CBA0]
       jmp       near ptr M06_L09
M06_L16:
       mov       r15,[rax+38]
       test      r15,r15
       je        near ptr M06_L08
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
       jmp       near ptr M06_L09
M06_L17:
       mov       rcx,rax
       mov       rdx,rsi
       call      qword ptr [7FF977C26040]
       mov       r15,rax
       test      r15,r15
       je        near ptr M06_L18
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NullValue
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rax,rax
       jne       near ptr M06_L18
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rax,rax
       jne       near ptr M06_L19
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.Int64Value
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rax,rax
       jne       near ptr M06_L24
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.UInt64Value
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rax,rax
       jne       near ptr M06_L25
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BooleanValue
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rax,rax
       jne       near ptr M06_L26
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rax,rax
       jne       near ptr M06_L27
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptArray
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdx,rax
       test      rdx,rdx
       jne       near ptr M06_L28
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptDate
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdx,rax
       test      rdx,rdx
       jne       near ptr M06_L29
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptRegex
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdx,rax
       test      rdx,rdx
       jne       near ptr M06_L30
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ClosureFunction
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdx,rax
       test      rdx,rdx
       jne       near ptr M06_L31
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptError
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdx,rax
       test      rdx,rdx
       jne       near ptr M06_L32
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Interop.ClrMethodBinding
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdx,rax
       test      rdx,rdx
       jne       near ptr M06_L33
       mov       rdx,r15
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptType
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdx,rax
       test      rdx,rdx
       jne       near ptr M06_L34
       mov       esi,408
       jmp       near ptr M06_L35
M06_L18:
       xor       r15d,r15d
       xor       esi,esi
       jmp       near ptr M06_L35
M06_L19:
       vmovsd    xmm0,qword ptr [rax+38]
       vucomisd  xmm0,xmm0
       jp        short M06_L20
       je        short M06_L21
M06_L20:
       mov       rsi,7FF8000000000004
       jmp       short M06_L23
M06_L21:
       vmovq     rsi,xmm0
       cmp       rsi,2
       jbe       short M06_L22
       jmp       short M06_L23
M06_L22:
       mov       rcx,7FF8000000000001
       add       rsi,rcx
M06_L23:
       xor       r15d,r15d
       jmp       near ptr M06_L35
M06_L24:
       mov       rsi,[rax+38]
       mov       rcx,0C20B400450
       mov       r15,[rcx]
       jmp       near ptr M06_L35
M06_L25:
       mov       rsi,[rax+38]
       mov       rcx,0C20B400458
       mov       r15,[rcx]
       jmp       short M06_L35
M06_L26:
       movzx     ecx,byte ptr [rax+40]
       mov       esi,2
       mov       edx,1
       test      ecx,ecx
       cmove     rsi,rdx
       xor       r15d,r15d
       jmp       short M06_L35
M06_L27:
       mov       rcx,[rax+38]
       mov       rdx,0E23DE70008
       test      rcx,rcx
       cmove     rcx,rdx
       mov       r15,rcx
       mov       esi,4
       jmp       short M06_L35
M06_L28:
       mov       r15,rdx
       mov       esi,18
       jmp       short M06_L35
M06_L29:
       mov       r15,rdx
       mov       esi,28
       jmp       short M06_L35
M06_L30:
       mov       r15,rdx
       mov       esi,48
       jmp       short M06_L35
M06_L31:
       mov       r15,rdx
       mov       esi,88
       jmp       short M06_L35
M06_L32:
       mov       r15,rdx
       mov       esi,808
       jmp       short M06_L35
M06_L33:
       mov       r15,rdx
       mov       esi,208
       jmp       short M06_L35
M06_L34:
       mov       r15,rdx
       mov       esi,108
M06_L35:
       mov       rcx,rbx
       mov       rdx,r15
       call      CORINFO_HELP_CHECKED_ASSIGN_REF
       mov       [rbx+8],rsi
       jmp       near ptr M06_L09
M06_L36:
       lea       rdx,[rsp+58]
       mov       rcx,rsi
       call      qword ptr [7FF977C26058]
       test      eax,eax
       je        near ptr M06_L42
       lea       rdx,[rsp+38]
       mov       rcx,[rsp+58]
       mov       r8,rbp
       mov       r9,rdi
       cmp       [rcx],ecx
       call      qword ptr [7FF977C3B890]
       cmp       qword ptr [rsp+38],0
       je        short M06_L37
       mov       rcx,[rsp+38]
       mov       rax,0C20B400450
       cmp       rcx,[rax]
       je        short M06_L41
       mov       rcx,[rsp+38]
       mov       rax,0C20B400458
       cmp       rcx,[rax]
       je        short M06_L41
       movsx     rcx,word ptr [rsp+40]
       jmp       short M06_L40
M06_L37:
       mov       rcx,[rsp+40]
       test      rcx,rcx
       je        short M06_L39
       dec       rcx
       mov       eax,1
       mov       edx,2
       cmp       rcx,1
       cmova     eax,edx
       mov       ecx,eax
M06_L38:
       jmp       short M06_L40
M06_L39:
       xor       ecx,ecx
       jmp       short M06_L38
M06_L40:
       test      ecx,ecx
       jne       short M06_L41
       lea       rcx,[rsp+38]
       call      qword ptr [7FF977C25E78]; AuroraScript.Runtime.ScriptDatum.get_Object()
       test      rax,rax
       je        short M06_L42
M06_L41:
       vmovdqu   xmm0,xmmword ptr [rsp+38]
       vmovdqu   xmmword ptr [rbx],xmm0
       jmp       near ptr M06_L09
M06_L42:
       mov       rax,0C20B400428
       mov       rax,[rax]
       vmovdqu   xmm0,xmmword ptr [rax+8]
       vmovdqu   xmmword ptr [rbx],xmm0
       jmp       near ptr M06_L09
M06_L43:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 1480
```
```assembly
; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rax,[rcx+20]
       mov       rax,[rax-18]
       mov       rdx,rax
       test      dl,1
       jne       short M07_L00
       ret
M07_L00:
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
       jne       short M08_L00
       mov       rcx,7FF977CA9074
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rax,rsi
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
M08_L00:
       mov       rcx,7FF977CA9070
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rcx,rbx
       add       rsp,28
       pop       rbx
       pop       rsi
       jmp       qword ptr [7FF9779FFD68]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBaseSlow(System.Runtime.CompilerServices.MethodTable*)
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
M09_L00:
       lea       r10d,[r8+1]
       movsxd    r10,r10d
       lea       r10,[r10+r10*2]
       lea       r10,[rax+r10*8]
       mov       r9d,[r10]
       mov       r11,[r10+8]
       and       r9d,0FFFFFFFE
       cmp       r11,rdx
       jne       short M09_L02
       mov       r11,rbx
       xor       r11,[r10+10]
       cmp       r11,1
       ja        short M09_L02
       cmp       r9d,[r10]
       jne       short M09_L04
M09_L01:
       cmp       r11d,1
       jne       short M09_L03
       mov       rax,rsi
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
M09_L02:
       test      r9d,r9d
       je        short M09_L04
       inc       ecx
       add       r8d,ecx
       and       r8d,[rax+4]
       cmp       ecx,8
       jl        short M09_L00
       jmp       short M09_L04
M09_L03:
       test      r11d,r11d
       jne       short M09_L05
       xor       eax,eax
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
M09_L04:
       mov       r11d,2
       jmp       short M09_L01
M09_L05:
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
       je        short M10_L00
       mov       rcx,rsi
       xor       r8d,r8d
       call      qword ptr [7FF96E851BC8]
       mov       rax,[rbx]
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
M10_L00:
       call      qword ptr [7FF96E84F410]
       int       3
; Total bytes of code 61
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
       jne       short M11_L01
       cmp       [rax],ecx
       jle       short M11_L03
       mov       rax,[rax+8]
       cmp       [rax],al
       add       edx,0FFFFFFFE
       movsxd    rcx,edx
       mov       rax,[rax+rcx*8+10]
       test      rax,rax
       je        short M11_L03
M11_L00:
       add       rsp,20
       pop       rbx
       ret
M11_L01:
       mov       ecx,ebx
       sar       ecx,18
       cmp       ecx,2
       jne       short M11_L02
       movsxd    rcx,edx
       add       rax,rcx
       jmp       short M11_L00
M11_L02:
       cmp       [rax+4],edx
       jle       short M11_L03
       mov       rcx,[rax+10]
       movsxd    rax,edx
       mov       rcx,[rcx+rax*8]
       test      rcx,rcx
       je        short M11_L03
       mov       rax,[rcx]
       test      rax,rax
       je        short M11_L03
       jmp       short M11_L00
M11_L03:
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
       je        short M12_L00
       cmp       [rdx],rcx
       jne       short M12_L01
M12_L00:
       mov       rax,rdx
       ret
M12_L01:
       jmp       qword ptr [7FF9779F4D20]; System.Runtime.CompilerServices.CastHelpers.ChkCastClassSpecial(Void*, System.Object)
; Total bytes of code 20
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
M13_L00:
       vmovdqa   xmmword ptr [rbp+rax+120],xmm4
       vmovdqa   xmmword ptr [rbp+rax+130],xmm4
       vmovdqa   xmmword ptr [rbp+rax+140],xmm4
       add       rax,30
       jne       short M13_L00
       mov       [rbp+120],rax
       mov       rax,26DEB65D3D4B
       mov       [rbp+8],rax
       mov       rdi,rcx
       vmovaps   xmm6,xmm1
       mov       rbx,r8
       mov       rsi,r9
       vmovq     rax,xmm6
       mov       rcx,7FF0000000000000
       andn      rax,rax,rcx
       je        near ptr M13_L20
       mov       r14,[rbx]
       mov       r15d,[rbx+8]
       xor       ecx,ecx
       test      r15d,r15d
       jg        near ptr M13_L27
M13_L01:
       mov       dword ptr [rbp+128],0FFFFFFFF
       test      r15d,r15d
       jne       near ptr M13_L37
M13_L02:
       mov       ecx,47
M13_L03:
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
       jbe       near ptr M13_L51
       mov       r9,[rbp+118]
       mov       byte ptr [r9],0
       vmovq     r9,xmm6
       shr       r9,3F
       mov       [rbp+110],r9b
       lea       r9,[rbp+100]
       lea       rdx,[rbp+128]
       mov       r8,rsi
       call      qword ptr [7FF977BA7150]; System.Number.GetFloatingPointMaxDigitsAndPrecision(Char, Int32 ByRef, System.Globalization.NumberFormatInfo, Boolean ByRef)
       mov       r15d,eax
       vxorps    xmm0,xmm0,xmm0
       vucomisd  xmm6,xmm0
       jp        short M13_L04
       je        near ptr M13_L16
M13_L04:
       cmp       byte ptr [rbp+100],0
       je        near ptr M13_L49
       mov       r13d,[rbp+128]
       vmovq     rax,xmm6
       test      rax,rax
       jl        near ptr M13_L38
       vmovaps   xmm0,xmm6
M13_L05:
       cmp       r13d,0FFFFFFFF
       jne       near ptr M13_L48
       vmovq     rax,xmm0
       mov       rdx,0FFFFFFFFFFFFF
       and       rdx,rax
       shr       rax,34
       and       eax,7FF
       je        near ptr M13_L39
       mov       rcx,10000000000000
       or        rdx,rcx
       add       eax,0FFFFFBCD
M13_L06:
       lea       rcx,[rdx*2+1]
       lea       r8d,[rax-1]
       xor       r10d,r10d
       lzcnt     r10,rcx
       shlx      rcx,rcx,r10
       sub       r8d,r10d
       mov       r10,10000000000000
       cmp       rdx,r10
       je        near ptr M13_L40
       lea       r10,[rdx*2-1]
       lea       r9d,[rax-1]
M13_L07:
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
       vmulsd    xmm0,xmm0,qword ptr [7FF9778A58B0]
       vroundsd  xmm0,xmm0,xmm0,0A
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       r9d,7FFFFFFF
       vcvttsd2si r11d,xmm1
       vucomisd  xmm0,qword ptr [7FF9778A58B8]
       cmovb     r9d,r11d
       add       r9d,15B
       mov       r11d,r9d
       sar       r11d,1F
       and       r11d,7
       add       r9d,r11d
       sar       r9d,3
       inc       r9d
       cmp       r9d,57
       jae       near ptr M13_L51
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
       jae       near ptr M13_L51
       mov       r10d,eax
       mov       r9,7FF96DBD36D0
       mov       r10d,[r9+r10*4]
       cmp       r11d,r10d
       jb        near ptr M13_L41
M13_L08:
       lea       r9d,[rax+1]
       xor       eax,eax
       mov       [rbp+0D8],eax
       test      r9d,r9d
       jle       near ptr M13_L42
M13_L09:
       mov       eax,r11d
       xor       edx,edx
       div       r10d
       mov       edx,eax
       imul      edx,r10d
       sub       r11d,edx
       mov       edx,[rbp+0D8]
       cmp       edx,r12d
       jae       near ptr M13_L51
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
       jb        short M13_L10
       mov       edx,0CCCCCCCD
       mov       r10d,r10d
       imul      r10,rdx
       shr       r10,23
       test      r9d,r9d
       jle       near ptr M13_L47
       mov       [rbp+68],rax
       jmp       short M13_L09
M13_L10:
       mov       eax,[rbp+0D8]
       mov       r11,[rbp+70]
       sub       r11,[rbp+0B8]
       mov       [rbp+10],r13
       mov       r10d,r10d
       shlx      r8,r10,r8
       mov       r10,r11
       sub       r10,1
       inc       r11
M13_L11:
       cmp       rdx,r10
       jb        near ptr M13_L44
M13_L12:
       cmp       rdx,r11
       jae       short M13_L13
       mov       rax,rcx
       sub       rax,rdx
       cmp       rax,r8
       jae       near ptr M13_L45
M13_L13:
       cmp       rdx,2
       jb        near ptr M13_L46
       sub       rcx,4
       cmp       rcx,rdx
       setae     al
       movzx     eax,al
M13_L14:
       mov       edx,r9d
       sub       edx,[rbp+0CC]
       mov       [rbp+0D0],edx
       movzx     r13d,al
M13_L15:
       test      r13d,r13d
       je        near ptr M13_L49
       mov       r8d,[rbp+0D8]
       add       r8d,[rbp+0D0]
       mov       [rbp+10C],r8d
       mov       r8d,[rbp+120]
       cmp       [rbp+0D8],r8d
       jae       near ptr M13_L51
       mov       r8,[rbp+118]
       mov       r9d,[rbp+0D8]
       mov       byte ptr [r8+r9],0
       mov       r8d,[rbp+0D8]
       mov       [rbp+108],r8d
M13_L16:
       test      r14d,r14d
       je        near ptr M13_L50
       cmp       dword ptr [rbp+128],0FFFFFFFF
       jne       short M13_L17
       mov       r15d,[rbp+108]
       mov       edx,11
       cmp       r15d,11
       cmovl     r15d,edx
M13_L17:
       mov       [rsp+20],rsi
       lea       rdx,[rbp+108]
       mov       rcx,rdi
       mov       r8d,r14d
       mov       r9d,r15d
       call      qword ptr [7FF977C25F38]; System.Number.NumberToString[[System.Char, System.Private.CoreLib]](System.Collections.Generic.ValueListBuilder`1<Char> ByRef, NumberBuffer ByRef, Char, Int32, System.Globalization.NumberFormatInfo)
M13_L18:
       xor       eax,eax
       mov       r8,26DEB65D3D4B
       cmp       [rbp+8],r8
       je        short M13_L19
       call      CORINFO_HELP_FAIL_FAST
M13_L19:
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
M13_L20:
       vucomisd  xmm6,xmm6
       jp        short M13_L21
       je        short M13_L23
M13_L21:
       mov       rax,[rsi+58]
       mov       r8,26DEB65D3D4B
       cmp       [rbp+8],r8
       je        short M13_L22
       call      CORINFO_HELP_FAIL_FAST
M13_L22:
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
M13_L23:
       vmovq     rax,xmm6
       test      rax,rax
       jl        short M13_L25
       mov       rax,[rsi+60]
       mov       r8,26DEB65D3D4B
       cmp       [rbp+8],r8
       je        short M13_L24
       call      CORINFO_HELP_FAIL_FAST
M13_L24:
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
M13_L25:
       mov       rax,[rsi+68]
       mov       r8,26DEB65D3D4B
       cmp       [rbp+8],r8
       je        short M13_L26
       call      CORINFO_HELP_FAIL_FAST
M13_L26:
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
M13_L27:
       movzx     ecx,word ptr [r14]
       mov       eax,ecx
       or        eax,20
       add       eax,0FFFFFF9F
       cmp       eax,19
       ja        near ptr M13_L01
       cmp       r15d,1
       jne       short M13_L29
       mov       dword ptr [rbp+128],0FFFFFFFF
M13_L28:
       jmp       near ptr M13_L03
M13_L29:
       cmp       r15d,2
       jne       short M13_L30
       movzx     eax,word ptr [r14+2]
       add       eax,0FFFFFFD0
       cmp       eax,0A
       jae       short M13_L31
       mov       [rbp+128],eax
       jmp       short M13_L28
M13_L30:
       cmp       r15d,3
       jne       short M13_L31
       movzx     eax,word ptr [r14+2]
       add       eax,0FFFFFFD0
       movzx     edx,word ptr [r14+4]
       add       edx,0FFFFFFD0
       cmp       eax,0A
       jae       short M13_L31
       cmp       edx,0A
       jae       short M13_L31
       lea       eax,[rax+rax*4]
       lea       eax,[rdx+rax*2]
       mov       [rbp+128],eax
       jmp       short M13_L28
M13_L31:
       xor       r13d,r13d
       mov       eax,1
       jmp       short M13_L33
M13_L32:
       cmp       r13d,5F5E100
       jge       short M13_L34
       lea       edx,[rax+1]
       mov       eax,eax
       movzx     eax,word ptr [r14+rax*2]
       lea       r8d,[r13+r13*4]
       lea       r13d,[rax+r8*2-30]
       mov       eax,edx
M13_L33:
       cmp       eax,r15d
       jae       short M13_L36
       mov       edx,eax
       movzx     edx,word ptr [r14+rdx*2]
       add       edx,0FFFFFFD0
       cmp       edx,9
       jbe       short M13_L32
       jmp       short M13_L35
M13_L34:
       call      qword ptr [7FF977C2C270]
       int       3
M13_L35:
       mov       r9d,eax
       cmp       word ptr [r14+r9*2],0
       jne       near ptr M13_L01
M13_L36:
       mov       [rbp+128],r13d
       jmp       near ptr M13_L28
M13_L37:
       test      ecx,ecx
       je        near ptr M13_L02
       xor       ecx,ecx
       jmp       near ptr M13_L03
M13_L38:
       vxorps    xmm0,xmm6,[7FF9778A58C0]
       jmp       near ptr M13_L05
M13_L39:
       mov       eax,0FFFFFBCE
       jmp       near ptr M13_L06
M13_L40:
       lea       r9d,[rax-2]
       mov       r10,3FFFFFFFFFFFFF
       jmp       near ptr M13_L07
M13_L41:
       dec       eax
       cmp       eax,0A
       jae       near ptr M13_L51
       mov       r10d,eax
       mov       r9,7FF96DBD36D0
       mov       r10d,[r9+r10*4]
       jmp       near ptr M13_L08
M13_L42:
       mov       rax,[rbp+68]
       jmp       short M13_L47
M13_L43:
       lea       r13d,[rax-1]
       cmp       r13d,r12d
       jae       near ptr M13_L51
       lea       r13d,[rax-1]
       add       r13,[rbp+10]
       dec       byte ptr [r13]
       add       rdx,r8
       jmp       near ptr M13_L11
M13_L44:
       mov       r13,rcx
       sub       r13,rdx
       cmp       r13,r8
       jb        near ptr M13_L12
       lea       r13,[rdx+r8]
       cmp       r13,r10
       jb        short M13_L43
       mov       r13,r10
       sub       r13,rdx
       mov       [rbp],r13
       lea       r13,[rdx+r8]
       sub       r13,r10
       cmp       [rbp],r13
       jae       short M13_L43
       jmp       near ptr M13_L12
M13_L45:
       lea       rax,[rdx+r8]
       cmp       rax,r11
       jb        short M13_L46
       mov       rax,r11
       sub       rax,rdx
       add       r8,rdx
       sub       r8,r11
       cmp       rax,r8
       jbe       near ptr M13_L13
M13_L46:
       xor       eax,eax
       jmp       near ptr M13_L14
M13_L47:
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
       jae       near ptr M13_L51
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
       jae       near ptr M13_L52
       mov       r8d,[rbp+0D8]
       mov       [rbp+64],r8d
       lea       rcx,[rbp+70]
       lea       rdx,[rbp+50]
       lea       r8,[rbp+0B8]
       call      qword ptr [7FF977C2CAC8]
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
       call      qword ptr [7FF977C25F20]; System.Number+Grisu3.TryRoundWeedShortest(System.Span`1<Byte>, Int32, UInt64, UInt64, UInt64, UInt64, UInt64)
       mov       r9d,[rbp+0B4]
       jmp       near ptr M13_L14
M13_L48:
       lea       rdx,[rbp+40]
       call      qword ptr [7FF977BA7180]; System.Number.ExtractFractionAndBiasedExponent[[System.Double, System.Private.CoreLib]](Double, Int32 ByRef)
       mov       ecx,[rbp+40]
       mov       [rbp+0F0],rax
       mov       [rbp+0F8],ecx
       lea       rcx,[rbp+0F0]
       lea       rdx,[rbp+0E0]
       call      qword ptr [7FF977BA7198]; System.Number+DiyFp.Normalize()
       vmovdqu   xmm0,xmmword ptr [rbp+118]
       vmovdqu   xmmword ptr [rbp+20],xmm0
       lea       r8,[rbp+0D0]
       mov       [rsp+20],r8
       lea       r8,[rbp+20]
       lea       rcx,[rbp+0E0]
       lea       r9,[rbp+0D8]
       mov       edx,r13d
       call      qword ptr [7FF977BA71B0]; System.Number+Grisu3.TryRunCounted(DiyFp ByRef, Int32, System.Span`1<Byte>, Int32 ByRef, Int32 ByRef)
       mov       r13d,eax
       jmp       near ptr M13_L15
M13_L49:
       movzx     r8d,byte ptr [rbp+100]
       lea       r9,[rbp+108]
       vmovaps   xmm0,xmm6
       mov       edx,[rbp+128]
       call      qword ptr [7FF977BA7258]; System.Number.Dragon4[[System.Double, System.Private.CoreLib]](Double, Int32, Boolean, NumberBuffer ByRef)
       jmp       near ptr M13_L16
M13_L50:
       lea       rdx,[rbp+108]
       mov       rcx,rdi
       mov       r8,rbx
       mov       r9,rsi
       call      qword ptr [7FF977BA7330]; System.Number.NumberToStringFormat[[System.Char, System.Private.CoreLib]](System.Collections.Generic.ValueListBuilder`1<Char> ByRef, NumberBuffer ByRef, System.ReadOnlySpan`1<Char>, System.Globalization.NumberFormatInfo)
       jmp       near ptr M13_L18
M13_L51:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
M13_L52:
       mov       rax,[rbp+68]
       mov       r9d,[rbp+0B4]
       jmp       near ptr M13_L47
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
       je        short M14_L00
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
M14_L00:
       mov       rax,0E23DE70008
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
       jae       short M15_L04
       cmp       r8,4
       jb        short M15_L01
       add       r8,0FFFFFFFFFFFFFFFC
       mov       eax,[rcx]
       sub       eax,[rdx]
       mov       ecx,[rcx+r8]
       sub       ecx,[rdx+r8]
       or        eax,ecx
       sete      al
       movzx     eax,al
M15_L00:
       vzeroupper
       ret
M15_L01:
       xor       eax,eax
       mov       r10,r8
       and       r10,2
       je        short M15_L02
       movzx     eax,word ptr [rcx]
       movzx     r9d,word ptr [rdx]
       sub       eax,r9d
M15_L02:
       test      r8b,1
       je        short M15_L03
       movzx     ecx,byte ptr [rcx+r10]
       movzx     edx,byte ptr [rdx+r10]
       sub       ecx,edx
       or        eax,ecx
M15_L03:
       test      eax,eax
       sete      al
       movzx     eax,al
       jmp       short M15_L00
M15_L04:
       cmp       rcx,rdx
       je        near ptr M15_L10
       cmp       r8,20
       jb        short M15_L07
       xor       eax,eax
       add       r8,0FFFFFFFFFFFFFFE0
       je        short M15_L06
M15_L05:
       vmovups   ymm0,[rcx+rax]
       vpcmpeqb  ymm0,ymm0,[rdx+rax]
       vpmovmskb r10d,ymm0
       cmp       r10d,0FFFFFFFF
       jne       near ptr M15_L12
       add       rax,20
       cmp       r8,rax
       ja        short M15_L05
M15_L06:
       vmovups   ymm0,[rcx+r8]
       vpcmpeqb  ymm0,ymm0,[rdx+r8]
       vpmovmskb ecx,ymm0
       cmp       ecx,0FFFFFFFF
       jne       short M15_L12
       jmp       short M15_L10
M15_L07:
       cmp       r8,10
       jb        short M15_L11
       xor       eax,eax
       add       r8,0FFFFFFFFFFFFFFF0
       je        short M15_L09
M15_L08:
       vmovups   xmm0,[rcx+rax]
       vpcmpeqb  xmm0,xmm0,[rdx+rax]
       vpmovmskb r10d,xmm0
       cmp       r10d,0FFFF
       jne       short M15_L12
       add       rax,10
       cmp       r8,rax
       ja        short M15_L08
M15_L09:
       vmovups   xmm0,[rcx+r8]
       vpcmpeqb  xmm0,xmm0,[rdx+r8]
       vpmovmskb eax,xmm0
       cmp       eax,0FFFF
       jne       short M15_L12
M15_L10:
       mov       eax,1
       vzeroupper
       ret
M15_L11:
       lea       rax,[r8-8]
       mov       r8,[rcx]
       sub       r8,[rdx]
       mov       rcx,[rcx+rax]
       sub       rcx,[rdx+rax]
       or        rcx,r8
       sete      al
       movzx     eax,al
       jmp       near ptr M15_L00
M15_L12:
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
M16_L00:
       mov       rbp,[rsi+20]
       mov       r8,[rbp+8]
       mov       r14d,[r8+8]
       dec       r14d
       js        near ptr M16_L07
       mov       r8,0C20B400068
       mov       r15,[r8]
M16_L01:
       mov       r8,[rbp+8]
       cmp       r14d,[r8+8]
       jae       near ptr M16_L11
       mov       edx,r14d
       shl       rdx,5
       mov       r8,[r8+rdx+10]
       cmp       r8,rdi
       je        short M16_L03
       test      r8,r8
       je        short M16_L02
       test      rdi,rdi
       je        short M16_L02
       mov       edx,[r8+8]
       cmp       edx,[rdi+8]
       jne       short M16_L02
       lea       rcx,[r8+0C]
       mov       r8d,[r8+8]
       add       r8d,r8d
       lea       rdx,[rdi+0C]
       call      qword ptr [7FF97782C330]; System.SpanHelpers.SequenceEqual(Byte ByRef, Byte ByRef, UIntPtr)
       test      eax,eax
       jne       short M16_L03
M16_L02:
       dec       r14d
       jns       short M16_L01
       jmp       short M16_L07
M16_L03:
       mov       eax,r14d
M16_L04:
       test      eax,eax
       jl        short M16_L08
       mov       rcx,[rbp+8]
       cmp       eax,[rcx+8]
       jae       near ptr M16_L11
       mov       eax,eax
       shl       rax,5
       movzx     eax,word ptr [rcx+rax+18]
       cmp       qword ptr [rsi+18],0
       jne       short M16_L09
M16_L05:
       xor       ecx,ecx
M16_L06:
       mov       rdx,[rsi+10]
       cmp       eax,[rdx+8]
       jae       near ptr M16_L11
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
M16_L07:
       mov       eax,0FFFFFFFF
       jmp       short M16_L04
M16_L08:
       mov       rsi,[rsi+8]
       test      rsi,rsi
       je        short M16_L10
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rsp+20],xmm0
       vmovdqu   xmmword ptr [rsp+28],xmm0
       jmp       near ptr M16_L00
M16_L09:
       mov       rcx,[rsi+18]
       cmp       [rcx+8],eax
       jle       short M16_L05
       mov       rcx,[rsi+18]
       cmp       eax,[rcx+8]
       jae       short M16_L11
       mov       edx,eax
       mov       rcx,[rcx+rdx*8+10]
       jmp       near ptr M16_L06
M16_L10:
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
M16_L11:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 367
```
```assembly
; AuroraScript.Runtime.Property.PropertyDescriptor.get_Getter()
       mov       rcx,[rcx]
       test      rcx,rcx
       jne       short M17_L00
       xor       eax,eax
       ret
M17_L00:
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
       je        short M18_L00
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       cmp       [rax],rcx
       jne       short M18_L01
M18_L00:
       test      rax,rax
       je        short M18_L02
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
M18_L01:
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       jmp       short M18_L00
M18_L02:
       mov       rdx,[rbx]
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbx,rax
       test      rbx,rbx
       jne       short M18_L03
       xor       eax,eax
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
M18_L03:
       cmp       dword ptr [rbx+8],0
       jne       short M18_L04
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0C20B401498
       mov       rax,[rcx]
       jmp       short M18_L05
M18_L04:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      CORINFO_HELP_NEWSFAST
       mov       rsi,rax
       mov       rcx,rsi
       mov       rdx,rbx
       call      qword ptr [7FF977C2CA68]
       mov       rax,rsi
M18_L05:
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
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
; AuroraBenchmark.BoundaryOpsBenchmarks.WriteIntProperty()
       push      r15
       push      r14
       push      r13
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,140
       vxorps    xmm4,xmm4,xmm4
       mov       rax,0FFFFFFFFFFFFFEE0
M00_L00:
       vmovdqa   xmmword ptr [rsp+rax+140],xmm4
       vmovdqa   xmmword ptr [rsp+rax+150],xmm4
       vmovdqa   xmmword ptr [rsp+rax+160],xmm4
       add       rax,30
       jne       short M00_L00
       mov       rbx,rcx
M00_L01:
       xor       esi,esi
       jmp       near ptr M00_L06
M00_L02:
       mov       rdx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       cmp       [rcx],rdx
       jne       near ptr M00_L08
       xor       ecx,ecx
M00_L03:
       test      rcx,rcx
       jne       near ptr M00_L09
       mov       r13,rdi
       test      r13,r13
       je        short M00_L04
       mov       rdx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       cmp       [r13],rdx
       jne       near ptr M00_L10
       xor       r13d,r13d
M00_L04:
       test      r13,r13
       jne       near ptr M00_L11
       mov       [rsp+30],rdi
       mov       [rsp+38],rbp
       mov       [rsp+20],r14
       mov       [rsp+28],r15
       lea       rdx,[rsp+30]
       lea       r9,[rsp+20]
       vmovsd    xmm2,qword ptr [7FF9778BC0B8]
       lea       rcx,[rsp+130]
       call      qword ptr [7FF977C35D10]; AuroraScript.Runtime.ObjectOps.SetNumericProperty(AuroraScript.Runtime.ScriptDatum, Double, AuroraScript.Runtime.ScriptDatum)
M00_L05:
       inc       esi
       cmp       esi,200
       jge       short M00_L07
M00_L06:
       mov       rdi,[rbx+68]
       mov       rbp,[rbx+70]
       mov       rdx,[rbx+8]
       mov       ecx,esi
       and       ecx,1F
       cmp       ecx,[rdx+8]
       jae       near ptr M00_L173
       shl       rcx,4
       lea       rdx,[rdx+rcx+10]
       mov       r14,[rdx]
       mov       r15,[rdx+8]
       mov       rcx,rdi
       test      rcx,rcx
       je        near ptr M00_L03
       jmp       near ptr M00_L02
M00_L07:
       add       rsp,140
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r13
       pop       r14
       pop       r15
       ret
M00_L08:
       mov       rdx,rdi
       mov       rcx,offset MT_AuroraScript.Runtime.Types.IAuroraNativeIndexer
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfInterface(Void*, System.Object)
       mov       rcx,rax
       jmp       near ptr M00_L03
M00_L09:
       mov       [rsp+30],r14
       mov       [rsp+38],r15
       lea       r8,[rsp+30]
       mov       r11,7FF9777804E0
       mov       edx,7
       call      qword ptr [r11]
       jmp       near ptr M00_L05
M00_L10:
       mov       rdx,rdi
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptPackedArray
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       r13,rax
       jmp       near ptr M00_L04
M00_L11:
       mov       rdx,r13
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt32Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       near ptr M00_L12
       mov       rdx,r13
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt8Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       near ptr M00_L26
       mov       rdx,r13
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptFloat32Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       near ptr M00_L40
       mov       rdx,r13
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptFloat64Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       near ptr M00_L54
       mov       rdx,r13
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptBooleanArray
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       near ptr M00_L68
       mov       rdx,r13
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptUInt8Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       near ptr M00_L82
       mov       rdx,r13
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt16Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       near ptr M00_L96
       mov       rdx,r13
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptUInt16Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       near ptr M00_L110
       mov       rdx,r13
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptUInt32Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       near ptr M00_L124
       mov       rdx,r13
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptInt64Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       near ptr M00_L138
       mov       rdx,r13
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptUInt64Array
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       test      rdi,rdi
       jne       near ptr M00_L154
       mov       rcx,r13
       mov       rax,[r13]
       mov       rax,[rax+58]
       call      qword ptr [rax+8]
       cmp       eax,7
       ja        near ptr M00_L172
       jmp       near ptr M00_L171
M00_L12:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],7
       jg        short M00_L13
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,7
       call      qword ptr [7FF977C3D3F8]
M00_L13:
       mov       rdi,[rdi+38]
       xor       ecx,ecx
       mov       [rsp+128],rcx
       mov       [rsp+118],r14
       mov       [rsp+120],r15
       cmp       qword ptr [rsp+118],0
       je        short M00_L14
       mov       rcx,[rsp+118]
       mov       rdx,0CCAD800448
       cmp       rcx,[rdx]
       je        short M00_L18
       mov       rcx,[rsp+118]
       mov       rdx,0CCAD800450
       cmp       rcx,[rdx]
       je        short M00_L18
       movsx     rcx,word ptr [rsp+120]
       jmp       short M00_L17
M00_L14:
       mov       rcx,[rsp+120]
       test      rcx,rcx
       je        short M00_L15
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L16
       mov       ecx,2
       jmp       short M00_L17
M00_L15:
       xor       ecx,ecx
       jmp       short M00_L17
M00_L16:
       mov       ecx,1
M00_L17:
       test      ecx,ecx
       jne       short M00_L18
       xor       ecx,ecx
       mov       [rsp+128],rcx
       jmp       near ptr M00_L24
M00_L18:
       cmp       qword ptr [rsp+118],0
       jne       short M00_L22
       cmp       qword ptr [rsp+120],2
       jbe       short M00_L22
       mov       rcx,[rsp+120]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L19
       vmovq     xmm0,rcx
       jmp       short M00_L21
M00_L19:
       cmp       rdx,2
       jbe       short M00_L20
       vmovsd    xmm0,qword ptr [7FF9778BC0C0]
       jmp       short M00_L21
M00_L20:
       vmovq     xmm0,rdx
M00_L21:
       vmovsd    qword ptr [rsp+128],xmm0
       mov       r14d,1
       jmp       short M00_L23
M00_L22:
       lea       rcx,[rsp+118]
       lea       rdx,[rsp+128]
       call      qword ptr [7FF977C3D410]
       mov       r14d,eax
M00_L23:
       test      r14d,r14d
       jne       short M00_L24
       vmovsd    xmm0,qword ptr [7FF9778BC0C0]
       jmp       short M00_L25
M00_L24:
       vmovsd    xmm0,qword ptr [rsp+128]
M00_L25:
       cmp       dword ptr [rdi+8],7
       jbe       near ptr M00_L173
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       r8d,7FFFFFFF
       vcvttsd2si ecx,xmm1
       vucomisd  xmm0,qword ptr [7FF9778BC0C8]
       cmovb     r8d,ecx
       mov       [rdi+2C],r8d
       jmp       near ptr M00_L05
M00_L26:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],7
       jg        short M00_L27
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,7
       call      qword ptr [7FF977C3D3F8]
M00_L27:
       mov       rdi,[rdi+38]
       xor       ecx,ecx
       mov       [rsp+110],rcx
       mov       [rsp+100],r14
       mov       [rsp+108],r15
       cmp       qword ptr [rsp+100],0
       je        short M00_L28
       mov       rcx,[rsp+100]
       mov       rdx,0CCAD800448
       cmp       rcx,[rdx]
       je        short M00_L32
       mov       rcx,[rsp+100]
       mov       rdx,0CCAD800450
       cmp       rcx,[rdx]
       je        short M00_L32
       movsx     rcx,word ptr [rsp+108]
       jmp       short M00_L31
M00_L28:
       mov       rcx,[rsp+108]
       test      rcx,rcx
       je        short M00_L29
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L30
       mov       ecx,2
       jmp       short M00_L31
M00_L29:
       xor       ecx,ecx
       jmp       short M00_L31
M00_L30:
       mov       ecx,1
M00_L31:
       test      ecx,ecx
       jne       short M00_L32
       xor       ecx,ecx
       mov       [rsp+110],rcx
       jmp       near ptr M00_L38
M00_L32:
       cmp       qword ptr [rsp+100],0
       jne       short M00_L36
       cmp       qword ptr [rsp+108],2
       jbe       short M00_L36
       mov       rcx,[rsp+108]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L33
       vmovq     xmm0,rcx
       jmp       short M00_L35
M00_L33:
       cmp       rdx,2
       jbe       short M00_L34
       vmovsd    xmm0,qword ptr [7FF9778BC0C0]
       jmp       short M00_L35
M00_L34:
       vmovq     xmm0,rdx
M00_L35:
       vmovsd    qword ptr [rsp+110],xmm0
       mov       r14d,1
       jmp       short M00_L37
M00_L36:
       lea       rcx,[rsp+100]
       lea       rdx,[rsp+110]
       call      qword ptr [7FF977C3D410]
       mov       r14d,eax
M00_L37:
       test      r14d,r14d
       jne       short M00_L38
       vmovsd    xmm0,qword ptr [7FF9778BC0C0]
       jmp       short M00_L39
M00_L38:
       vmovsd    xmm0,qword ptr [rsp+110]
M00_L39:
       cmp       dword ptr [rdi+8],7
       jbe       near ptr M00_L173
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       edx,7FFFFFFF
       vcvttsd2si ecx,xmm1
       vucomisd  xmm0,qword ptr [7FF9778BC0C8]
       cmovb     edx,ecx
       mov       [rdi+17],dl
       jmp       near ptr M00_L05
M00_L40:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],7
       jg        short M00_L41
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,7
       call      qword ptr [7FF977C3D3F8]
M00_L41:
       mov       rdi,[rdi+38]
       xor       ecx,ecx
       mov       [rsp+0F8],rcx
       mov       [rsp+0E8],r14
       mov       [rsp+0F0],r15
       cmp       qword ptr [rsp+0E8],0
       je        short M00_L42
       mov       rcx,[rsp+0E8]
       mov       rdx,0CCAD800448
       cmp       rcx,[rdx]
       je        short M00_L46
       mov       rcx,[rsp+0E8]
       mov       rdx,0CCAD800450
       cmp       rcx,[rdx]
       je        short M00_L46
       movsx     rcx,word ptr [rsp+0F0]
       jmp       short M00_L45
M00_L42:
       mov       rcx,[rsp+0F0]
       test      rcx,rcx
       je        short M00_L43
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L44
       mov       ecx,2
       jmp       short M00_L45
M00_L43:
       xor       ecx,ecx
       jmp       short M00_L45
M00_L44:
       mov       ecx,1
M00_L45:
       test      ecx,ecx
       jne       short M00_L46
       xor       ecx,ecx
       mov       [rsp+0F8],rcx
       jmp       near ptr M00_L52
M00_L46:
       cmp       qword ptr [rsp+0E8],0
       jne       short M00_L50
       cmp       qword ptr [rsp+0F0],2
       jbe       short M00_L50
       mov       rcx,[rsp+0F0]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L47
       vmovq     xmm0,rcx
       jmp       short M00_L49
M00_L47:
       cmp       rdx,2
       jbe       short M00_L48
       vmovsd    xmm0,qword ptr [7FF9778BC0C0]
       jmp       short M00_L49
M00_L48:
       vmovq     xmm0,rdx
M00_L49:
       vmovsd    qword ptr [rsp+0F8],xmm0
       mov       r14d,1
       jmp       short M00_L51
M00_L50:
       lea       rcx,[rsp+0E8]
       lea       rdx,[rsp+0F8]
       call      qword ptr [7FF977C3D410]
       mov       r14d,eax
M00_L51:
       test      r14d,r14d
       jne       short M00_L52
       vmovsd    xmm0,qword ptr [7FF9778BC0C0]
       jmp       short M00_L53
M00_L52:
       vmovsd    xmm0,qword ptr [rsp+0F8]
M00_L53:
       cmp       dword ptr [rdi+8],7
       jbe       near ptr M00_L173
       vcvtsd2ss xmm0,xmm0,xmm0
       vmovss    dword ptr [rdi+2C],xmm0
       jmp       near ptr M00_L05
M00_L54:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],7
       jg        short M00_L55
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,7
       call      qword ptr [7FF977C3D3F8]
M00_L55:
       mov       rdi,[rdi+38]
       xor       ecx,ecx
       mov       [rsp+0E0],rcx
       mov       [rsp+0D0],r14
       mov       [rsp+0D8],r15
       cmp       qword ptr [rsp+0D0],0
       je        short M00_L56
       mov       rcx,[rsp+0D0]
       mov       rdx,0CCAD800448
       cmp       rcx,[rdx]
       je        short M00_L60
       mov       rcx,[rsp+0D0]
       mov       rdx,0CCAD800450
       cmp       rcx,[rdx]
       je        short M00_L60
       movsx     rcx,word ptr [rsp+0D8]
       jmp       short M00_L59
M00_L56:
       mov       rcx,[rsp+0D8]
       test      rcx,rcx
       je        short M00_L57
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L58
       mov       ecx,2
       jmp       short M00_L59
M00_L57:
       xor       ecx,ecx
       jmp       short M00_L59
M00_L58:
       mov       ecx,1
M00_L59:
       test      ecx,ecx
       jne       short M00_L60
       xor       ecx,ecx
       mov       [rsp+0E0],rcx
       jmp       near ptr M00_L66
M00_L60:
       cmp       qword ptr [rsp+0D0],0
       jne       short M00_L64
       cmp       qword ptr [rsp+0D8],2
       jbe       short M00_L64
       mov       rcx,[rsp+0D8]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L61
       vmovq     xmm0,rcx
       jmp       short M00_L63
M00_L61:
       cmp       rdx,2
       jbe       short M00_L62
       vmovsd    xmm0,qword ptr [7FF9778BC0C0]
       jmp       short M00_L63
M00_L62:
       vmovq     xmm0,rdx
M00_L63:
       vmovsd    qword ptr [rsp+0E0],xmm0
       mov       r14d,1
       jmp       short M00_L65
M00_L64:
       lea       rcx,[rsp+0D0]
       lea       rdx,[rsp+0E0]
       call      qword ptr [7FF977C3D410]
       mov       r14d,eax
M00_L65:
       test      r14d,r14d
       jne       short M00_L66
       vmovsd    xmm0,qword ptr [7FF9778BC0C0]
       jmp       short M00_L67
M00_L66:
       vmovsd    xmm0,qword ptr [rsp+0E0]
M00_L67:
       cmp       dword ptr [rdi+8],7
       jbe       near ptr M00_L173
       vmovsd    qword ptr [rdi+48],xmm0
       jmp       near ptr M00_L05
M00_L68:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],7
       jg        short M00_L69
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,7
       call      qword ptr [7FF977C3D3F8]
M00_L69:
       mov       rdi,[rdi+38]
       mov       [rsp+0C0],r14
       mov       [rsp+0C8],r15
       cmp       qword ptr [rsp+0C0],0
       je        short M00_L72
       mov       rdx,[rsp+0C0]
       mov       rcx,0CCAD800448
       cmp       rdx,[rcx]
       jne       short M00_L70
       mov       r14d,1000
       jmp       short M00_L75
M00_L70:
       mov       rdx,[rsp+0C0]
       mov       rcx,0CCAD800450
       cmp       rdx,[rcx]
       jne       short M00_L71
       mov       r14d,2000
       jmp       short M00_L75
M00_L71:
       movsx     r14,word ptr [rsp+0C8]
       jmp       short M00_L75
M00_L72:
       mov       rdx,[rsp+0C8]
       test      rdx,rdx
       je        short M00_L73
       dec       rdx
       cmp       rdx,1
       jbe       short M00_L74
       mov       r14d,2
       jmp       short M00_L75
M00_L73:
       xor       r14d,r14d
       jmp       short M00_L75
M00_L74:
       mov       r14d,1
M00_L75:
       cmp       r14d,4
       ja        short M00_L76
       mov       edx,r14d
       lea       rcx,[7FF9778BC0D0]
       mov       ecx,[rcx+rdx*4]
       lea       rax,[M00_L01]
       add       rcx,rax
       jmp       rcx
M00_L76:
       cmp       r14d,1000
       je        near ptr M00_L78
       cmp       r14d,2000
       je        near ptr M00_L78
       lea       rcx,[rsp+0C0]
       call      qword ptr [7FF977C35E60]; AuroraScript.Runtime.ScriptDatum.get_Object()
       mov       r15,rax
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rdx,0CCAD800408
       cmp       r15,[rdx]
       setne     r15b
       movzx     r15d,r15b
       jmp       near ptr M00_L81
       cmp       qword ptr [rsp+0C8],2
       sete      r15b
       movzx     r15d,r15b
       jmp       near ptr M00_L81
       mov       rcx,7FF8000000000001
       cmp       [rsp+0C8],rcx
       je        short M00_L77
       mov       rcx,8000000000000000
       cmp       [rsp+0C8],rcx
       je        short M00_L77
       mov       rcx,7FF8000000000004
       cmp       [rsp+0C8],rcx
       setne     r15b
       movzx     r15d,r15b
       jmp       short M00_L81
M00_L77:
       xor       r15d,r15d
       jmp       short M00_L81
M00_L78:
       cmp       qword ptr [rsp+0C8],0
       setne     r15b
       movzx     r15d,r15b
       jmp       short M00_L81
       mov       rdx,[rsp+0C0]
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rax,rax
       je        short M00_L79
       cmp       dword ptr [rax+8],0
       sete      cl
       movzx     ecx,cl
       jmp       short M00_L80
M00_L79:
       mov       ecx,1
M00_L80:
       test      ecx,ecx
       sete      r15b
       movzx     r15d,r15b
M00_L81:
       cmp       dword ptr [rdi+8],7
       jbe       near ptr M00_L173
       mov       [rdi+17],r15b
       jmp       near ptr M00_L05
M00_L82:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],7
       jg        short M00_L83
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,7
       call      qword ptr [7FF977C3D3F8]
M00_L83:
       mov       rdi,[rdi+38]
       xor       ecx,ecx
       mov       [rsp+0B8],rcx
       mov       [rsp+0A8],r14
       mov       [rsp+0B0],r15
       cmp       qword ptr [rsp+0A8],0
       je        short M00_L84
       mov       rcx,[rsp+0A8]
       mov       rdx,0CCAD800448
       cmp       rcx,[rdx]
       je        short M00_L88
       mov       rcx,[rsp+0A8]
       mov       rdx,0CCAD800450
       cmp       rcx,[rdx]
       je        short M00_L88
       movsx     rcx,word ptr [rsp+0B0]
       jmp       short M00_L87
M00_L84:
       mov       rcx,[rsp+0B0]
       test      rcx,rcx
       je        short M00_L85
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L86
       mov       ecx,2
       jmp       short M00_L87
M00_L85:
       xor       ecx,ecx
       jmp       short M00_L87
M00_L86:
       mov       ecx,1
M00_L87:
       test      ecx,ecx
       jne       short M00_L88
       xor       ecx,ecx
       mov       [rsp+0B8],rcx
       jmp       near ptr M00_L94
M00_L88:
       cmp       qword ptr [rsp+0A8],0
       jne       short M00_L92
       cmp       qword ptr [rsp+0B0],2
       jbe       short M00_L92
       mov       rcx,[rsp+0B0]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L89
       vmovq     xmm0,rcx
       jmp       short M00_L91
M00_L89:
       cmp       rdx,2
       jbe       short M00_L90
       vmovsd    xmm0,qword ptr [7FF9778BC0C0]
       jmp       short M00_L91
M00_L90:
       vmovq     xmm0,rdx
M00_L91:
       vmovsd    qword ptr [rsp+0B8],xmm0
       mov       r14d,1
       jmp       short M00_L93
M00_L92:
       lea       rcx,[rsp+0A8]
       lea       rdx,[rsp+0B8]
       call      qword ptr [7FF977C3D410]
       mov       r14d,eax
M00_L93:
       test      r14d,r14d
       jne       short M00_L94
       vmovsd    xmm0,qword ptr [7FF9778BC0C0]
       jmp       short M00_L95
M00_L94:
       vmovsd    xmm0,qword ptr [rsp+0B8]
M00_L95:
       cmp       dword ptr [rdi+8],7
       jbe       near ptr M00_L173
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       edx,7FFFFFFF
       vcvttsd2si ecx,xmm1
       vucomisd  xmm0,qword ptr [7FF9778BC0C8]
       cmovb     edx,ecx
       mov       [rdi+17],dl
       jmp       near ptr M00_L05
M00_L96:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],7
       jg        short M00_L97
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,7
       call      qword ptr [7FF977C3D3F8]
M00_L97:
       mov       rdi,[rdi+38]
       xor       ecx,ecx
       mov       [rsp+0A0],rcx
       mov       [rsp+90],r14
       mov       [rsp+98],r15
       cmp       qword ptr [rsp+90],0
       je        short M00_L98
       mov       rcx,[rsp+90]
       mov       rdx,0CCAD800448
       cmp       rcx,[rdx]
       je        short M00_L102
       mov       rcx,[rsp+90]
       mov       rdx,0CCAD800450
       cmp       rcx,[rdx]
       je        short M00_L102
       movsx     rcx,word ptr [rsp+98]
       jmp       short M00_L101
M00_L98:
       mov       rcx,[rsp+98]
       test      rcx,rcx
       je        short M00_L99
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L100
       mov       ecx,2
       jmp       short M00_L101
M00_L99:
       xor       ecx,ecx
       jmp       short M00_L101
M00_L100:
       mov       ecx,1
M00_L101:
       test      ecx,ecx
       jne       short M00_L102
       xor       ecx,ecx
       mov       [rsp+0A0],rcx
       jmp       near ptr M00_L108
M00_L102:
       cmp       qword ptr [rsp+90],0
       jne       short M00_L106
       cmp       qword ptr [rsp+98],2
       jbe       short M00_L106
       mov       rcx,[rsp+98]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L103
       vmovq     xmm0,rcx
       jmp       short M00_L105
M00_L103:
       cmp       rdx,2
       jbe       short M00_L104
       vmovsd    xmm0,qword ptr [7FF9778BC0C0]
       jmp       short M00_L105
M00_L104:
       vmovq     xmm0,rdx
M00_L105:
       vmovsd    qword ptr [rsp+0A0],xmm0
       mov       r14d,1
       jmp       short M00_L107
M00_L106:
       lea       rcx,[rsp+90]
       lea       rdx,[rsp+0A0]
       call      qword ptr [7FF977C3D410]
       mov       r14d,eax
M00_L107:
       test      r14d,r14d
       jne       short M00_L108
       vmovsd    xmm0,qword ptr [7FF9778BC0C0]
       jmp       short M00_L109
M00_L108:
       vmovsd    xmm0,qword ptr [rsp+0A0]
M00_L109:
       cmp       dword ptr [rdi+8],7
       jbe       near ptr M00_L173
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       edx,7FFFFFFF
       vcvttsd2si ecx,xmm1
       vucomisd  xmm0,qword ptr [7FF9778BC0C8]
       cmovb     edx,ecx
       mov       [rdi+1E],dx
       jmp       near ptr M00_L05
M00_L110:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],7
       jg        short M00_L111
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,7
       call      qword ptr [7FF977C3D3F8]
M00_L111:
       mov       rdi,[rdi+38]
       xor       ecx,ecx
       mov       [rsp+88],rcx
       mov       [rsp+78],r14
       mov       [rsp+80],r15
       cmp       qword ptr [rsp+78],0
       je        short M00_L112
       mov       rcx,[rsp+78]
       mov       rdx,0CCAD800448
       cmp       rcx,[rdx]
       je        short M00_L116
       mov       rcx,[rsp+78]
       mov       rdx,0CCAD800450
       cmp       rcx,[rdx]
       je        short M00_L116
       movsx     rcx,word ptr [rsp+80]
       jmp       short M00_L115
M00_L112:
       mov       rcx,[rsp+80]
       test      rcx,rcx
       je        short M00_L113
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L114
       mov       ecx,2
       jmp       short M00_L115
M00_L113:
       xor       ecx,ecx
       jmp       short M00_L115
M00_L114:
       mov       ecx,1
M00_L115:
       test      ecx,ecx
       jne       short M00_L116
       xor       ecx,ecx
       mov       [rsp+88],rcx
       jmp       near ptr M00_L122
M00_L116:
       cmp       qword ptr [rsp+78],0
       jne       short M00_L120
       cmp       qword ptr [rsp+80],2
       jbe       short M00_L120
       mov       rcx,[rsp+80]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L117
       vmovq     xmm0,rcx
       jmp       short M00_L119
M00_L117:
       cmp       rdx,2
       jbe       short M00_L118
       vmovsd    xmm0,qword ptr [7FF9778BC0C0]
       jmp       short M00_L119
M00_L118:
       vmovq     xmm0,rdx
M00_L119:
       vmovsd    qword ptr [rsp+88],xmm0
       mov       r14d,1
       jmp       short M00_L121
M00_L120:
       lea       rcx,[rsp+78]
       lea       rdx,[rsp+88]
       call      qword ptr [7FF977C3D410]
       mov       r14d,eax
M00_L121:
       test      r14d,r14d
       jne       short M00_L122
       vmovsd    xmm0,qword ptr [7FF9778BC0C0]
       jmp       short M00_L123
M00_L122:
       vmovsd    xmm0,qword ptr [rsp+88]
M00_L123:
       cmp       dword ptr [rdi+8],7
       jbe       near ptr M00_L173
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       edx,7FFFFFFF
       vcvttsd2si ecx,xmm1
       vucomisd  xmm0,qword ptr [7FF9778BC0C8]
       cmovb     edx,ecx
       mov       [rdi+1E],dx
       jmp       near ptr M00_L05
M00_L124:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],7
       jg        short M00_L125
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,7
       call      qword ptr [7FF977C3D3F8]
M00_L125:
       mov       rdi,[rdi+38]
       xor       ecx,ecx
       mov       [rsp+70],rcx
       mov       [rsp+60],r14
       mov       [rsp+68],r15
       cmp       qword ptr [rsp+60],0
       je        short M00_L126
       mov       rcx,[rsp+60]
       mov       rdx,0CCAD800448
       cmp       rcx,[rdx]
       je        short M00_L130
       mov       rcx,[rsp+60]
       mov       rdx,0CCAD800450
       cmp       rcx,[rdx]
       je        short M00_L130
       movsx     rcx,word ptr [rsp+68]
       jmp       short M00_L129
M00_L126:
       mov       rcx,[rsp+68]
       test      rcx,rcx
       je        short M00_L127
       dec       rcx
       cmp       rcx,1
       jbe       short M00_L128
       mov       ecx,2
       jmp       short M00_L129
M00_L127:
       xor       ecx,ecx
       jmp       short M00_L129
M00_L128:
       mov       ecx,1
M00_L129:
       test      ecx,ecx
       jne       short M00_L130
       xor       ecx,ecx
       mov       [rsp+70],rcx
       jmp       short M00_L136
M00_L130:
       cmp       qword ptr [rsp+60],0
       jne       short M00_L134
       cmp       qword ptr [rsp+68],2
       jbe       short M00_L134
       mov       rcx,[rsp+68]
       mov       rdx,8007FFFFFFFFFFFF
       add       rdx,rcx
       cmp       rdx,3
       jbe       short M00_L131
       vmovq     xmm0,rcx
       jmp       short M00_L133
M00_L131:
       cmp       rdx,2
       jbe       short M00_L132
       vmovsd    xmm0,qword ptr [7FF9778BC0C0]
       jmp       short M00_L133
M00_L132:
       vmovq     xmm0,rdx
M00_L133:
       vmovsd    qword ptr [rsp+70],xmm0
       mov       r14d,1
       jmp       short M00_L135
M00_L134:
       lea       rcx,[rsp+60]
       lea       rdx,[rsp+70]
       call      qword ptr [7FF977C3D410]
       mov       r14d,eax
M00_L135:
       test      r14d,r14d
       jne       short M00_L136
       vmovsd    xmm0,qword ptr [7FF9778BC0C0]
       jmp       short M00_L137
M00_L136:
       vmovsd    xmm0,qword ptr [rsp+70]
M00_L137:
       cmp       dword ptr [rdi+8],7
       jbe       near ptr M00_L173
       vxorps    xmm1,xmm1,xmm1
       vmaxsd    xmm1,xmm0,xmm1
       mov       edx,0FFFFFFFF
       vcvttsd2si rcx,xmm1
       vucomisd  xmm0,qword ptr [7FF9778BC0E8]
       cmovb     edx,ecx
       mov       [rdi+2C],edx
       jmp       near ptr M00_L05
M00_L138:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],7
       jg        short M00_L139
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,7
       call      qword ptr [7FF977C3D3F8]
M00_L139:
       mov       rdi,[rdi+38]
       test      r14,r14
       je        short M00_L142
       mov       r8,0CCAD800448
       cmp       r14,[r8]
       jne       short M00_L140
       mov       r8d,1000
       jmp       short M00_L145
M00_L140:
       mov       r8,0CCAD800450
       cmp       r14,[r8]
       jne       short M00_L141
       mov       r8d,2000
       jmp       short M00_L145
M00_L141:
       movsx     r8,r15w
       jmp       short M00_L145
M00_L142:
       test      r15,r15
       je        short M00_L143
       lea       r8,[r15-1]
       cmp       r8,1
       jbe       short M00_L144
       mov       r8d,2
       jmp       short M00_L145
M00_L143:
       xor       r8d,r8d
       jmp       short M00_L145
M00_L144:
       mov       r8d,1
M00_L145:
       cmp       r8d,2
       je        short M00_L147
       cmp       r8d,1000
       je        short M00_L146
       cmp       r8d,2000
       jne       near ptr M00_L152
       test      r15,r15
       jl        near ptr M00_L152
M00_L146:
       mov       rax,r15
       jmp       near ptr M00_L153
M00_L147:
       mov       rax,8007FFFFFFFFFFFF
       add       rax,r15
       cmp       rax,3
       jbe       short M00_L148
       vmovq     xmm0,r15
       jmp       short M00_L150
M00_L148:
       cmp       rax,2
       jbe       short M00_L149
       vmovsd    xmm0,qword ptr [7FF9778BC0C0]
       jmp       short M00_L150
M00_L149:
       vmovq     xmm0,rax
M00_L150:
       vucomisd  xmm0,qword ptr [7FF9778BC0F0]
       jb        short M00_L152
       vmovsd    xmm1,qword ptr [7FF9778BC0F8]
       vucomisd  xmm1,xmm0
       jbe       short M00_L152
       vroundsd  xmm1,xmm1,xmm0,0B
       vucomisd  xmm1,xmm0
       jp        short M00_L152
       jne       short M00_L152
       vxorps    xmm1,xmm1,xmm1
       vucomisd  xmm0,xmm1
       jp        short M00_L151
       jne       short M00_L151
       vmovq     r8,xmm0
       not       r8
       shr       r8,3F
       test      r8d,r8d
       je        short M00_L152
M00_L151:
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       r8,7FFFFFFFFFFFFFFF
       vcvttsd2si rcx,xmm1
       vucomisd  xmm0,qword ptr [7FF9778BC0F8]
       cmovb     r8,rcx
       mov       rax,r8
       jmp       short M00_L153
M00_L152:
       mov       [rsp+30],r14
       mov       [rsp+38],r15
       lea       r8,[rsp+30]
       lea       rcx,[rsp+50]
       mov       edx,13
       call      qword ptr [7FF977C3D428]
       xor       eax,eax
M00_L153:
       cmp       dword ptr [rdi+8],7
       jbe       near ptr M00_L173
       mov       [rdi+48],rax
       jmp       near ptr M00_L05
M00_L154:
       mov       rdx,[rdi+38]
       cmp       dword ptr [rdx+8],7
       jg        short M00_L155
       mov       rdx,[rdi+38]
       mov       edx,[rdx+8]
       mov       ecx,7
       call      qword ptr [7FF977C3D3F8]
M00_L155:
       mov       rdi,[rdi+38]
       test      r14,r14
       je        short M00_L158
       mov       r8,0CCAD800448
       cmp       r14,[r8]
       jne       short M00_L156
       mov       r8d,1000
       jmp       short M00_L161
M00_L156:
       mov       r8,0CCAD800450
       cmp       r14,[r8]
       jne       short M00_L157
       mov       r8d,2000
       jmp       short M00_L161
M00_L157:
       movsx     r8,r15w
       jmp       short M00_L161
M00_L158:
       test      r15,r15
       je        short M00_L159
       lea       r8,[r15-1]
       cmp       r8,1
       jbe       short M00_L160
       mov       r8d,2
       jmp       short M00_L161
M00_L159:
       xor       r8d,r8d
       jmp       short M00_L161
M00_L160:
       mov       r8d,1
M00_L161:
       cmp       r8d,2
       je        short M00_L164
       cmp       r8d,1000
       je        short M00_L162
       cmp       r8d,2000
       jne       near ptr M00_L169
       jmp       short M00_L163
M00_L162:
       test      r15,r15
       jl        near ptr M00_L169
M00_L163:
       jmp       near ptr M00_L170
M00_L164:
       mov       r8,8007FFFFFFFFFFFF
       add       r8,r15
       cmp       r8,3
       jbe       short M00_L165
       vmovq     xmm0,r15
       jmp       short M00_L167
M00_L165:
       cmp       r8,2
       jbe       short M00_L166
       vmovsd    xmm0,qword ptr [7FF9778BC0C0]
       jmp       short M00_L167
M00_L166:
       vmovq     xmm0,r8
M00_L167:
       vxorps    xmm1,xmm1,xmm1
       vucomisd  xmm0,xmm1
       jb        short M00_L169
       vmovsd    xmm1,qword ptr [7FF9778BC100]
       vucomisd  xmm1,xmm0
       jbe       short M00_L169
       vroundsd  xmm1,xmm1,xmm0,0B
       vucomisd  xmm1,xmm0
       jp        short M00_L169
       jne       short M00_L169
       vxorps    xmm1,xmm1,xmm1
       vucomisd  xmm0,xmm1
       jp        short M00_L168
       jne       short M00_L168
       vmovq     r8,xmm0
       not       r8
       shr       r8,3F
       test      r8d,r8d
       je        short M00_L169
M00_L168:
       vxorps    xmm1,xmm1,xmm1
       vmaxsd    xmm1,xmm0,xmm1
       vmovsd    xmm2,qword ptr [7FF9778BC100]
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
       jmp       short M00_L170
M00_L169:
       mov       [rsp+30],r14
       mov       [rsp+38],r15
       lea       r8,[rsp+30]
       lea       rcx,[rsp+40]
       mov       edx,14
       call      qword ptr [7FF977C3D428]
       xor       r15d,r15d
M00_L170:
       cmp       dword ptr [rdi+8],7
       jbe       short M00_L173
       mov       [rdi+48],r15
       jmp       near ptr M00_L05
M00_L171:
       mov       rcx,r13
       mov       rax,[r13]
       mov       rax,[rax+58]
       call      qword ptr [rax+8]
       mov       edx,eax
       mov       ecx,7
       call      qword ptr [7FF977C3D3F8]
M00_L172:
       mov       [rsp+30],r14
       mov       [rsp+38],r15
       lea       r8,[rsp+30]
       mov       rcx,r13
       mov       edx,7
       mov       rax,[r13]
       mov       rax,[rax+58]
       call      qword ptr [rax+18]
       jmp       near ptr M00_L05
M00_L173:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 4920
```
```assembly
; AuroraScript.Runtime.ObjectOps.SetNumericProperty(AuroraScript.Runtime.ScriptDatum, Double, AuroraScript.Runtime.ScriptDatum)
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,48
       vxorps    xmm4,xmm4,xmm4
       vmovdqu   ymmword ptr [rsp+20],ymm4
       xor       eax,eax
       mov       [rsp+40],rax
       vmovsd    qword ptr [rsp+80],xmm2
       mov       rsi,rcx
       mov       rbx,r9
M01_L00:
       vmovdqu   xmm1,xmmword ptr [rdx]
       vmovdqu   xmmword ptr [rsp+38],xmm1
       cmp       qword ptr [rsp+38],0
       je        near ptr M01_L07
       mov       rcx,[rsp+38]
       mov       rax,0CCAD800448
       cmp       rcx,[rax]
       je        near ptr M01_L05
       mov       rcx,[rsp+38]
       mov       rax,0CCAD800450
       movsx     rdx,word ptr [rsp+40]
       mov       r8d,2000
       cmp       rcx,[rax]
       cmove     edx,r8d
M01_L01:
       cmp       edx,4
       jbe       near ptr M01_L06
       cmp       edx,1000
       je        near ptr M01_L30
       cmp       edx,2000
       je        near ptr M01_L31
       mov       rdx,[rsp+38]
       mov       rdi,rdx
       test      rdi,rdi
       je        short M01_L02
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       cmp       [rdi],rcx
       jne       near ptr M01_L10
M01_L02:
       test      rdi,rdi
       je        near ptr M01_L32
M01_L03:
       call      qword ptr [7FF97783D2A8]; System.Globalization.NumberFormatInfo.get_CurrentInfo()
       mov       r8,rax
       vmovsd    xmm0,qword ptr [rsp+80]
       xor       edx,edx
       call      qword ptr [7FF977BB7108]; System.Number.FormatFloat[[System.Double, System.Private.CoreLib]](Double, System.String, System.Globalization.NumberFormatInfo)
       mov       r9,offset MT_AuroraScript.Runtime.Types.ScriptObject
       cmp       [rdi],r9
       jne       near ptr M01_L36
       vmovdqu   xmm0,xmmword ptr [rbx]
       vmovdqu   xmmword ptr [rsp+20],xmm0
       lea       r9,[rsp+20]
       mov       rcx,rdi
       mov       r8,rax
       xor       edx,edx
       call      qword ptr [7FF977ABBCB0]; AuroraScript.Runtime.Types.ScriptObject.SetPropertyDatum(AuroraScript.Runtime.ScriptContext, System.String, AuroraScript.Runtime.ScriptDatum)
M01_L04:
       vmovdqu   xmm0,xmmword ptr [rbx]
       vmovdqu   xmmword ptr [rsi],xmm0
       mov       rax,rsi
       add       rsp,48
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       ret
M01_L05:
       mov       edx,1000
       jmp       near ptr M01_L01
M01_L06:
       mov       ecx,edx
       lea       rax,[7FF9778BA770]
       mov       eax,[rax+rcx*4]
       lea       rdx,[M01_L00]
       add       rax,rdx
       jmp       rax
M01_L07:
       mov       rdx,[rsp+40]
       test      rdx,rdx
       je        short M01_L09
       dec       rdx
       mov       ecx,1
       mov       eax,2
       cmp       rdx,1
       cmova     ecx,eax
       mov       edx,ecx
M01_L08:
       jmp       near ptr M01_L01
M01_L09:
       xor       edx,edx
       jmp       short M01_L08
M01_L10:
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rdi,rax
       jmp       near ptr M01_L02
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rax,0CCAD800408
       mov       rdi,[rax]
       jmp       near ptr M01_L03
       cmp       qword ptr [rsp+40],2
       je        short M01_L11
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BooleanValue
       call      qword ptr [7FF977835740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0CCAD801510
       mov       rdi,[rcx]
       jmp       short M01_L12
M01_L11:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BooleanValue
       call      qword ptr [7FF977835740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0CCAD801508
       mov       rdi,[rcx]
M01_L12:
       jmp       near ptr M01_L03
       mov       rcx,[rsp+40]
       mov       rax,8007FFFFFFFFFFFF
       add       rax,rcx
       cmp       rax,3
       jbe       short M01_L13
       vmovq     xmm1,rcx
       jmp       short M01_L15
M01_L13:
       cmp       rax,2
       jbe       short M01_L14
       vmovsd    xmm1,qword ptr [7FF9778BA788]
       jmp       short M01_L15
M01_L14:
       vmovq     xmm1,rax
M01_L15:
       vucomisd  xmm1,xmm1
       jp        short M01_L16
       je        short M01_L17
M01_L16:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977835740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0CCAD8014F8
       mov       rdi,[rcx]
       jmp       near ptr M01_L29
M01_L17:
       vxorps    xmm2,xmm2,xmm2
       vucomisd  xmm1,xmm2
       jp        short M01_L18
       jne       short M01_L18
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977835740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0CCAD8014A8
       mov       rdi,[rcx]
       jmp       near ptr M01_L29
M01_L18:
       vucomisd  xmm1,qword ptr [7FF9778BA790]
       jp        short M01_L19
       jne       short M01_L19
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977835740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0CCAD8014B0
       mov       rdi,[rcx]
       jmp       near ptr M01_L29
M01_L19:
       vucomisd  xmm1,qword ptr [7FF9778BA798]
       jp        short M01_L20
       jne       short M01_L20
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977835740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0CCAD8014B8
       mov       rdi,[rcx]
       jmp       near ptr M01_L29
M01_L20:
       vucomisd  xmm1,qword ptr [7FF9778BA7A0]
       jp        short M01_L21
       jne       short M01_L21
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977835740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0CCAD8014C0
       mov       rdi,[rcx]
       jmp       near ptr M01_L29
M01_L21:
       vucomisd  xmm1,qword ptr [7FF9778BA7A8]
       jp        short M01_L22
       jne       short M01_L22
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977835740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0CCAD8014C8
       mov       rdi,[rcx]
       jmp       near ptr M01_L29
M01_L22:
       vucomisd  xmm1,qword ptr [7FF9778BA7B0]
       jp        short M01_L23
       jne       short M01_L23
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977835740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0CCAD8014D0
       mov       rdi,[rcx]
       jmp       near ptr M01_L29
M01_L23:
       vucomisd  xmm1,qword ptr [7FF9778BA7B8]
       jp        short M01_L24
       jne       short M01_L24
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977835740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0CCAD8014D8
       mov       rdi,[rcx]
       jmp       near ptr M01_L29
M01_L24:
       vucomisd  xmm1,qword ptr [7FF9778BA7C0]
       jp        short M01_L25
       jne       short M01_L25
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977835740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0CCAD8014E0
       mov       rdi,[rcx]
       jmp       near ptr M01_L29
M01_L25:
       vucomisd  xmm1,qword ptr [7FF9778BA7C8]
       jp        short M01_L26
       jne       short M01_L26
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977835740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0CCAD8014E8
       mov       rdi,[rcx]
       jmp       near ptr M01_L29
M01_L26:
       vucomisd  xmm1,qword ptr [7FF9778BA7D0]
       jp        short M01_L27
       jne       short M01_L27
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977835740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0CCAD8014F0
       mov       rdi,[rcx]
       jmp       short M01_L29
M01_L27:
       vmovsd    qword ptr [rsp+30],xmm1
       vucomisd  xmm1,qword ptr [7FF9778BA7D8]
       jp        short M01_L28
       jne       short M01_L28
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      qword ptr [7FF977835740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0CCAD8014A0
       mov       rdi,[rcx]
       jmp       short M01_L29
M01_L28:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.NumberValue
       call      CORINFO_HELP_NEWSFAST
       mov       rdi,rax
       mov       rcx,rdi
       vmovsd    xmm1,qword ptr [rsp+30]
       call      qword ptr [7FF977C3D440]
M01_L29:
       jmp       near ptr M01_L03
M01_L30:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.Int64Value
       call      CORINFO_HELP_NEWSFAST
       mov       rdi,rax
       mov       rcx,rdi
       mov       rdx,[rsp+40]
       call      qword ptr [7FF977C35E00]
       jmp       near ptr M01_L03
M01_L31:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.UInt64Value
       call      CORINFO_HELP_NEWSFAST
       mov       rdi,rax
       mov       rcx,rdi
       mov       rdx,[rsp+40]
       call      qword ptr [7FF977C35E30]
       jmp       near ptr M01_L03
       lea       rcx,[rsp+38]
       call      qword ptr [7FF977C35E48]
       mov       rdi,rax
       jmp       near ptr M01_L03
M01_L32:
       mov       rdx,[rsp+38]
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbp,rax
       test      rbp,rbp
       jne       short M01_L33
       xor       edi,edi
       jmp       near ptr M01_L03
M01_L33:
       cmp       dword ptr [rbp+8],0
       jne       short M01_L34
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0CCAD801490
       mov       rdi,[rcx]
       jmp       short M01_L35
M01_L34:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      CORINFO_HELP_NEWSFAST
       mov       rdi,rax
       mov       rcx,rdi
       mov       rdx,rbp
       call      qword ptr [7FF977C3C9A8]
M01_L35:
       jmp       near ptr M01_L03
M01_L36:
       vmovdqu   xmm0,xmmword ptr [rbx]
       vmovdqu   xmmword ptr [rsp+20],xmm0
       lea       r9,[rsp+20]
       mov       rcx,rdi
       mov       r8,rax
       xor       edx,edx
       mov       rax,[rdi]
       mov       rax,[rax+50]
       call      qword ptr [rax+20]
       jmp       near ptr M01_L04
; Total bytes of code 1357
```
```assembly
; System.Runtime.CompilerServices.CastHelpers.IsInstanceOfInterface(Void*, System.Object)
       test      rdx,rdx
       je        short M02_L01
       mov       rax,[rdx]
       movzx     r8d,word ptr [rax+0E]
       test      r8,r8
       jne       short M02_L02
M02_L00:
       test      dword ptr [rax],500C0000
       jne       short M02_L05
       xor       edx,edx
M02_L01:
       mov       rax,rdx
       ret
M02_L02:
       mov       r10,[rax+38]
       cmp       r8,4
       jl        short M02_L04
M02_L03:
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
       jge       short M02_L03
       test      r8,r8
       je        short M02_L00
M02_L04:
       cmp       [r10],rcx
       je        short M02_L01
       add       r10,8
       dec       r8
       test      r8,r8
       jg        short M02_L04
       jmp       short M02_L00
M02_L05:
       jmp       qword ptr [7FF977BB5470]; System.Runtime.CompilerServices.CastHelpers.IsInstance_Helper(Void*, System.Object)
; Total bytes of code 109
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
; AuroraScript.Runtime.ScriptDatum.get_Object()
       push      rsi
       push      rbx
       sub       rsp,28
       mov       rbx,rcx
       mov       rdx,[rbx]
       mov       rax,rdx
       test      rax,rax
       je        short M04_L00
       mov       rcx,offset MT_AuroraScript.Runtime.Types.ScriptObject
       cmp       [rax],rcx
       jne       short M04_L01
M04_L00:
       test      rax,rax
       je        short M04_L02
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
M04_L01:
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       jmp       short M04_L00
M04_L02:
       mov       rdx,[rbx]
       mov       rcx,offset MT_System.String
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       mov       rbx,rax
       test      rbx,rbx
       jne       short M04_L03
       xor       eax,eax
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
M04_L03:
       cmp       dword ptr [rbx+8],0
       jne       short M04_L04
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0CCAD801490
       mov       rax,[rcx]
       jmp       short M04_L05
M04_L04:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      CORINFO_HELP_NEWSFAST
       mov       rsi,rax
       mov       rcx,rsi
       mov       rdx,rbx
       call      qword ptr [7FF977C3C9A8]
       mov       rax,rsi
M04_L05:
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
; Total bytes of code 165
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
       jmp       qword ptr [7FF977835C38]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBaseSlow(System.Runtime.CompilerServices.MethodTable*)
; Total bytes of code 23
```
```assembly
; System.Globalization.NumberFormatInfo.get_CurrentInfo()
       sub       rsp,28
       mov       rcx,gs:[58]
       mov       rcx,[rcx+48]
       cmp       dword ptr [rcx+238],2
       jle       short M06_L02
       mov       rcx,[rcx+240]
       mov       rax,[rcx+10]
       test      rax,rax
       je        short M06_L02
M06_L00:
       mov       rcx,[rax+10]
       test      rcx,rcx
       jne       short M06_L01
       mov       rcx,0CCAD800190
       mov       rcx,[rcx]
       test      rcx,rcx
       jne       short M06_L01
       mov       rcx,0CCAD800170
       mov       rcx,[rcx]
       test      rcx,rcx
       jne       short M06_L01
       call      qword ptr [7FF97783D4B8]; System.Globalization.CultureInfo.InitializeUserDefaultCulture()
       mov       rcx,rax
M06_L01:
       cmp       byte ptr [rcx+61],0
       jne       short M06_L03
       mov       rax,[rcx+18]
       test      rax,rax
       je        short M06_L03
       add       rsp,28
       ret
M06_L02:
       mov       ecx,2
       call      qword ptr [7FF977BBCB28]; System.Runtime.CompilerServices.StaticsHelpers.GetOptimizedGCThreadStaticBase(Int32)
       jmp       short M06_L00
M06_L03:
       mov       rdx,0ECE02C1518
       mov       rax,[rcx]
       mov       rax,[rax+50]
       call      qword ptr [rax]
       mov       rdx,rax
       mov       rcx,offset MT_System.Globalization.NumberFormatInfo
       call      qword ptr [7FF977836328]; System.Runtime.CompilerServices.CastHelpers.ChkCastClass(Void*, System.Object)
       nop
       add       rsp,28
       ret
; Total bytes of code 173
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
       mov       rax,0E0F90834C4CE
       mov       [rbp],rax
       mov       r9,r8
       test      [rsp],esp
       sub       rsp,40
       lea       r8,[rsp+20]
       mov       [rbp+28],r8
       mov       dword ptr [rbp+30],20
       test      rdx,rdx
       jne       near ptr M07_L04
       xor       r8d,r8d
       xor       ecx,ecx
M07_L00:
       mov       [rbp+8],r8
       mov       [rbp+10],ecx
       lea       r8,[rbp+8]
       lea       rcx,[rbp+18]
       vmovaps   xmm1,xmm0
       call      qword ptr [7FF977BB7120]; System.Number.FormatFloat[[System.Double, System.Private.CoreLib],[System.Char, System.Private.CoreLib]](System.Collections.Generic.ValueListBuilder`1<Char> ByRef, Double, System.ReadOnlySpan`1<Char>, System.Globalization.NumberFormatInfo)
       mov       rbx,rax
       test      rbx,rbx
       jne       short M07_L01
       mov       ecx,[rbp+20]
       cmp       ecx,[rbp+30]
       ja        short M07_L05
       mov       rax,[rbp+28]
       mov       [rbp+8],rax
       mov       [rbp+10],ecx
       lea       rcx,[rbp+8]
       call      System.String.Ctor(System.ReadOnlySpan`1<Char>)
       mov       rbx,rax
M07_L01:
       mov       rdx,[rbp+18]
       test      rdx,rdx
       je        short M07_L02
       xor       ecx,ecx
       mov       [rbp+18],rcx
       mov       rcx,0CCAD8002B0
       mov       rcx,[rcx]
       xor       r8d,r8d
       call      qword ptr [7FF977A29840]; Precode of System.Buffers.SharedArrayPool`1[[System.Char, System.Private.CoreLib]].Return(Char[], Boolean)
M07_L02:
       mov       rax,rbx
       mov       r8,0E0F90834C4CE
       cmp       [rbp],r8
       je        short M07_L03
       call      CORINFO_HELP_FAIL_FAST
M07_L03:
       nop
       lea       rsp,[rbp+38]
       pop       rbx
       pop       rbp
       ret
M07_L04:
       lea       r8,[rdx+0C]
       mov       ecx,[rdx+8]
       jmp       near ptr M07_L00
M07_L05:
       call      qword ptr [7FF977A07D50]
       int       3
; Total bytes of code 236
```
```assembly
; AuroraScript.Runtime.Types.ScriptObject.SetPropertyDatum(AuroraScript.Runtime.ScriptContext, System.String, AuroraScript.Runtime.ScriptDatum)
       push      r15
       push      r14
       push      r13
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
       mov       rsi,rcx
       mov       rbp,rdx
       mov       rdi,r8
       mov       rbx,r9
       mov       r14,[rsi+20]
       mov       r8,[r14+8]
       mov       r15d,[r8+8]
       dec       r15d
       js        near ptr M08_L22
       mov       r8,0CCAD800068
       mov       r13,[r8]
M08_L00:
       mov       r8,[r14+8]
       cmp       r15d,[r8+8]
       jae       near ptr M08_L35
       mov       edx,r15d
       shl       rdx,5
       mov       r8,[r8+rdx+10]
       cmp       r8,rdi
       je        short M08_L01
       test      r8,r8
       je        near ptr M08_L19
       test      rdi,rdi
       je        near ptr M08_L19
       mov       edx,[r8+8]
       cmp       edx,[rdi+8]
       jne       near ptr M08_L19
       lea       rcx,[r8+0C]
       mov       r8d,[r8+8]
       add       r8d,r8d
       lea       rdx,[rdi+0C]
       call      qword ptr [7FF97783C330]; System.SpanHelpers.SequenceEqual(Byte ByRef, Byte ByRef, UIntPtr)
       test      eax,eax
       je        near ptr M08_L19
M08_L01:
       mov       r8d,r15d
M08_L02:
       test      r8d,r8d
       jl        near ptr M08_L23
       mov       rcx,[r14+8]
       cmp       r8d,[rcx+8]
       jae       near ptr M08_L35
       mov       r8d,r8d
       shl       r8,5
       movzx     r8d,word ptr [rcx+r8+18]
       cmp       qword ptr [rsi+18],0
       jne       near ptr M08_L24
M08_L03:
       xor       ecx,ecx
M08_L04:
       mov       rdx,[rsi+10]
       cmp       r8d,[rdx+8]
       jae       near ptr M08_L35
       mov       r8d,r8d
       shl       r8,4
       vmovdqu   xmm0,xmmword ptr [rdx+r8+10]
       vmovdqu   xmmword ptr [rsp+38],xmm0
       vmovdqu   xmm0,xmmword ptr [rsp+30]
       vmovdqu   xmmword ptr [rsp+58],xmm0
       mov       r8,[rsp+40]
       mov       [rsp+68],r8
       mov       [rsp+58],rcx
M08_L05:
       mov       rdx,[rsp+58]
       test      rdx,rdx
       jne       near ptr M08_L27
M08_L06:
       mov       rdx,[rsp+60]
       mov       rcx,offset MT_AuroraScript.Runtime.Types.BondingAccessor
       call      System.Runtime.CompilerServices.CastHelpers.IsInstanceOfClass(Void*, System.Object)
       test      rax,rax
       jne       near ptr M08_L28
M08_L07:
       mov       ebp,3
       mov       r14,[rsi+20]
       mov       r8,[r14+8]
       mov       r15d,[r8+8]
       dec       r15d
       js        near ptr M08_L29
       mov       r8,0CCAD800068
       mov       r13,[r8]
M08_L08:
       mov       r8,[r14+8]
       cmp       r15d,[r8+8]
       jae       near ptr M08_L35
       mov       edx,r15d
       shl       rdx,5
       mov       r8,[r8+rdx+10]
       cmp       r8,rdi
       je        short M08_L09
       test      r8,r8
       je        near ptr M08_L20
       test      rdi,rdi
       je        near ptr M08_L20
       mov       edx,[r8+8]
       cmp       edx,[rdi+8]
       jne       near ptr M08_L20
       lea       rcx,[r8+0C]
       mov       r8d,[r8+8]
       add       r8d,r8d
       lea       rdx,[rdi+0C]
       call      qword ptr [7FF97783C330]; System.SpanHelpers.SequenceEqual(Byte ByRef, Byte ByRef, UIntPtr)
       test      eax,eax
       je        near ptr M08_L20
M08_L09:
       mov       eax,r15d
M08_L10:
       test      eax,eax
       jl        short M08_L11
       mov       rcx,[r14+8]
       cmp       eax,[rcx+8]
       jae       near ptr M08_L35
       mov       eax,eax
       shl       rax,5
       movzx     ebp,word ptr [rcx+rax+1C]
       and       ebp,18
       or        ebp,3
M08_L11:
       mov       eax,[rsi+30]
       test      al,2
       jne       near ptr M08_L18
       test      al,1
       jne       near ptr M08_L30
M08_L12:
       mov       r14,[rsi+20]
       mov       r8,[r14+8]
       mov       r15d,[r8+8]
       dec       r15d
       js        near ptr M08_L31
       mov       r8,0CCAD800068
       mov       r13,[r8]
M08_L13:
       mov       r8,[r14+8]
       cmp       r15d,[r8+8]
       jae       near ptr M08_L35
       mov       edx,r15d
       shl       rdx,5
       mov       r8,[r8+rdx+10]
       cmp       r8,rdi
       je        short M08_L14
       test      r8,r8
       je        near ptr M08_L21
       test      rdi,rdi
       je        near ptr M08_L21
       mov       edx,[r8+8]
       cmp       edx,[rdi+8]
       jne       near ptr M08_L21
       lea       rcx,[r8+0C]
       mov       r8d,[r8+8]
       add       r8d,r8d
       lea       rdx,[rdi+0C]
       call      qword ptr [7FF97783C330]; System.SpanHelpers.SequenceEqual(Byte ByRef, Byte ByRef, UIntPtr)
       test      eax,eax
       je        near ptr M08_L21
M08_L14:
       mov       eax,r15d
M08_L15:
       test      eax,eax
       jl        near ptr M08_L32
       mov       rcx,[r14+8]
       cmp       eax,[rcx+8]
       jae       near ptr M08_L35
       mov       eax,eax
       shl       rax,5
       mov       rax,[rcx+rax+18]
       mov       [rsp+28],rax
       test      byte ptr [rsp+2C],1
       je        near ptr M08_L33
M08_L16:
       mov       rcx,[rsi+20]
       lea       r9,[rsp+28]
       mov       rdx,rdi
       mov       r8d,ebp
       cmp       [rcx],ecx
       call      qword ptr [7FF977A0E928]; AuroraScript.Runtime.Property.HiddenClass.AddProperty(System.String, AuroraScript.Runtime.Property.PropertyFlags, AuroraScript.Runtime.Property.PropertyMeta ByRef)
       lea       rcx,[rsi+20]
       mov       rdx,rax
       call      CORINFO_HELP_ASSIGN_REF
       movzx     ecx,word ptr [rsp+28]
       mov       rax,[rsi+10]
       mov       eax,[rax+8]
       cmp       ecx,eax
       jge       near ptr M08_L34
M08_L17:
       mov       rax,[rsi+10]
       movzx     ecx,word ptr [rsp+28]
       cmp       ecx,[rax+8]
       jae       near ptr M08_L35
       shl       rcx,4
       lea       rdi,[rax+rcx+10]
       mov       rsi,rbx
       call      CORINFO_HELP_ASSIGN_BYREF
       movsq
M08_L18:
       add       rsp,70
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       pop       r13
       pop       r14
       pop       r15
       ret
M08_L19:
       dec       r15d
       jns       near ptr M08_L00
       jmp       short M08_L22
M08_L20:
       dec       r15d
       jns       near ptr M08_L08
       jmp       near ptr M08_L29
M08_L21:
       dec       r15d
       jns       near ptr M08_L13
       jmp       near ptr M08_L31
M08_L22:
       mov       r8d,0FFFFFFFF
       jmp       near ptr M08_L02
M08_L23:
       mov       rcx,[rsi+8]
       test      rcx,rcx
       je        short M08_L26
       jmp       short M08_L25
M08_L24:
       mov       rcx,[rsi+18]
       cmp       [rcx+8],r8d
       jle       near ptr M08_L03
       mov       rcx,[rsi+18]
       cmp       r8d,[rcx+8]
       jae       near ptr M08_L35
       mov       edx,r8d
       mov       rcx,[rcx+rdx*8+10]
       jmp       near ptr M08_L04
M08_L25:
       lea       r8,[rsp+58]
       mov       rdx,rdi
       call      qword ptr [7FF977C35F50]; AuroraScript.Runtime.Types.ScriptObject.TryResolveProperty(System.String, AuroraScript.Runtime.Property.PropertyDescriptor ByRef)
       test      eax,eax
       je        near ptr M08_L07
       jmp       near ptr M08_L05
M08_L26:
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rsp+58],xmm0
       vmovdqu   xmmword ptr [rsp+60],xmm0
       jmp       near ptr M08_L07
M08_L27:
       mov       rdx,[rdx+10]
       test      rdx,rdx
       je        near ptr M08_L06
       lea       rcx,[rsp+58]
       call      qword ptr [7FF977C35F68]; AuroraScript.Runtime.Property.PropertyDescriptor.get_Setter()
       cmp       [rax],al
       lea       rdx,[rsp+48]
       mov       rcx,rax
       mov       r8,rbp
       mov       r9,rbx
       call      qword ptr [7FF977C3CAE0]
       jmp       near ptr M08_L18
M08_L28:
       mov       rax,[rax+40]
       test      rax,rax
       je        near ptr M08_L07
       mov       rdx,rbp
       mov       r8,rsi
       mov       r9,rbx
       mov       rcx,[rax+8]
       call      qword ptr [rax+18]
       jmp       near ptr M08_L18
M08_L29:
       mov       eax,0FFFFFFFF
       jmp       near ptr M08_L10
M08_L30:
       call      qword ptr [7FF977A0E8E0]
       jmp       near ptr M08_L12
M08_L31:
       mov       eax,0FFFFFFFF
       jmp       near ptr M08_L15
M08_L32:
       xor       ecx,ecx
       mov       [rsp+28],rcx
       jmp       near ptr M08_L16
M08_L33:
       call      qword ptr [7FF977A0E910]
       jmp       near ptr M08_L16
M08_L34:
       mov       rcx,rsi
       call      qword ptr [7FF977A0E940]; AuroraScript.Runtime.Types.ScriptObject.Resize()
       jmp       near ptr M08_L17
M08_L35:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 1108
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
       jne       short M09_L00
       mov       rcx,7FF977CB858C
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rax,rsi
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
M09_L00:
       mov       rcx,7FF977CB8588
       call      CORINFO_HELP_COUNTPROFILE32
       mov       rcx,rbx
       add       rsp,28
       pop       rbx
       pop       rsi
       jmp       qword ptr [7FF977A0FD50]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBaseSlow(System.Runtime.CompilerServices.MethodTable*)
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
M10_L00:
       lea       r10d,[r8+1]
       movsxd    r10,r10d
       lea       r10,[r10+r10*2]
       lea       r10,[rax+r10*8]
       mov       r9d,[r10]
       mov       r11,[r10+8]
       and       r9d,0FFFFFFFE
       cmp       r11,rdx
       jne       short M10_L02
       mov       r11,rbx
       xor       r11,[r10+10]
       cmp       r11,1
       ja        short M10_L02
       cmp       r9d,[r10]
       jne       short M10_L04
M10_L01:
       cmp       r11d,1
       jne       short M10_L03
       mov       rax,rsi
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
M10_L02:
       test      r9d,r9d
       je        short M10_L04
       inc       ecx
       add       r8d,ecx
       and       r8d,[rax+4]
       cmp       ecx,8
       jl        short M10_L00
       jmp       short M10_L04
M10_L03:
       test      r11d,r11d
       jne       short M10_L05
       xor       eax,eax
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
M10_L04:
       mov       r11d,2
       jmp       short M10_L01
M10_L05:
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
       je        short M12_L00
       mov       rcx,rsi
       xor       r8d,r8d
       call      qword ptr [7FF96E851BC8]
       mov       rax,[rbx]
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
M12_L00:
       call      qword ptr [7FF96E84F410]
       int       3
; Total bytes of code 61
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
       jne       short M13_L01
       cmp       [rax],ecx
       jle       short M13_L03
       mov       rax,[rax+8]
       cmp       [rax],al
       add       edx,0FFFFFFFE
       movsxd    rcx,edx
       mov       rax,[rax+rcx*8+10]
       test      rax,rax
       je        short M13_L03
M13_L00:
       add       rsp,20
       pop       rbx
       ret
M13_L01:
       mov       ecx,ebx
       sar       ecx,18
       cmp       ecx,2
       jne       short M13_L02
       movsxd    rcx,edx
       add       rax,rcx
       jmp       short M13_L00
M13_L02:
       cmp       [rax+4],edx
       jle       short M13_L03
       mov       rcx,[rax+10]
       movsxd    rax,edx
       mov       rcx,[rcx+rax*8]
       test      rcx,rcx
       je        short M13_L03
       mov       rax,[rcx]
       test      rax,rax
       je        short M13_L03
       jmp       short M13_L00
M13_L03:
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
       je        short M14_L00
       cmp       [rdx],rcx
       jne       short M14_L01
M14_L00:
       mov       rax,rdx
       ret
M14_L01:
       jmp       qword ptr [7FF977A04D20]; System.Runtime.CompilerServices.CastHelpers.ChkCastClassSpecial(Void*, System.Object)
; Total bytes of code 20
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
M15_L00:
       vmovdqa   xmmword ptr [rbp+rax+120],xmm4
       vmovdqa   xmmword ptr [rbp+rax+130],xmm4
       vmovdqa   xmmword ptr [rbp+rax+140],xmm4
       add       rax,30
       jne       short M15_L00
       mov       [rbp+120],rax
       mov       rax,0E0F90834C4CE
       mov       [rbp+8],rax
       mov       rdi,rcx
       vmovaps   xmm6,xmm1
       mov       rbx,r8
       mov       rsi,r9
       vmovq     rax,xmm6
       mov       rcx,7FF0000000000000
       andn      rax,rax,rcx
       je        near ptr M15_L19
       mov       r14,[rbx]
       mov       r15d,[rbx+8]
       xor       ecx,ecx
       test      r15d,r15d
       jg        near ptr M15_L26
M15_L01:
       mov       dword ptr [rbp+128],0FFFFFFFF
       test      r15d,r15d
       jne       near ptr M15_L36
M15_L02:
       mov       ecx,47
M15_L03:
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
       jbe       near ptr M15_L49
       mov       r9,[rbp+118]
       mov       byte ptr [r9],0
       vmovq     r9,xmm6
       shr       r9,3F
       mov       [rbp+110],r9b
       lea       r9,[rbp+100]
       lea       rdx,[rbp+128]
       mov       r8,rsi
       call      qword ptr [7FF977BB7138]; System.Number.GetFloatingPointMaxDigitsAndPrecision(Char, Int32 ByRef, System.Globalization.NumberFormatInfo, Boolean ByRef)
       mov       r15d,eax
       vxorps    xmm0,xmm0,xmm0
       vucomisd  xmm6,xmm0
       jp        short M15_L04
       je        near ptr M15_L15
M15_L04:
       cmp       byte ptr [rbp+100],0
       je        near ptr M15_L47
       mov       r13d,[rbp+128]
       vmovq     rax,xmm6
       test      rax,rax
       jl        near ptr M15_L37
       vmovaps   xmm0,xmm6
M15_L05:
       cmp       r13d,0FFFFFFFF
       jne       near ptr M15_L46
       vmovq     rax,xmm0
       mov       rdx,0FFFFFFFFFFFFF
       and       rdx,rax
       shr       rax,34
       and       eax,7FF
       je        near ptr M15_L38
       mov       rcx,10000000000000
       or        rdx,rcx
       add       eax,0FFFFFBCD
M15_L06:
       lea       rcx,[rdx*2+1]
       lea       r8d,[rax-1]
       xor       r10d,r10d
       lzcnt     r10,rcx
       shlx      rcx,rcx,r10
       sub       r8d,r10d
       mov       r10,10000000000000
       cmp       rdx,r10
       je        near ptr M15_L39
       lea       r10,[rdx*2-1]
       lea       r9d,[rax-1]
M15_L07:
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
       vmulsd    xmm0,xmm0,qword ptr [7FF9778B5CE0]
       vroundsd  xmm0,xmm0,xmm0,0A
       vcmpordsd xmm1,xmm0,xmm0
       vandpd    xmm1,xmm1,xmm0
       mov       r9d,7FFFFFFF
       vcvttsd2si r11d,xmm1
       vucomisd  xmm0,qword ptr [7FF9778B5CE8]
       cmovb     r9d,r11d
       add       r9d,15B
       mov       r11d,r9d
       sar       r11d,1F
       and       r11d,7
       add       r9d,r11d
       sar       r9d,3
       inc       r9d
       cmp       r9d,57
       jae       near ptr M15_L49
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
       jae       near ptr M15_L49
       mov       r10d,eax
       mov       r9,7FF96DBD36D0
       mov       r10d,[r9+r10*4]
       cmp       r11d,r10d
       jae       short M15_L08
       dec       eax
       cmp       eax,0A
       jae       near ptr M15_L49
       mov       r10d,eax
       mov       r9,7FF96DBD36D0
       mov       r10d,[r9+r10*4]
M15_L08:
       lea       r9d,[rax+1]
       xor       eax,eax
       mov       [rbp+0D8],eax
       test      r9d,r9d
       jle       near ptr M15_L45
M15_L09:
       mov       eax,r11d
       xor       edx,edx
       div       r10d
       mov       edx,eax
       imul      edx,r10d
       sub       r11d,edx
       mov       edx,[rbp+0D8]
       cmp       edx,r12d
       jae       near ptr M15_L49
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
       jae       near ptr M15_L40
       mov       eax,[rbp+0D8]
       mov       r11,[rbp+70]
       sub       r11,[rbp+0B8]
       mov       [rbp+10],r13
       mov       r10d,r10d
       shlx      r8,r10,r8
       mov       r10,r11
       sub       r10,1
       inc       r11
M15_L10:
       cmp       rdx,r10
       jb        near ptr M15_L42
M15_L11:
       cmp       rdx,r11
       jae       short M15_L12
       mov       rax,rcx
       sub       rax,rdx
       cmp       rax,r8
       jae       near ptr M15_L43
M15_L12:
       cmp       rdx,2
       jb        near ptr M15_L44
       sub       rcx,4
       cmp       rcx,rdx
       setae     al
       movzx     eax,al
M15_L13:
       mov       edx,r9d
       sub       edx,[rbp+0CC]
       mov       [rbp+0D0],edx
       movzx     r13d,al
M15_L14:
       test      r13d,r13d
       je        near ptr M15_L47
       mov       r8d,[rbp+0D8]
       add       r8d,[rbp+0D0]
       mov       [rbp+10C],r8d
       mov       r8d,[rbp+120]
       cmp       [rbp+0D8],r8d
       jae       near ptr M15_L49
       mov       r8,[rbp+118]
       mov       r9d,[rbp+0D8]
       mov       byte ptr [r8+r9],0
       mov       r8d,[rbp+0D8]
       mov       [rbp+108],r8d
M15_L15:
       test      r14d,r14d
       je        near ptr M15_L48
       cmp       dword ptr [rbp+128],0FFFFFFFF
       jne       short M15_L16
       mov       r15d,[rbp+108]
       mov       edx,11
       cmp       r15d,11
       cmovl     r15d,edx
M15_L16:
       mov       [rsp+20],rsi
       lea       rdx,[rbp+108]
       mov       rcx,rdi
       mov       r8d,r14d
       mov       r9d,r15d
       call      qword ptr [7FF977C35F20]; System.Number.NumberToString[[System.Char, System.Private.CoreLib]](System.Collections.Generic.ValueListBuilder`1<Char> ByRef, NumberBuffer ByRef, Char, Int32, System.Globalization.NumberFormatInfo)
M15_L17:
       xor       eax,eax
       mov       r8,0E0F90834C4CE
       cmp       [rbp+8],r8
       je        short M15_L18
       call      CORINFO_HELP_FAIL_FAST
M15_L18:
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
M15_L19:
       vucomisd  xmm6,xmm6
       jp        short M15_L20
       je        short M15_L22
M15_L20:
       mov       rax,[rsi+58]
       mov       r8,0E0F90834C4CE
       cmp       [rbp+8],r8
       je        short M15_L21
       call      CORINFO_HELP_FAIL_FAST
M15_L21:
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
M15_L22:
       vmovq     rax,xmm6
       test      rax,rax
       jl        short M15_L24
       mov       rax,[rsi+60]
       mov       r8,0E0F90834C4CE
       cmp       [rbp+8],r8
       je        short M15_L23
       call      CORINFO_HELP_FAIL_FAST
M15_L23:
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
M15_L24:
       mov       rax,[rsi+68]
       mov       r8,0E0F90834C4CE
       cmp       [rbp+8],r8
       je        short M15_L25
       call      CORINFO_HELP_FAIL_FAST
M15_L25:
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
M15_L26:
       movzx     ecx,word ptr [r14]
       mov       eax,ecx
       or        eax,20
       add       eax,0FFFFFF9F
       cmp       eax,19
       ja        near ptr M15_L01
       cmp       r15d,1
       jne       short M15_L28
       mov       dword ptr [rbp+128],0FFFFFFFF
M15_L27:
       jmp       near ptr M15_L03
M15_L28:
       cmp       r15d,2
       jne       short M15_L29
       movzx     eax,word ptr [r14+2]
       add       eax,0FFFFFFD0
       cmp       eax,0A
       jae       short M15_L30
       mov       [rbp+128],eax
       jmp       short M15_L27
M15_L29:
       cmp       r15d,3
       jne       short M15_L30
       movzx     eax,word ptr [r14+2]
       add       eax,0FFFFFFD0
       movzx     edx,word ptr [r14+4]
       add       edx,0FFFFFFD0
       cmp       eax,0A
       jae       short M15_L30
       cmp       edx,0A
       jae       short M15_L30
       lea       eax,[rax+rax*4]
       lea       eax,[rdx+rax*2]
       mov       [rbp+128],eax
       jmp       short M15_L27
M15_L30:
       xor       r13d,r13d
       mov       eax,1
       jmp       short M15_L32
M15_L31:
       cmp       r13d,5F5E100
       jge       short M15_L33
       lea       edx,[rax+1]
       mov       eax,eax
       movzx     eax,word ptr [r14+rax*2]
       lea       r8d,[r13+r13*4]
       lea       r13d,[rax+r8*2-30]
       mov       eax,edx
M15_L32:
       cmp       eax,r15d
       jae       short M15_L35
       mov       edx,eax
       movzx     edx,word ptr [r14+rdx*2]
       add       edx,0FFFFFFD0
       cmp       edx,9
       jbe       short M15_L31
       jmp       short M15_L34
M15_L33:
       call      qword ptr [7FF977C3C240]
       int       3
M15_L34:
       mov       r9d,eax
       cmp       word ptr [r14+r9*2],0
       jne       near ptr M15_L01
M15_L35:
       mov       [rbp+128],r13d
       jmp       near ptr M15_L27
M15_L36:
       test      ecx,ecx
       je        near ptr M15_L02
       xor       ecx,ecx
       jmp       near ptr M15_L03
M15_L37:
       vxorps    xmm0,xmm6,[7FF9778B5CF0]
       jmp       near ptr M15_L05
M15_L38:
       mov       eax,0FFFFFBCE
       jmp       near ptr M15_L06
M15_L39:
       lea       r9d,[rax-2]
       mov       r10,3FFFFFFFFFFFFF
       jmp       near ptr M15_L07
M15_L40:
       mov       edx,0CCCCCCCD
       mov       r10d,r10d
       imul      r10,rdx
       shr       r10,23
       test      r9d,r9d
       mov       [rbp+68],rax
       jg        near ptr M15_L09
       jmp       short M15_L45
M15_L41:
       lea       r13d,[rax-1]
       cmp       r13d,r12d
       jae       near ptr M15_L49
       lea       r13d,[rax-1]
       add       r13,[rbp+10]
       dec       byte ptr [r13]
       add       rdx,r8
       jmp       near ptr M15_L10
M15_L42:
       mov       r13,rcx
       sub       r13,rdx
       cmp       r13,r8
       jb        near ptr M15_L11
       lea       r13,[rdx+r8]
       cmp       r13,r10
       jb        short M15_L41
       mov       r13,r10
       sub       r13,rdx
       mov       [rbp],r13
       lea       r13,[rdx+r8]
       sub       r13,r10
       cmp       [rbp],r13
       jae       short M15_L41
       jmp       near ptr M15_L11
M15_L43:
       lea       rax,[rdx+r8]
       cmp       rax,r11
       jb        short M15_L44
       mov       rax,r11
       sub       rax,rdx
       add       r8,rdx
       sub       r8,r11
       cmp       rax,r8
       jbe       near ptr M15_L12
M15_L44:
       xor       eax,eax
       jmp       near ptr M15_L13
M15_L45:
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
       jae       near ptr M15_L49
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
       jae       near ptr M15_L50
       mov       r11d,[rbp+0D8]
       mov       [rbp+64],r11d
       lea       rcx,[rbp+70]
       lea       rdx,[rbp+50]
       lea       r8,[rbp+0B8]
       call      qword ptr [7FF977C3CA08]
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
       call      qword ptr [7FF977C35F08]; System.Number+Grisu3.TryRoundWeedShortest(System.Span`1<Byte>, Int32, UInt64, UInt64, UInt64, UInt64, UInt64)
       mov       r9d,[rbp+0B4]
       jmp       near ptr M15_L13
M15_L46:
       lea       rdx,[rbp+40]
       call      qword ptr [7FF977BB7168]; System.Number.ExtractFractionAndBiasedExponent[[System.Double, System.Private.CoreLib]](Double, Int32 ByRef)
       mov       ecx,[rbp+40]
       mov       [rbp+0F0],rax
       mov       [rbp+0F8],ecx
       lea       rcx,[rbp+0F0]
       lea       rdx,[rbp+0E0]
       call      qword ptr [7FF977BB7180]; System.Number+DiyFp.Normalize()
       vmovdqu   xmm0,xmmword ptr [rbp+118]
       vmovdqu   xmmword ptr [rbp+20],xmm0
       lea       r8,[rbp+0D0]
       mov       [rsp+20],r8
       lea       r8,[rbp+20]
       lea       rcx,[rbp+0E0]
       lea       r9,[rbp+0D8]
       mov       edx,r13d
       call      qword ptr [7FF977BB7198]; System.Number+Grisu3.TryRunCounted(DiyFp ByRef, Int32, System.Span`1<Byte>, Int32 ByRef, Int32 ByRef)
       mov       r13d,eax
       jmp       near ptr M15_L14
M15_L47:
       movzx     r8d,byte ptr [rbp+100]
       lea       r9,[rbp+108]
       vmovaps   xmm0,xmm6
       mov       edx,[rbp+128]
       call      qword ptr [7FF977BB7240]; System.Number.Dragon4[[System.Double, System.Private.CoreLib]](Double, Int32, Boolean, NumberBuffer ByRef)
       jmp       near ptr M15_L15
M15_L48:
       lea       rdx,[rbp+108]
       mov       rcx,rdi
       mov       r8,rbx
       mov       r9,rsi
       call      qword ptr [7FF977BB7318]; System.Number.NumberToStringFormat[[System.Char, System.Private.CoreLib]](System.Collections.Generic.ValueListBuilder`1<Char> ByRef, NumberBuffer ByRef, System.ReadOnlySpan`1<Char>, System.Globalization.NumberFormatInfo)
       jmp       near ptr M15_L17
M15_L49:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
M15_L50:
       mov       r9d,[rbp+0B4]
       jmp       near ptr M15_L45
; Total bytes of code 2653
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
       je        short M16_L00
       mov       edx,esi
       mov       rcx,offset MT_System.String
       call      00007FF9D74C4E80
       mov       rdi,rax
       cmp       [rdi],dil
       lea       rcx,[rdi+0C]
       mov       r8d,esi
       add       r8,r8
       mov       rdx,rbx
       call      qword ptr [7FF977835818]; System.SpanHelpers.Memmove(Byte ByRef, Byte ByRef, UIntPtr)
       mov       rax,rdi
       add       rsp,20
       pop       rbx
       pop       rsi
       pop       rdi
       ret
M16_L00:
       mov       rax,0ECE02C0008
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
       jae       short M17_L05
       cmp       r8,4
       jae       short M17_L04
       xor       eax,eax
       mov       r10,r8
       and       r10,2
       je        short M17_L00
       movzx     eax,word ptr [rcx]
       movzx     r9d,word ptr [rdx]
       sub       eax,r9d
M17_L00:
       test      r8b,1
       jne       short M17_L03
M17_L01:
       test      eax,eax
       sete      al
       movzx     eax,al
M17_L02:
       vzeroupper
       ret
M17_L03:
       movzx     r8d,byte ptr [rcx+r10]
       movzx     ecx,byte ptr [rdx+r10]
       sub       r8d,ecx
       or        eax,r8d
       jmp       short M17_L01
M17_L04:
       add       r8,0FFFFFFFFFFFFFFFC
       mov       eax,[rcx]
       sub       eax,[rdx]
       mov       ecx,[rcx+r8]
       sub       ecx,[rdx+r8]
       or        eax,ecx
       sete      al
       movzx     eax,al
       jmp       short M17_L02
M17_L05:
       cmp       rcx,rdx
       je        near ptr M17_L11
       cmp       r8,20
       jb        short M17_L08
       xor       eax,eax
       add       r8,0FFFFFFFFFFFFFFE0
       je        short M17_L07
M17_L06:
       vmovups   ymm0,[rcx+rax]
       vpcmpeqb  ymm0,ymm0,[rdx+rax]
       vpmovmskb r10d,ymm0
       cmp       r10d,0FFFFFFFF
       jne       near ptr M17_L13
       add       rax,20
       cmp       r8,rax
       ja        short M17_L06
M17_L07:
       vmovups   ymm0,[rcx+r8]
       vpcmpeqb  ymm0,ymm0,[rdx+r8]
       vpmovmskb ecx,ymm0
       cmp       ecx,0FFFFFFFF
       jne       short M17_L13
       jmp       short M17_L11
M17_L08:
       cmp       r8,10
       jb        short M17_L12
       xor       eax,eax
       add       r8,0FFFFFFFFFFFFFFF0
       je        short M17_L10
M17_L09:
       vmovups   xmm0,[rcx+rax]
       vpcmpeqb  xmm0,xmm0,[rdx+rax]
       vpmovmskb r10d,xmm0
       cmp       r10d,0FFFF
       jne       short M17_L13
       add       rax,10
       cmp       r8,rax
       ja        short M17_L09
M17_L10:
       vmovups   xmm0,[rcx+r8]
       vpcmpeqb  xmm0,xmm0,[rdx+r8]
       vpmovmskb eax,xmm0
       cmp       eax,0FFFF
       jne       short M17_L13
M17_L11:
       mov       eax,1
       vzeroupper
       ret
M17_L12:
       lea       rax,[r8-8]
       mov       r8,[rcx]
       sub       r8,[rdx]
       mov       rcx,[rcx+rax]
       sub       rcx,[rdx+rax]
       or        rcx,r8
       sete      al
       movzx     eax,al
       jmp       near ptr M17_L02
M17_L13:
       xor       eax,eax
       vzeroupper
       ret
; Total bytes of code 290
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
       js        near ptr M18_L04
       mov       r8,0CCAD800068
       mov       rdi,[r8]
M18_L00:
       mov       r8,[rbx+8]
       cmp       esi,[r8+8]
       jae       near ptr M18_L11
       mov       edx,esi
       shl       rdx,5
       mov       r8,[r8+rdx+10]
       cmp       r8,rbp
       je        short M18_L01
       test      r8,r8
       je        short M18_L03
       test      rbp,rbp
       je        short M18_L03
       mov       edx,[r8+8]
       cmp       edx,[rbp+8]
       jne       short M18_L03
       lea       rcx,[r8+0C]
       mov       r8d,[r8+8]
       add       r8d,r8d
       lea       rdx,[rbp+0C]
       call      qword ptr [7FF97783C330]; System.SpanHelpers.SequenceEqual(Byte ByRef, Byte ByRef, UIntPtr)
       test      eax,eax
       je        short M18_L03
M18_L01:
       mov       r13d,esi
M18_L02:
       test      r13d,r13d
       jl        short M18_L05
       mov       rax,[rbx+8]
       cmp       r13d,[rax+8]
       jae       near ptr M18_L11
       mov       ecx,r13d
       shl       rcx,5
       lea       rax,[rax+rcx+18]
       movzx     ecx,word ptr [rax+4]
       mov       rax,[rax]
       movzx     edx,r15w
       cmp       ecx,edx
       jne       short M18_L05
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
M18_L03:
       dec       esi
       jns       near ptr M18_L00
M18_L04:
       mov       r13d,0FFFFFFFF
       jmp       short M18_L02
M18_L05:
       movzx     r12d,r15w
       cmp       qword ptr [rbx+10],0
       je        short M18_L06
       mov       rcx,[rbx+10]
       mov       [rsp+38],rbp
       mov       [rsp+40],r12w
       lea       rdx,[rsp+38]
       lea       r8,[rsp+90]
       cmp       [rcx],ecx
       call      qword ptr [7FF977AD0BB0]; Precode of System.Collections.Generic.Dictionary`2[[AuroraScript.Runtime.Property.TransitionKey, AuroraScript],[System.__Canon, System.Private.CoreLib]].TryGetValue(AuroraScript.Runtime.Property.TransitionKey, System.__Canon ByRef)
       test      eax,eax
       je        short M18_L06
       mov       rcx,[rsp+90]
       mov       rdx,rbp
       mov       r8,r14
       cmp       [rcx],ecx
       call      qword ptr [7FF977A0E838]; AuroraScript.Runtime.Property.HiddenClass.TryGet(System.String, AuroraScript.Runtime.Property.PropertyMeta ByRef)
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
M18_L06:
       test      r13d,r13d
       jl        near ptr M18_L07
       mov       rcx,[rbx+8]
       cmp       r13d,[rcx+8]
       jae       near ptr M18_L11
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
       call      qword ptr [7FF977BB54E8]; System.Object.MemberwiseClone()
       mov       r15,rax
       mov       r8,[r14]
       lea       rcx,[rsp+48]
       mov       rdx,rbp
       call      qword ptr [7FF977A0EA18]; AuroraScript.Runtime.Property.HiddenProperty..ctor(System.String, AuroraScript.Runtime.Property.PropertyMeta)
       cmp       r13d,[r15+8]
       jae       near ptr M18_L11
       mov       eax,r13d
       shl       rax,5
       lea       rdi,[r15+rax+10]
       lea       rsi,[rsp+48]
       call      CORINFO_HELP_ASSIGN_BYREF
       movsq
       call      CORINFO_HELP_ASSIGN_BYREF
       movsq
       jmp       near ptr M18_L09
M18_L07:
       movzx     eax,word ptr [rbx+38]
       movzx     edx,word ptr [rbx+38]
       add       edx,1
       jo        near ptr M18_L12
       cmp       edx,0FFFF
       ja        near ptr M18_L12
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
       je        short M18_L08
       mov       r8,[rbx+8]
       mov       r8d,[r8+8]
       mov       rcx,[rbx+8]
       mov       rdx,r15
       call      qword ptr [7FF97783F588]; System.Array.Copy(System.Array, System.Array, Int32)
M18_L08:
       mov       edi,[r15+8]
       dec       edi
       mov       r8,[r14]
       lea       rcx,[rsp+68]
       mov       rdx,rbp
       call      qword ptr [7FF977A0EA18]; AuroraScript.Runtime.Property.HiddenProperty..ctor(System.String, AuroraScript.Runtime.Property.PropertyMeta)
       cmp       edi,[r15+8]
       jae       near ptr M18_L11
       mov       eax,edi
       shl       rax,5
       lea       rdi,[r15+rax+10]
       lea       rsi,[rsp+68]
       call      CORINFO_HELP_ASSIGN_BYREF
       movsq
       call      CORINFO_HELP_ASSIGN_BYREF
       movsq
M18_L09:
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
       call      qword ptr [7FF977A0EA30]; AuroraScript.Runtime.Property.HiddenClass..ctor(AuroraScript.Runtime.Property.HiddenProperty[], UInt16, AuroraScript.Runtime.Property.HiddenClass, System.String, Boolean)
       cmp       qword ptr [rbx+10],0
       jne       short M18_L10
       mov       rcx,offset MT_System.Collections.Generic.Dictionary<AuroraScript.Runtime.Property.TransitionKey, AuroraScript.Runtime.Property.HiddenClass>
       call      CORINFO_HELP_NEWSFAST
       mov       rdi,rax
       mov       rcx,rdi
       mov       edx,4
       xor       r8d,r8d
       call      qword ptr [7FF977A0EAD8]; System.Collections.Generic.Dictionary`2[[AuroraScript.Runtime.Property.TransitionKey, AuroraScript],[System.__Canon, System.Private.CoreLib]]..ctor(Int32, System.Collections.Generic.IEqualityComparer`1<AuroraScript.Runtime.Property.TransitionKey>)
       lea       rcx,[rbx+10]
       mov       rdx,rdi
       call      CORINFO_HELP_ASSIGN_REF
M18_L10:
       mov       rcx,[rbx+10]
       cmp       [rcx],cl
       mov       [rsp+38],rbp
       mov       [rsp+40],r12w
       lea       rdx,[rsp+38]
       mov       r8,rsi
       mov       r9d,1
       call      qword ptr [7FF977A0EBE0]; System.Collections.Generic.Dictionary`2[[AuroraScript.Runtime.Property.TransitionKey, AuroraScript],[System.__Canon, System.Private.CoreLib]].TryInsert(AuroraScript.Runtime.Property.TransitionKey, System.__Canon, System.Collections.Generic.InsertionBehavior)
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
M18_L11:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
M18_L12:
       call      CORINFO_HELP_OVERFLOW
       int       3
; Total bytes of code 868
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
M19_L00:
       mov       rbp,[rsi+20]
       mov       r8,[rbp+8]
       mov       r14d,[r8+8]
       dec       r14d
       js        near ptr M19_L07
       mov       r8,0CCAD800068
       mov       r15,[r8]
M19_L01:
       mov       r8,[rbp+8]
       cmp       r14d,[r8+8]
       jae       near ptr M19_L11
       mov       edx,r14d
       shl       rdx,5
       mov       r8,[r8+rdx+10]
       cmp       r8,rdi
       je        short M19_L02
       test      r8,r8
       je        near ptr M19_L06
       test      rdi,rdi
       je        near ptr M19_L06
       mov       edx,[r8+8]
       cmp       edx,[rdi+8]
       jne       near ptr M19_L06
       lea       rcx,[r8+0C]
       mov       r8d,[r8+8]
       add       r8d,r8d
       lea       rdx,[rdi+0C]
       call      qword ptr [7FF97783C330]; System.SpanHelpers.SequenceEqual(Byte ByRef, Byte ByRef, UIntPtr)
       test      eax,eax
       je        short M19_L06
M19_L02:
       mov       eax,r14d
M19_L03:
       test      eax,eax
       jl        near ptr M19_L08
       mov       rcx,[rbp+8]
       cmp       eax,[rcx+8]
       jae       near ptr M19_L11
       mov       eax,eax
       shl       rax,5
       movzx     eax,word ptr [rcx+rax+18]
       cmp       qword ptr [rsi+18],0
       jne       near ptr M19_L09
M19_L04:
       xor       ecx,ecx
M19_L05:
       mov       rdx,[rsi+10]
       cmp       eax,[rdx+8]
       jae       near ptr M19_L11
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
M19_L06:
       dec       r14d
       jns       near ptr M19_L01
M19_L07:
       mov       eax,0FFFFFFFF
       jmp       near ptr M19_L03
M19_L08:
       mov       rsi,[rsi+8]
       test      rsi,rsi
       je        short M19_L10
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rsp+20],xmm0
       vmovdqu   xmmword ptr [rsp+28],xmm0
       jmp       near ptr M19_L00
M19_L09:
       mov       rcx,[rsi+18]
       cmp       [rcx+8],eax
       jle       near ptr M19_L04
       mov       rcx,[rsi+18]
       cmp       eax,[rcx+8]
       jae       short M19_L11
       mov       edx,eax
       mov       rcx,[rcx+rdx*8+10]
       jmp       near ptr M19_L05
M19_L10:
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
M19_L11:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 396
```
```assembly
; AuroraScript.Runtime.Property.PropertyDescriptor.get_Setter()
       mov       rcx,[rcx]
       test      rcx,rcx
       jne       short M20_L00
       xor       eax,eax
       ret
M20_L00:
       mov       rax,[rcx+10]
       ret
; Total bytes of code 16
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
       je        short M21_L00
       mov       rax,[rbp+10]
       mov       rax,[rax+10]
       mov       eax,[rax+8]
       add       eax,eax
       mov       [rbp-8],eax
       jmp       short M21_L01
M21_L00:
       mov       dword ptr [rbp-8],2
M21_L01:
       mov       eax,[rbp-8]
       mov       [rbp-4],eax
       mov       rax,[rbp+10]
       cmp       [rax],al
       mov       rax,[rbp+10]
       lea       rcx,[rax+10]
       mov       edx,[rbp-4]
       call      qword ptr [7FF977A0ED90]; System.Array.Resize[[AuroraScript.Runtime.ScriptDatum, AuroraScript]](AuroraScript.Runtime.ScriptDatum[] ByRef, Int32)
       mov       rax,[rbp+10]
       cmp       qword ptr [rax+18],0
       je        short M21_L02
       mov       rax,[rbp+10]
       cmp       [rax],al
       mov       rax,[rbp+10]
       lea       rdx,[rax+18]
       mov       r8d,[rbp-4]
       mov       rcx,7FF977AD3990
       call      qword ptr [7FF977A0C600]; System.Array.Resize[[System.__Canon, System.Private.CoreLib]](System.__Canon[] ByRef, Int32)
M21_L02:
       nop
       add       rsp,30
       pop       rbp
       ret
; Total bytes of code 142
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
       call      qword ptr [7FF977C35DB8]; System.SpanHelpers.ClearWithReferences(IntPtr ByRef, UIntPtr)
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
       call      qword ptr [7FF977C35DA0]; AuroraScript.Runtime.Types.ScriptArray.EnsureCapacity(Int32)
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
       call      qword ptr [7FF977C3C240]
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
       call      qword ptr [7FF977C3C240]
       jmp       near ptr M00_L08
M00_L15:
       mov       ecx,2
       call      qword ptr [7FF97783FD38]
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
       call      qword ptr [7FF977C3C120]
       int       3
M00_L18:
       call      qword ptr [7FF9778357E8]; System.SpanHelpers.ClearWithoutReferences(Byte ByRef, UIntPtr)
       jmp       near ptr M00_L01
M00_L19:
       test      edi,edi
       jge       near ptr M00_L01
       mov       rcx,rsi
       xor       edx,edx
       call      qword ptr [7FF977C35DA0]; AuroraScript.Runtime.Types.ScriptArray.EnsureCapacity(Int32)
       mov       r8d,[rsi+34]
       neg       r8d
       mov       edx,[rsi+34]
       mov       rcx,[rsi+38]
       call      qword ptr [7FF977C35D88]; System.Array.Clear(System.Array, Int32, Int32)
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
       call      qword ptr [7FF977C3C168]
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
       call      qword ptr [7FF977A07D50]
       int       3
M00_L31:
       call      CORINFO_HELP_POLL_GC
       jmp       near ptr M00_L07
M00_L32:
       mov       rcx,rax
       mov       rdx,r14
       call      qword ptr [7FF977C3C0A8]
       jmp       near ptr M00_L07
M00_L33:
       call      qword ptr [7FF977C36040]
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
       call      qword ptr [7FF97783F588]; System.Array.Copy(System.Array, System.Array, Int32)
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
       jmp       qword ptr [7FF977C35DB8]; System.SpanHelpers.ClearWithReferences(IntPtr ByRef, UIntPtr)
M05_L01:
       mov       ecx,2
       call      qword ptr [7FF97783FD38]
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
       call      qword ptr [7FF977C3C120]
       int       3
M05_L04:
       call      qword ptr [7FF9778357E8]; System.SpanHelpers.ClearWithoutReferences(Byte ByRef, UIntPtr)
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
       mov       rdx,0B86B800450
       cmp       r15,[rdx]
       je        near ptr M00_L09
       mov       rdx,0B86B800458
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
       call      qword ptr [7FF977C25D88]; AuroraScript.Runtime.CallOps.EnsureCapacity(AuroraScript.Runtime.ScriptDatum[], Int32, Int32)
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
       call      qword ptr [7FF977C25D88]; AuroraScript.Runtime.CallOps.EnsureCapacity(AuroraScript.Runtime.ScriptDatum[], Int32, Int32)
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
       mov       rcx,0B86B8014A0
       mov       rsi,[rcx]
       jmp       short M00_L20
M00_L19:
       mov       rcx,offset MT_AuroraScript.Runtime.Types.StringValue
       call      CORINFO_HELP_NEWSFAST
       mov       rsi,rax
       mov       rcx,rsi
       mov       rdx,rdi
       call      qword ptr [7FF977C27F60]
M00_L20:
       jmp       near ptr M00_L02
M00_L21:
       test      r15d,r15d
       jne       short M00_L24
       xor       ebp,ebp
       xor       r15d,r15d
       jmp       near ptr M00_L03
M00_L22:
       mov       rcx,0B86B800B38
       mov       rcx,[rcx]
       mov       edx,r15d
       call      qword ptr [7FF977BF2DB8]; Precode of System.Buffers.SharedArrayPool`1[[AuroraScript.Runtime.ScriptDatum, AuroraScript]].Rent(Int32)
       mov       rsi,rax
       mov       rcx,r14
       mov       rdx,rsi
       xor       r8d,r8d
       call      qword ptr [7FF97782F588]; System.Array.Copy(System.Array, System.Array, Int32)
       mov       rcx,0B86B800B38
       mov       rcx,[rcx]
       mov       rdx,r14
       xor       r8d,r8d
       call      qword ptr [7FF977BF2DC0]; System.Buffers.SharedArrayPool`1[[AuroraScript.Runtime.ScriptDatum, AuroraScript]].Return(AuroraScript.Runtime.ScriptDatum[], Boolean)
       mov       r14,rsi
       jmp       near ptr M00_L04
M00_L23:
       test      r15d,r15d
       jne       short M00_L24
       xor       ecx,ecx
       xor       esi,esi
       jmp       near ptr M00_L05
M00_L24:
       call      qword ptr [7FF9779F7D50]
       int       3
M00_L25:
       call      CORINFO_HELP_POLL_GC
       jmp       near ptr M00_L06
M00_L26:
       mov       rdx,rbp
       call      qword ptr [7FF977C27F48]
       jmp       near ptr M00_L06
M00_L27:
       call      qword ptr [7FF977C25F08]
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
       mov       rcx,0B86B800B38
       mov       rcx,[rcx]
       call      qword ptr [7FF977BF2DB8]; Precode of System.Buffers.SharedArrayPool`1[[AuroraScript.Runtime.ScriptDatum, AuroraScript]].Rent(Int32)
       mov       rdi,rax
       mov       rcx,rbx
       mov       rdx,rdi
       mov       r8d,esi
       call      qword ptr [7FF97782F588]; System.Array.Copy(System.Array, System.Array, Int32)
       test      esi,esi
       jle       short M02_L01
       mov       rcx,rbx
       mov       r8d,esi
       xor       edx,edx
       call      qword ptr [7FF977C2C000]; System.Array.Clear(System.Array, Int32, Int32)
M02_L01:
       mov       rcx,0B86B800B38
       mov       rcx,[rcx]
       mov       rdx,rbx
       xor       r8d,r8d
       call      qword ptr [7FF977BF2DC0]; System.Buffers.SharedArrayPool`1[[AuroraScript.Runtime.ScriptDatum, AuroraScript]].Return(AuroraScript.Runtime.ScriptDatum[], Boolean)
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
       jmp       qword ptr [7FF977825C38]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBaseSlow(System.Runtime.CompilerServices.MethodTable*)
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
       call      qword ptr [7FF97782FD38]
M05_L01:
       mov       rax,[rbp+18]
       mov       ecx,[rax+8]
       call      qword ptr [7FF977BACB28]; System.Buffers.Utilities.SelectBucketIndex(Int32)
       mov       [rbp-14],eax
       mov       ecx,5
       call      qword ptr [7FF977BACB40]; System.Runtime.CompilerServices.StaticsHelpers.GetOptimizedGCThreadStaticBase(Int32)
       mov       rax,[rax+10]
       mov       [rbp-40],rax
       mov       rax,[rbp-40]
       mov       [rbp-48],rax
       cmp       qword ptr [rbp-40],0
       jne       short M05_L02
       mov       rcx,[rbp+10]
       call      qword ptr [7FF977CFDDD0]; System.Buffers.SharedArrayPool`1[[AuroraScript.Runtime.ScriptDatum, AuroraScript]].InitializeTlsBucketsAndTrimming()
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
       call      qword ptr [7FF977C2C6C0]
M05_L03:
       mov       rax,[rbp+18]
       mov       eax,[rax+8]
       mov       [rbp-0C0],eax
       mov       ecx,[rbp-14]
       call      qword ptr [7FF977BACBB8]; System.Buffers.Utilities.GetMaxSizeForBucket(Int32)
       cmp       eax,[rbp-0C0]
       je        short M05_L04
       mov       rcx,offset MT_System.ArgumentException
       call      CORINFO_HELP_NEWSFAST
       mov       [rbp-0F0],rax
       call      qword ptr [7FF977CF6568]
       mov       [rbp-0F8],rax
       mov       ecx,29B
       mov       rdx,7FF977764000
       call      qword ptr [7FF97782F210]
       mov       [rbp-100],rax
       mov       rdx,[rbp-0F8]
       mov       r8,[rbp-100]
       mov       rcx,[rbp-0F0]
       call      qword ptr [7FF977B1D128]
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
       call      qword ptr [7FF977CF6580]; System.Buffers.SharedArrayPoolThreadLocalArray..ctor(System.Array)
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
       call      qword ptr [7FF977CFDDE8]
       mov       [rbp-0E8],rax
M05_L05:
       mov       rcx,[rbp-0E8]
       mov       rdx,[rbp-38]
       cmp       [rcx],ecx
       call      qword ptr [7FF977CF65B0]
       mov       [rbp-28],eax
M05_L06:
       mov       rax,0B86B8002B8
       mov       rax,[rax]
       mov       [rbp-30],rax
       mov       rcx,[rbp-30]
       cmp       [rcx],ecx
       call      qword ptr [7FF977BACB58]; System.Diagnostics.Tracing.EventSource.IsEnabled()
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
       call      qword ptr [7FF977BACB70]
       mov       [rbp-104],eax
       mov       r9d,[rbp-104]
       mov       edx,[rbp-4C]
       mov       r8d,[rbp-50]
       mov       rcx,[rbp-30]
       cmp       [rcx],ecx
       call      qword ptr [7FF977CF65C8]
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
       call      qword ptr [7FF977BACB70]
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
       call      qword ptr [7FF977CF65E0]
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
       call      qword ptr [7FF977BACB40]; System.Runtime.CompilerServices.StaticsHelpers.GetOptimizedGCThreadStaticBase(Int32)
       lea       rcx,[rax+10]
       mov       rdx,[rbp-8]
       call      CORINFO_HELP_ASSIGN_REF
       mov       rax,[rbp+10]
       mov       rcx,[rax+8]
       mov       rdx,[rbp-8]
       xor       r8d,r8d
       cmp       [rcx],ecx
       call      qword ptr [7FF9779F5F80]; System.Runtime.CompilerServices.ConditionalWeakTable`2[[System.__Canon, System.Private.CoreLib],[System.__Canon, System.Private.CoreLib]].Add(System.__Canon, System.__Canon)
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
       mov       rax,0B86B8016F8
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
       mov       rax,0B86B8016F0
       mov       rax,[rax]
       mov       [rbp-28],rax
       mov       rdx,[rbp-28]
       mov       rcx,[rbp-20]
       mov       r8,7FF977CF9E00
       call      qword ptr [7FF977826BB0]; System.MulticastDelegate.CtorClosed(System.Object, IntPtr)
       mov       rcx,offset MT_System.Buffers.SharedArrayPool<AuroraScript.Runtime.ScriptDatum>+<>c
       call      System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rdx,[rbp-20]
       mov       rcx,0B86B8016F8
       call      CORINFO_HELP_ASSIGN_REF
       mov       rax,[rbp-20]
       mov       [rbp-18],rax
M11_L00:
       mov       rcx,[rbp-18]
       mov       rdx,[rbp+10]
       call      qword ptr [7FF977CF6628]; System.Gen2GcCallback.Register(System.Func`2<System.Object,Boolean>, System.Object)
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
       mov       rcx,7FF977CB21AC
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
       mov       rcx,7FF977CB21A8
       call      CORINFO_HELP_COUNTPROFILE32
       call      qword ptr [7FF977C2F150]
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
       push      rbx
       sub       rsp,20
       mov       rbx,rcx
       xor       esi,esi
       xor       edi,edi
       jmp       short M00_L03
M00_L00:
       mov       eax,esi
       add       rsp,20
       pop       rbx
       pop       rsi
       pop       rdi
       ret
M00_L01:
       vmovq     rcx,xmm0
       test      rcx,rcx
       jl        short M00_L04
M00_L02:
       xor       esi,eax
       inc       edi
       cmp       edi,200
       jge       short M00_L00
M00_L03:
       mov       rax,[rbx+10]
       cmp       edi,[rax+8]
       jae       short M00_L05
       vmovsd    xmm0,qword ptr [rax+rdi*8+10]
       vmovaps   xmm1,xmm0
       vxorps    xmm2,xmm2,xmm2
       vmaxsd    xmm2,xmm1,xmm2
       mov       eax,0FFFFFFFF
       vcvttsd2si rcx,xmm2
       vucomisd  xmm1,qword ptr [7FF9778AFA08]
       cmovb     eax,ecx
       mov       ecx,eax
       vxorps    xmm1,xmm1,xmm1
       vcvtsi2sd xmm1,xmm1,rcx
       vucomisd  xmm1,xmm0
       jp        short M00_L04
       jne       short M00_L04
       test      eax,eax
       jne       short M00_L02
       jmp       short M00_L01
M00_L04:
       call      qword ptr [7FF977C35D10]
       jmp       short M00_L02
M00_L05:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 135
```

## .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3 (Job: Job-BZNPUE(IterationCount=5, IterationTime=200ms, LaunchCount=1, WarmupCount=3))

```assembly
; AuroraBenchmark.BoundaryOpsBenchmarks.IsUInt32()
       sub       rsp,28
       xor       eax,eax
       mov       rcx,[rcx+18]
       xor       edx,edx
       jmp       short M00_L02
       xchg      ax,ax
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
       vmovaps   xmm1,xmm0
       vxorps    xmm2,xmm2,xmm2
       vmaxsd    xmm2,xmm1,xmm2
       mov       r8d,0FFFFFFFF
       vcvttsd2si r10,xmm2
       vucomisd  xmm1,qword ptr [7FF9778BF9E0]
       cmovb     r8d,r10d
       mov       r10d,r8d
       vxorps    xmm1,xmm1,xmm1
       vcvtsi2sd xmm1,xmm1,r10
       vucomisd  xmm1,xmm0
       jp        short M00_L01
       jne       short M00_L01
       test      r8d,r8d
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
; Total bytes of code 127
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
       mov       rcx,0C507400450
       cmp       rbp,[rcx]
       jne       short M00_L06
       mov       r15d,1000
       jmp       near ptr M00_L01
M00_L06:
       mov       rcx,offset MT_AuroraScript.Runtime.ScriptDatum
       call      qword ptr [7FF977825728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       r8,0C507400458
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
       call      qword ptr [7FF977C25DA0]
       xor       r13d,r13d
       jmp       near ptr M00_L04
M00_L16:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 521
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
       vmovsd    xmm1,qword ptr [7FF9778C04A8]
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
       vmovsd    xmm2,qword ptr [7FF9778C04A8]
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
       call      qword ptr [7FF977845728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rcx,0A751800450
       cmp       rbp,[rcx]
       jne       short M00_L06
       mov       r15d,1000
       jmp       near ptr M00_L01
M00_L06:
       mov       rcx,offset MT_AuroraScript.Runtime.ScriptDatum
       call      qword ptr [7FF977845728]; System.Runtime.CompilerServices.StaticsHelpers.GetGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       r8,0A751800458
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
       vmovsd    xmm0,qword ptr [7FF9778C04B0]
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
       call      qword ptr [7FF977C45D88]
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

