using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GVDT_Agente3 : ISSR_Agent {

	public override void Start()
    {
        Debug.LogFormat("{0}: comienza", Myself.Name);

    }

    public override  IEnumerator Update()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.5f);
            current_event = ISSREventType.onTickElapsed;
            current_state = AgentStateMachine();
        }

    }

    ISSRState AgentStateMachine()//Función principal de máquina de estados
    {
        ISSRState next_state = current_state;//Estado de salida , en principio igual que el estado actual

        switch (current_state)

        {
            case ISSRState.Idle:
                next_state = SF_Idle();
                break;

            case ISSRState.AvoidingObstacle:

                next_state = SF_AvoidingObstacle();
                break;

            case ISSRState.GoingToGripSmallStone:

                next_state = SF_GoingToGripSmallStone();
                break;

            case ISSRState.GoingToGoalWithSmallStone:

                next_state =SF_GoingToGoalWithSmallStone();
                break;

            case ISSRState.End:
                break;
            case ISSRState.Error:
                break;

            default:

                Debug.LogFormat("{0}: Estado '{1}' no considerado", Myself.Name, current_state);
                break;

        }
        if (current_state != next_state) //si ha cambiado el estado
        {
            Debug.LogWarningFormat("{0}: Estado '{1}'-->'{2}' por evento '{3}'",Myself.Name,current_state,next_state,current_event);
        }

        return next_state;

    }

    //FUNCIONES PARA CADA ESTADO
    ISSRState SF_Idle()//SF--> "State Function"
    {
        ISSRState next_state = current_state;//Estado de salida , en principio igual que el estado actual

        switch (current_event)
        {
            case ISSREventType.onTickElapsed:

                focus_object = ISSRHelp.GetCloserToMeObjectInList(this, Valid_Small_Stones, ISSR_Type.SmallStone);///coge de la lista la piedra pequeña más cercana al agente y la convierte en objeto de interés
                if (focus_object != null)//si hay alguna piedra pequeña 
                {
                    next_state = ProcessStone();
                }
                else//Si focus_object es null significa que no hay más piedras disponibles
                {
                    Debug.LogFormat("{0}: No conozco más piedras pequeñas", Myself.Name);

                }

                break;
            default:

                Debug.LogWarningFormat("{0}: Evento '{1}' no considerado en estado '{2}'", Myself.Name, current_event, current_event);
                break;

        }
       
        return next_state;

    }

    ISSRState SF_AvoidingObstacle()//SF--> "State Function"
    {
        ISSRState next_state = current_state;//Estado de salida , en principio igual que el estado actual

        switch (current_event)
        {
            case ISSREventType.onCollision:

                next_state = ProcessCollision();
                break;

            case ISSREventType.onGObjectCollision:

                next_state = ProcessCollision();
                break;

            case ISSREventType.onGObjectScored:

                Debug.LogFormat("{0}: Se ha introducido la piedra '{1}' mientras se esquivaba un obstáculo", Myself.Name, focus_object.Name);
                next_state = ISSRState.Idle;
                break;

            case ISSREventType.onDestArrived:

                next_state = ResumeAfterCollision();
                break;

            default:

                Debug.LogWarningFormat("{0}: Evento '{1}' no considerado en estado '{2}'", Myself.Name, current_event, current_event);
                break;

        }

        return next_state;

    }


    ISSRState SF_GoingToGripSmallStone()//SF--> "State Function"
    {
        ISSRState next_state = current_state;//Estado de salida , en principio igual que el estado actual

        switch (current_event)
        {
            case ISSREventType.onGripSuccess:

                Valid_Small_Stones.Remove(focus_object);//Elimina el objeto de interés de la lista de piedras pequeñas disponibles
                acGotoLocation(iMyGoalLocation());
                if(acCheckError())
                {
                    next_state = ISSRState.Error;//señala error
                }
                else
                {
                    next_state = ISSRState.GoingToGoalWithSmallStone;
                    Debug.LogFormat("{0}: Piedra '{1}' agarrada con éxito", Myself.Name, focus_object.Name);

                }
                break;

            case ISSREventType.onEnterSensingArea:

                if (object_just_seen.Equals(focus_object))
                {
                    acGripObject(focus_object);

                    if (acCheckError())
                    {
                         next_state = ISSRState.Error;//señala error
                    }
                    
                }
          
                break;
            case ISSREventType.onCollision:

                if (focus_object.Equals(colliding_object))//Si el objeto con el que ha colisionado es el que tenía como objetivo se solicita volver a coger el objeto
                {
                    next_state = ProcessStone();
                }
                else
                {
                    next_state = ProcessCollision();
                }
                break;

            default:

                Debug.LogWarningFormat("{0}: Evento '{1}' no considerado en estado '{2}'", Myself.Name, current_event, current_event);
                break;

        }

        return next_state;

    }

    ISSRState SF_GoingToGoalWithSmallStone()//SF--> "State Function"
    {
        ISSRState next_state = current_state;//Estado de salida , en principio igual que el estado actual

        switch (current_event)
        {
            case ISSREventType.onGObjectScored:

                next_state = ISSRState.Idle;//Pasa a estado inactivo
                Debug.LogFormat("{0}: Piedra '{1}' introducida con éxito", Myself.Name, focus_object.Name);
                break;

            case ISSREventType.onCollision:

                next_state = ProcessCollision();
                break;

            case ISSREventType.onGObjectCollision:

                next_state = ProcessCollision();
                break;

            default:

                Debug.LogWarningFormat("{0}: Evento '{1}' no considerado en estado '{2}'", Myself.Name, current_event, current_event);
                break;
        }

        return next_state;

    }


    //FUNCIONES DE EVENTOS

    public override void onEnterSensingArea(ISSR_Object obj)
    {
        object_just_seen = obj;//Anotams objeto que vemos

        if (obj.type == ISSR_Type.SmallStone)
        {
            if (!Valid_Small_Stones.Contains(obj))
            {
                Valid_Small_Stones.Add(obj);
                Debug.LogFormat("{0}: nueva piedra pequeña '{1}' anotada en la lista de piedras pequeñas", Myself.Name, obj.Name);
            }
           
        }
        current_state = AgentStateMachine();
    }

    public override void onGripSuccess(ISSR_Object obj_gripped)
    {
        Debug.LogFormat("{0}: piedra '{1}' agarrada ", Myself.Name, obj_gripped.Name);
        current_state = AgentStateMachine();
       
    }

    public override void onGObjectScored(ISSR_Object stone_that_scored)
    {
        Debug.LogFormat("{0}: piedra '{1}' metida en meta", Myself.Name, stone_that_scored.Name);
        current_state = AgentStateMachine();
    }

    public override void onCollision(ISSR_Object obj_that_collided_with_me)
    {
        Debug.LogFormat("{0}: He colisionado con '{1}'", Myself.Name, obj_that_collided_with_me.Name);
        colliding_object = obj_that_collided_with_me;
        current_state=AgentStateMachine();


    }

    public override void onGObjectCollision(ISSR_Object obj_that_collided_with_gripped_obj)
    {
        Debug.LogFormat("{0}: El objeto que tengo agarrado ha colisionado con '{1}'", Myself.Name, obj_that_collided_with_gripped_obj.Name);
        colliding_object = obj_that_collided_with_gripped_obj;
        current_state=AgentStateMachine();


    }

    public override void onDestArrived()
    {
        current_state = AgentStateMachine();
    }

       

    //FUNCIONES AUXILIARES
    private ISSRState ProcessStone()
    {
        ISSRState next_state;
        Debug.LogFormat("{0}: Trata de coger '{1}'", Myself.Name, focus_object.Name);

        if (oiSensable(focus_object))//si está a la vista va a coger el objeto
        {
            acGripObject(focus_object);

        }
        else //sino está a la vista irá a la última posición donde lo vio
        {
            acGotoLocation(focus_object.LastLocation);
        }

        if (acCheckError())//si hay error en la acción
        {
            next_state = ISSRState.Error;//señala error
        }
        else//si no hay error
        {
            next_state = ISSRState.GoingToGripSmallStone;//cambia a estado de ir a coger piedra pequeña
        }

        return next_state;
    }

    private ISSRState ProcessCollision()  // Procesar colisi�n 
    {
        ISSRState next_state = current_state;
        Vector3 localizacionSegura = ISSRHelp.CalculateSafeLocation(this, colliding_object);//calcula posicion segura para evitar obstáculo

        switch (current_state)
            
        {
            
            case ISSRState.GoingToGripSmallStone:

                last_state = current_state;//Guarda el estado actual en el último estado para así poder recuperarlo más tarde y poder completar lo que se estaba llevando a cabo antes de la colisión
                acGotoLocation(localizacionSegura);//solicita ir a la posición segura
                if (acCheckError())//comprueba si hay error a la hora de solicitar ir a la posición segura
                {
                    next_state = ISSRState.Error;
                }
                else
                {
                    next_state = ISSRState.AvoidingObstacle;//Pone como siguiente estado el AvoidingObstacle en caso de que no haya error al ir a la posicón segura
                }
                break;
            case ISSRState.GoingToGoalWithSmallStone:

                last_state = current_state;//Guarda el estado actual en el último estado para así poder recuperarlo más tarde y poder completar lo que se estaba llevando a cabo antes de la colisió
                acGotoLocation(localizacionSegura);//solicita ir a la posición segura
                if (acCheckError())//comprueba si hay error a la hora de solicitar ir a la posición segura
                {
                    next_state = ISSRState.Error;
                }
                else
                {
                    next_state = ISSRState.AvoidingObstacle;//Pone como siguiente estado el AvoidingObstacle en caso de que no haya error al ir a la posicón segura
                }
                break;

            case ISSRState.AvoidingObstacle:
              
                acGotoLocation(localizacionSegura);//solicita ir a la posición segura
                if (acCheckError())//comprueba si hay error a la hora de solicitar ir a la posición segura
                {
                    next_state = ISSRState.Error;
                }
                break;

            default:
                Debug.LogErrorFormat("ProcessCollision() en {0}, estado {1} no considerado al colisionar", Myself.Name, current_state);
                next_state = ISSRState.Error;//Da error para avisarnos en el futuro de que hemos entrado a un estado aún no considerado
                break;
        }

        return next_state;
    }

    private ISSRState ResumeAfterCollision() // Permite continuar con lo que se estaba haciendo en el momento de la colisión.
    { 
        ISSRState next_state = current_state;

        switch (last_state)  // Según estado anterior 
        {
            case ISSRState.GoingToGripSmallStone:
                next_state = ProcessStone();  // Volver a pedir coger piedra o ir a su lugar
                break;
            case ISSRState.GoingToGoalWithSmallStone:
                acGotoLocation(iMyGoalLocation());  // volver a pedir ir a la meta
                if (acCheckError())
                {
                    next_state = ISSRState.Error;
                }
                else
                {
                    next_state = ISSRState.GoingToGoalWithSmallStone;
                }
                break;
            default:
                Debug.LogErrorFormat("{0}, estado {1} no considerado al volver de colisi�n", Myself.Name, last_state);
                next_state = ISSRState.Error;//Da error para avisarnos de que hemos vuelto a un estado que aún no se ha considerado en esta función
                break;
        }
        return next_state;
    }

}
