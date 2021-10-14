using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GVDT_Equipo4: ISSR_TeamBehaviour{
    public override void CreateTeam(){
      //No hay error al inicializar
      if(!InitError()){
        if(RegisterTeam("GVDT","Pioneros")){
          //Registro del GVDT_Equipo1
          Debug.Log("El equipo GVDT despierta");
          for(int index=0; index < this.GetNumberOfAgentsInTeam(); index ++){
            //Crear agentes en posiciones de los marcadores
            CreateAgent(new GVDT_Agente4());
          }
        }
      }
    }
}
