from pathlib import Path
import ezdxf,json,sys
q=Path(sys.argv[1]) if len(sys.argv)>1 else Path(__file__).resolve().parent.parent/'app/amostras-testes/referencias-dxf';q.mkdir(exist_ok=True)
for version in ['R12','R2010']:
 d=ezdxf.new(version);m=d.modelspace();d.layers.new('EXTERNO');m.add_line((-12.25,3.5),(47,-8.125),dxfattribs={'layer':'EXTERNO'});m.add_circle((10,20),4.75);m.add_text('Iluminação Ω 漢字',dxfattribs={'insert':(7,8),'height':2.5,'rotation':30});d.saveas(q/('externo-'+version+'.dxf'))
d=ezdxf.new('R2010');m=d.modelspace();m.add_line((0,0),(10,0));m.add_lwpolyline([(0,0),(2,3),(5,0)],close=True);m.add_text('Alinhado',dxfattribs={'insert':(1,1),'height':2,'halign':1,'align_point':(2,2)});d.saveas(q/'externo-entidades-adicionais.dxf')
(q/'gerador-independente.json').write_text(json.dumps({'tool':'ezdxf','version':ezdxf.__version__,'fixtures':['externo-R12.dxf','externo-R2010.dxf','externo-entidades-adicionais.dxf'],'purpose':'Fixtures generated without application C# writer; coordinates, Unicode, rotation and unsupported entities'},indent=2))
