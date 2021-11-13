using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GVDT_Equipo6 : ISSR_TeamBehaviour {

    public override void CreateTeam()
    {

        if (!InitError())
        {//No hay error al inicializar
            if (RegisterTeam("GVDT", "Robotroncos"))
                Debug.Log("Los robotroncos están readys");
            {//Registro del equipo
                for (int index = 0; index < GetNumberOfAgentsInTeam(); index++)
                {//crear agentes en posiciones
                   CreateAgent(new GVDT_Agente6());
                }
            }
        }
      
    }

 
}
