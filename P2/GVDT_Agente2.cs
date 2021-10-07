using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//Hola don pepito
//hola don jose
public class GVDT_Agente1 : ISSR_Agent
{
    public bool target_fixed = false;
    // Start is called before the first frame update
    public override void Start()
    {
      //  Debug.LogFormat("{0}: Comienza", Myself.Name);
    }

    // Update is called once per frame
    public override IEnumerator Update()
    {
        while(true){
          yield return new WaitForSeconds(0.5f);
          //En este punto generaremos un evento de tiempo
          //current_event = ISSREventType.onTickElapsed;
        }
    }

    public override void onEnterSensingArea(ISSR_Object obj)
    {
      if( (obj.type == ISSR_Type.SmallStone) && !target_fixed){
        //target_fixed = true;
        Debug.LogFormat("{0}: veo {1}, voy a por ella", Myself.Name, obj.Name);
        acGripObject(obj);
      }else{
        Debug.LogFormat("{0}: Estoy ocupado", Myself.Name);
      }
    }

    //Podemos programar las acciones
    public override void onGripSuccess(ISSR_Object obj_gripped)
    {

      Debug.LogFormat("{0}: piedra {1} cogida, voy para la meta", Myself.Name, obj_gripped.Name);
      acGotoLocation(iMyGoalLocation());
    }

    public override void onGObjectScored(ISSR_Object stone_that_scored)
    {
      //target_fixed = false;
      Debug.LogFormat("{0}: piedra {1} en meta", Myself.Name, stone_that_scored.Name);
    }
}
