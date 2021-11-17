using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GVDT_Equipo6 : ISSR_TeamBehaviour {

    public override void CreateTeam()
    {
        if (!InitError())
        {
            if(RegisterTeam("GVDT", "R0b0Troncos"))
            {
                for (int i = 0; i < GetNumberOfAgentsInTeam(); i++)
                {
                    CreateAgent(new GVDT_Agente6());
                }
            }
        }
    }

}
