using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GVDT_Equipo3 : ISSR_TeamBehaviour {

    public override void CreateTeam()
    {
        if (!InitError())
        {
            if(RegisterTeam("GVDT", "XXXX"))
            {
                for (int i = 0; i < GetNumberOfAgentsInTeam(); i++)
                {
                    CreateAgent(new GVDT_Agente3());
                }
            }
        }
    }

}
